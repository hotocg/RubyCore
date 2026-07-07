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
        /// <summary>
        /// Ruby VALUE 指针
        /// </summary>
        public IntPtr Pointer;

        /// <summary>
        /// Ruby VALUE 结构包装
        /// </summary>
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

        /// <summary>
        /// 使用已有 Ruby VALUE 创建对象包装
        /// </summary>
        public RbObject(VALUE refVal)
        {
            Pointer = refVal.Pointer;
        }

        /// <summary>
        /// 使用 Ruby ID 创建对象包装
        /// </summary>
        public RbObject(ID refVal)
        {
            Pointer = refVal.Pointer;
        }

        /// <summary>
        /// 转为 Ruby 字符串表示
        /// </summary>
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
        /// <para>等价于 Ruby 的 obj.method_name(*args)</para>
        /// </summary>
        public RbObject InvokeMethod(string methodName, params RbObject[] args)
        {
            var methodId = Runtime.rb_intern(methodName);
            var result = Runtime.rb_funcallv_protect(this.Ref, methodId, args.Select(x => x.Ref).ToArray(), out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return result.Obj;
        }

        /// <summary>
        /// 调用当前对象的方法
        /// <para>参数会先转换为 Ruby 对象</para>
        /// </summary>
        public RbObject InvokeMethod(string methodName, params object[] args)
        {
            return InvokeMethod(methodName, args.Select(RbConverter.ToRubyValue).ToArray());
        }

        /// <summary>
        /// 调用对象自身，等价于 Ruby 的 call
        /// <para>用于 Proc、Method 等本身可调用的 Ruby 对象</para>
        /// </summary>
        public RbObject Invoke(params RbObject[] args)
        {
            return InvokeMethod("call", args);
        }

        /// <summary>
        /// 调用对象自身，等价于 Ruby 的 call
        /// <para>参数会先转换为 Ruby 对象</para>
        /// </summary>
        public RbObject Invoke(params object[] args)
        {
            return InvokeMethod("call", args);
        }

        /// <summary>
        /// 构建动态调用参数
        /// <para>命名参数会合并为最后一个 Ruby Hash 参数</para>
        /// </summary>
        private static RbObject[] BuildDynamicInvokeArgs(CallInfo callInfo, object[] args)
        {
            var namedArgCount = callInfo.ArgumentNames.Count;
            if (namedArgCount == 0)
            {
                return args.Select(RbConverter.ToRubyValue).ToArray();
            }

            var normalArgCount = args.Length - namedArgCount;
            var rbArgs = new RbObject[normalArgCount + 1];

            for (int i = 0; i < normalArgCount; i++)
            {
                rbArgs[i] = RbConverter.ToRubyValue(args[i]);
            }

            var namedArgs = new RbHash();
            for (int i = 0; i < namedArgCount; i++)
            {
                var key = new RbSymbol(callInfo.ArgumentNames[i]);
                var value = RbConverter.ToRubyValue(args[normalArgCount + i]);
                namedArgs.SetItem(key, value);
            }

            rbArgs[normalArgCount] = namedArgs;
            return rbArgs;
        }
        #endregion

        #region 索引访问
        /// <summary>
        /// 读取索引项
        /// </summary>
        public virtual RbObject GetItem(params RbObject[] keys)
        {
            return InvokeMethod("[]", keys);
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
            return InvokeMethod("[]=", key, value);
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
            return InvokeMethod("length").As<int>();
        }

        /// <summary>
        /// 通过 Ruby 对象作为键访问索引项
        /// </summary>
        public virtual RbObject this[RbObject key]
        {
            get => GetItem(key);
            set => SetItem(key, value);
        }

        /// <summary>
        /// 通过字符串作为键访问索引项
        /// </summary>
        public virtual RbObject this[string key]
        {
            get => GetItem(key);
            set => SetItem(key, value);
        }

        /// <summary>
        /// 通过整数作为索引访问索引项
        /// </summary>
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
            return InvokeMethod("respond_to?", name).As<bool>();
        }

        /// <summary>
        /// 读取 Ruby 属性或无参方法
        /// </summary>
        public RbObject GetAttr(string name)
        {
            return InvokeMethod(name);
        }

        /// <summary>
        /// 设置 Ruby 属性
        /// </summary>
        public RbObject SetAttr(string name, object value)
        {
            return InvokeMethod($"{name}=", value);
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
        /// <summary>
        /// 读取动态成员
        /// </summary>
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

        /// <summary>
        /// 设置动态成员
        /// </summary>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            SetAttr(binder.Name, value);
            return true;
        }

        /// <summary>
        /// 调用动态对象自身
        /// </summary>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = Invoke(BuildDynamicInvokeArgs(binder.CallInfo, args));
            return true;
        }

        /// <summary>
        /// 调用动态成员方法
        /// </summary>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = InvokeMethod(binder.Name, BuildDynamicInvokeArgs(binder.CallInfo, args));
            return true;
        }

        /// <summary>
        /// 读取动态索引项
        /// </summary>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = GetItem(indexes);
            return true;
        }

        /// <summary>
        /// 设置动态索引项
        /// </summary>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length != 1) return false;

            SetItem(indexes[0], value);
            return true;
        }

        /// <summary>
        /// 动态转换为托管类型
        /// </summary>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return RbConverter.ToManagedValue(this, binder.Type, out result);
        }

        /// <summary>
        /// 转发动态二元运算到 Ruby 运算符方法
        /// </summary>
        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            var rbArg = RbConverter.ToRubyValue(arg);

            switch (binder.Operation)
            {
                case ExpressionType.Add:
                    result = InvokeMethod("+", rbArg);
                    return true;
                case ExpressionType.Subtract:
                    result = InvokeMethod("-", rbArg);
                    return true;
                case ExpressionType.Multiply:
                    result = InvokeMethod("*", rbArg);
                    return true;
                case ExpressionType.Divide:
                    result = InvokeMethod("/", rbArg);
                    return true;
                case ExpressionType.Modulo:
                    result = InvokeMethod("%", rbArg);
                    return true;
                case ExpressionType.LeftShift:
                    result = InvokeMethod("<<", rbArg);
                    return true;
                case ExpressionType.Equal:
                    result = InvokeMethod("==", rbArg).As<bool>();
                    return true;
                case ExpressionType.NotEqual:
                    result = !InvokeMethod("==", rbArg).As<bool>();
                    return true;
                case ExpressionType.GreaterThan:
                    result = InvokeMethod(">", rbArg).As<bool>();
                    return true;
                case ExpressionType.GreaterThanOrEqual:
                    result = InvokeMethod(">=", rbArg).As<bool>();
                    return true;
                case ExpressionType.LessThan:
                    result = InvokeMethod("<", rbArg).As<bool>();
                    return true;
                case ExpressionType.LessThanOrEqual:
                    result = InvokeMethod("<=", rbArg).As<bool>();
                    return true;
                default:
                    result = null;
                    return false;
            }
        }

        /// <summary>
        /// 转发动态一元运算到 Ruby 运算符方法
        /// </summary>
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
                    result = InvokeMethod("-@");
                    return true;
                case ExpressionType.UnaryPlus:
                    result = InvokeMethod("+@");
                    return true;
                default:
                    result = null;
                    return false;
            }
        }
        #endregion
    }
}
