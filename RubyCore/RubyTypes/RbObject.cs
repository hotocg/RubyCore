using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace RubyCore
{
    /// <summary>
    /// Ruby 对象包装
    /// <para>统一提供方法调用、索引访问、属性访问、动态调用和托管类型转换入口</para>
    /// </summary>
    public class RbObject : DynamicObject
    {
        public IntPtr Pointer;
        public VALUE Ref => new(Pointer);

        /// <summary>
        /// 是否空指针
        /// </summary>
        public bool IsNull => Pointer == IntPtr.Zero;

        /// <summary>
        /// 是否空值
        /// </summary>
        public bool IsNil => Pointer == RbTypeMap.Qnil.Pointer;

        /// <summary>
        /// Ruby 类型
        /// </summary>
        public RbClass Class => new RbClass(Runtime.rb_obj_class(this.Ref));

        public RbObject(VALUE refVal)
        {
            Pointer = refVal.Pointer;
        }

        public RbObject(ID refVal)
        {
            Pointer = refVal.Pointer;
        }

        public override string ToString()
        {
            var strObj = Runtime.rb_obj_as_string(new(Pointer));
            var ptr = Runtime.rb_string_value_cstr(ref strObj);

            return StrPtr.PtrToString(ptr);
        }

        #region 比较
        /// <summary>
        /// 获取对象的哈希值
        /// </summary>
        public override int GetHashCode()
        {
            var hash = Runtime.rb_hash(this.Ref);
            return hash.Pointer.GetHashCode();
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// <para>用于 LINQ、集合类、字典 ... 比较</para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;

            var rbObj = obj as RbObject;
            if (rbObj is null) return false;
            if (Pointer == rbObj.Pointer) return true;

            return false;
        }
        #endregion

        #region 调用
        /// <summary>
        /// 调用当前对象的方法
        /// </summary>
        public RbObject Invoke(string methodName, params RbObject[] args)
        {
            var methodId = Runtime.rb_intern(methodName);
            return Runtime.rb_funcallv(this.Ref, methodId, args.Select(x => x.Ref).ToArray()).Obj;
        }

        /// <summary>
        /// 调用当前对象的方法
        /// </summary>
        public RbObject Invoke(string methodName, params object[] args)
        {
            return Invoke(methodName, args.Select(RbConverter.ToRubyValue).ToArray());
        }

        /// <summary>
        /// 调用对象自身，等价于 Ruby 的 call
        /// </summary>
        public RbObject Invoke(params RbObject[] args)
        {
            return Invoke("call", args);
        }

        /// <summary>
        /// 调用对象自身，等价于 Ruby 的 call
        /// </summary>
        public RbObject Invoke(params object[] args)
        {
            return Invoke(args.Select(RbConverter.ToRubyValue).ToArray());
        }
        #endregion

        #region 索引访问
        /// <summary>
        /// 读取索引项
        /// </summary>
        public virtual RbObject GetItem(params RbObject[] keys)
        {
            return Invoke("[]", keys);
        }

        /// <summary>
        /// 读取索引项
        /// </summary>
        public virtual RbObject GetItem(params object[] keys)
        {
            return GetItem(keys.Select(RbConverter.ToRubyValue).ToArray());
        }

        /// <summary>
        /// 设置索引项
        /// </summary>
        public virtual RbObject SetItem(RbObject key, RbObject value)
        {
            return Invoke("[]=", key, value);
        }

        /// <summary>
        /// 设置索引项
        /// </summary>
        public virtual RbObject SetItem(object key, object value)
        {
            return SetItem(RbConverter.ToRubyValue(key), RbConverter.ToRubyValue(value));
        }

        /// <summary>
        /// 获取对象长度
        /// </summary>
        public virtual int Length()
        {
            return Invoke("length").As<int>();
        }

        public virtual RbObject this[RbObject key]
        {
            get => GetItem(key);
            set => SetItem(key, value);
        }

        public virtual RbObject this[string key]
        {
            get => GetItem(key);
            set => SetItem(key, value);
        }

        public virtual RbObject this[int index]
        {
            get => GetItem(index);
            set => SetItem(index, value);
        }
        #endregion

        #region 属性访问
        /// <summary>
        /// 判断对象是否响应指定方法
        /// </summary>
        public bool HasAttr(string name)
        {
            return Invoke("respond_to?", name).As<bool>();
        }

        /// <summary>
        /// 读取 Ruby 属性或无参方法
        /// </summary>
        public RbObject GetAttr(string name)
        {
            return Invoke(name);
        }

        /// <summary>
        /// 设置 Ruby 属性
        /// </summary>
        public RbObject SetAttr(string name, object value)
        {
            return Invoke($"{name}=", value);
        }
        #endregion

        #region 类型转换
        /// <summary>
        /// 转为托管对象
        /// </summary>
        public object As(Type type = null)
        {
            if (!RbConverter.ToManagedValue(this, type ?? typeof(object), out var result))
            {
                throw new InvalidCastException($"无法将 Ruby 对象转换为 {type?.FullName ?? "object"}");
            }

            return result;
        }

        /// <summary>
        /// 转为托管对象
        /// </summary>
        public T As<T>()
        {
            return (T)As(typeof(T));
        }
        #endregion

        #region 动态对象
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!HasAttr(binder.Name))
            {
                result = null;
                return false;
            }

            result = GetAttr(binder.Name);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            SetAttr(binder.Name, value);
            return true;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = Invoke(args);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = Invoke(binder.Name, args);
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = GetItem(indexes);
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length != 1) return false;

            SetItem(indexes[0], value);
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return RbConverter.ToManagedValue(this, binder.Type, out result);
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            var rbArg = RbConverter.ToRubyValue(arg);

            switch (binder.Operation)
            {
                case ExpressionType.Add:
                    result = Invoke("+", rbArg);
                    return true;
                case ExpressionType.Subtract:
                    result = Invoke("-", rbArg);
                    return true;
                case ExpressionType.Multiply:
                    result = Invoke("*", rbArg);
                    return true;
                case ExpressionType.Divide:
                    result = Invoke("/", rbArg);
                    return true;
                case ExpressionType.Modulo:
                    result = Invoke("%", rbArg);
                    return true;
                case ExpressionType.LeftShift:
                    result = Invoke("<<", rbArg);
                    return true;
                case ExpressionType.Equal:
                    result = Invoke("==", rbArg).As<bool>();
                    return true;
                case ExpressionType.NotEqual:
                    result = !Invoke("==", rbArg).As<bool>();
                    return true;
                case ExpressionType.GreaterThan:
                    result = Invoke(">", rbArg).As<bool>();
                    return true;
                case ExpressionType.GreaterThanOrEqual:
                    result = Invoke(">=", rbArg).As<bool>();
                    return true;
                case ExpressionType.LessThan:
                    result = Invoke("<", rbArg).As<bool>();
                    return true;
                case ExpressionType.LessThanOrEqual:
                    result = Invoke("<=", rbArg).As<bool>();
                    return true;
                default:
                    result = null;
                    return false;
            }
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            switch (binder.Operation)
            {
                case ExpressionType.IsTrue:
                    result = As<bool>();
                    return true;
                case ExpressionType.IsFalse:
                case ExpressionType.Not:
                    result = !As<bool>();
                    return true;
                case ExpressionType.Negate:
                    result = Invoke("-@");
                    return true;
                case ExpressionType.UnaryPlus:
                    result = Invoke("+@");
                    return true;
                default:
                    result = null;
                    return false;
            }
        }
        #endregion
    }
}
