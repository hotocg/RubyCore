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
        /// 根据 Ruby 对象真实类型创建合适的包装对象
        /// <para>用于让 Exec、InvokeMethod、GetItem 等返回值自动获得更具体的 Rb* 类型</para>
        /// </summary>
        public static RbObject Wrap(VALUE value)
        {
            if (value.IsNull) return new RbObject(value);
            if (value.Pointer == RbTypeMap.Qnil.Pointer) return RbTypeMap.Qnil;
            if (value.Pointer == RbTypeMap.Qtrue.Pointer || value.Pointer == RbTypeMap.Qfalse.Pointer) return new RbBool(value);

            switch (Runtime.GetClassName(value))
            {
                case "Array":
                    return new RbArray(value);
                case "Hash":
                    return new RbHash(value);
                case "String":
                    return new RbString(value);
                case "Symbol":
                    return new RbSymbol(value);
                case "Float":
                    return new RbFloat(value);
                case "Fixnum":
                case "Bignum":
                case "Integer":
                    return new RbInt(value);
                case "Set":
                    return new RbSet(value);
                default:
                    return new RbObject(value);
            }
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
            if (state != 0)
            {
                try
                {
                    RbException.CatchThrowToCLR();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ruby 方法调用失败: {Class}#{methodName}{Environment.NewLine}异常: {ex.Message.Trim()}", ex);
                }
            }

            return result.Obj;
        }

        /// <summary>
        /// <inheritdoc cref="InvokeMethod(string, RbObject[])"/>
        /// </summary>
        public RbObject InvokeMethod(string methodName, params object[] args)
        {
            return InvokeMethod(methodName, args.Select(RbConverter.ToRubyValue).ToArray());
        }

        /// <summary>
        /// <inheritdoc cref="InvokeMethod(string, RbObject[])"/>
        /// </summary>
        public T InvokeMethod<T>(string methodName, params object[] args)
        {
            return InvokeMethod(methodName, args).As<T>();
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
        /// <inheritdoc cref="Invoke(RbObject[])"/>
        /// </summary>
        public RbObject Invoke(params object[] args)
        {
            return InvokeMethod("call", args);
        }

        /// <summary>
        /// <inheritdoc cref="Invoke(RbObject[])"/>
        /// </summary>
        public T Invoke<T>(params object[] args)
        {
            return Invoke(args).As<T>();
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
        /// <inheritdoc cref="GetItem(RbObject[])"/>
        /// </summary>
        public virtual RbObject GetItem(params object[] keys)
        {
            return InvokeMethod("[]", keys);
        }

        /// <summary>
        /// <inheritdoc cref="GetItem(RbObject[])"/>
        /// </summary>
        public virtual T GetItem<T>(params object[] keys)
        {
            return GetItem(keys).As<T>();
        }

        /// <summary>
        /// 设置索引项
        /// </summary>
        public virtual RbObject SetItem(RbObject key, RbObject value)
        {
            return InvokeMethod("[]=", key, value);
        }

        /// <summary>
        /// <inheritdoc cref="SetItem(RbObject, RbObject)"/>
        /// </summary>
        public virtual RbObject SetItem(object key, object value)
        {
            return InvokeMethod("[]=", key, value);
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
        /// <inheritdoc cref="RespondTo(string)"/>
        /// </summary>
        public bool HasAttr(string name)
        {
            return RespondTo(name);
        }

        /// <summary>
        /// 判断对象是否响应指定方法
        /// <para>等价于 Ruby 的 respond_to?</para>
        /// </summary>
        public bool RespondTo(string name)
        {
            return InvokePredicate("respond_to", name);
        }

        /// <summary>
        /// 读取 Ruby 属性或无参方法
        /// </summary>
        public RbObject GetAttr(string name)
        {
            return InvokeMethod(name);
        }

        /// <summary>
        /// <inheritdoc cref="GetAttr(string)"/>
        /// </summary>
        public T GetAttr<T>(string name)
        {
            return GetAttr(name).As<T>();
        }

        /// <summary>
        /// 设置 Ruby 属性
        /// </summary>
        public RbObject SetAttr(string name, object value)
        {
            return InvokeMethod($"{name}=", value);
        }
        #endregion

        #region 常量
        /// <summary>
        /// 获取当前模块或类下的 Ruby 常量
        /// <para>等价于 Ruby 的 Module::Constant</para>
        /// </summary>
        public RbObject GetConstant(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 常量名称不能为空", nameof(name));

            var symbol = new RbSymbol(name);
            var result = Runtime.rb_const_get_protect(this.Ref, symbol.Ref, out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return result.Obj;
        }

        /// <summary>
        /// 尝试获取当前模块或类下的 Ruby 常量
        /// </summary>
        public bool TryGetConstant(string name, out RbObject result)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                result = null;
                return false;
            }

            var symbol = new RbSymbol(name);
            var value = Runtime.rb_const_get_protect(this.Ref, symbol.Ref, out int state);
            if (state != 0)
            {
                Runtime.rb_set_errinfo(RbTypeMap.Qnil.Ref);
                result = null;
                return false;
            }

            result = value.Obj;
            return true;
        }

        /// <summary>
        /// 设置当前模块或类下的 Ruby 常量
        /// <para>等价于 Ruby 的 Module::Constant = value</para>
        /// </summary>
        public RbObject SetConstant(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 常量名称不能为空", nameof(name));

            var symbol = new RbSymbol(name);
            var rbValue = RbConverter.ToRubyValue(value);
            var result = Runtime.rb_const_set_protect(this.Ref, symbol.Ref, rbValue.Ref, out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return result.Obj;
        }
        #endregion

        #region Ruby 谓词和特殊方法
        /// <summary>
        /// 判断对象是否属于指定类或模块
        /// <para>等价于 Ruby 的 is_a?</para>
        /// </summary>
        public bool IsA(object klass)
        {
            return InvokePredicate("is_a", klass);
        }

        /// <summary>
        /// <inheritdoc cref="IsA(object)"/>
        /// </summary>
        public bool KindOf(object klass)
        {
            return InvokePredicate("kind_of", klass);
        }

        /// <summary>
        /// 判断对象是否正好是指定类的实例
        /// <para>等价于 Ruby 的 instance_of?</para>
        /// </summary>
        public bool InstanceOf(object klass)
        {
            return InvokePredicate("instance_of", klass);
        }

        /// <summary>
        /// 判断对象是否为空集合或空字符串
        /// <para>等价于 Ruby 的 empty?</para>
        /// </summary>
        public bool IsEmpty()
        {
            return InvokePredicate("empty");
        }

        /// <summary>
        /// 判断对象是否包含指定值
        /// <para>等价于 Ruby 的 include?</para>
        /// </summary>
        public bool Include(object value)
        {
            return InvokePredicate("include", value);
        }

        /// <summary>
        /// 判断对象是否包含指定键
        /// <para>等价于 Ruby 的 key?</para>
        /// </summary>
        public virtual bool HasKey(object key)
        {
            return InvokePredicate("key", key);
        }

        /// <summary>
        /// 调用 Ruby 问号结尾的谓词方法
        /// <para>methodName 不需要包含问号</para>
        /// </summary>
        public bool InvokePredicate(string methodName, params object[] args)
        {
            return InvokeMethod($"{methodName}?", args).As<bool>();
        }

        /// <summary>
        /// 调用 Ruby 感叹号结尾的方法
        /// <para>methodName 不需要包含感叹号</para>
        /// </summary>
        public RbObject InvokeBang(string methodName, params object[] args)
        {
            return InvokeMethod($"{methodName}!", args);
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
                throw new InvalidCastException($"无法将 Ruby 对象 ({this} : {Class}) 转换为 ({type?.FullName ?? "object"})");
            }

            return result;
        }

        /// <summary>
        /// <inheritdoc cref="As(Type)"/>
        /// </summary>
        public T As<T>()
        {
            return (T)As(typeof(T));
        }

        /// <summary>
        /// 转为 Ruby 数组包装
        /// </summary>
        public RbArray AsRbArray()
        {
            return new RbArray(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby 布尔值包装
        /// </summary>
        public RbBool AsRbBool()
        {
            return new RbBool(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby 浮点数包装
        /// </summary>
        public RbFloat AsRbFloat()
        {
            return new RbFloat(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby 哈希包装
        /// </summary>
        public RbHash AsRbHash()
        {
            return new RbHash(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby 整数包装
        /// </summary>
        public RbInt AsRbInt()
        {
            return new RbInt(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby 可迭代对象包装
        /// </summary>
        public RbIterable AsRbIterable()
        {
            return new RbIterable(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby 字符串包装
        /// </summary>
        public RbString AsRbString()
        {
            return new RbString(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby Set 包装
        /// </summary>
        public RbSet AsRbSet()
        {
            return new RbSet(this.Ref);
        }

        /// <summary>
        /// 转为 Ruby Symbol 包装
        /// </summary>
        public RbSymbol AsRbSymbol()
        {
            return new RbSymbol(this.Ref);
        }
        #endregion

        #region 动态对象
        /// <summary>
        /// 读取动态成员
        /// </summary>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (HasAttr(binder.Name))
            {
                result = GetAttr(binder.Name);
                return true;
            }

            if (TryGetConstant(binder.Name, out var constant))
            {
                result = constant;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// 设置动态成员
        /// </summary>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (RespondTo($"{binder.Name}="))
            {
                SetAttr(binder.Name, value);
                return true;
            }

            SetConstant(binder.Name, value);
            return true;
        }

        /// <summary>
        /// 调用动态对象自身
        /// </summary>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = InvokeMethod("call", BuildDynamicInvokeArgs(binder.CallInfo, args));
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
            switch (binder.Operation)
            {
                case ExpressionType.Add:
                    result = InvokeMethod("+", arg);
                    return true;
                case ExpressionType.Subtract:
                    result = InvokeMethod("-", arg);
                    return true;
                case ExpressionType.Multiply:
                    result = InvokeMethod("*", arg);
                    return true;
                case ExpressionType.Divide:
                    result = InvokeMethod("/", arg);
                    return true;
                case ExpressionType.Modulo:
                    result = InvokeMethod("%", arg);
                    return true;
                case ExpressionType.LeftShift:
                    result = InvokeMethod("<<", arg);
                    return true;
                case ExpressionType.Equal:
                    result = InvokeMethod("==", arg).As<bool>();
                    return true;
                case ExpressionType.NotEqual:
                    result = !InvokeMethod("==", arg).As<bool>();
                    return true;
                case ExpressionType.GreaterThan:
                    result = InvokeMethod(">", arg).As<bool>();
                    return true;
                case ExpressionType.GreaterThanOrEqual:
                    result = InvokeMethod(">=", arg).As<bool>();
                    return true;
                case ExpressionType.LessThan:
                    result = InvokeMethod("<", arg).As<bool>();
                    return true;
                case ExpressionType.LessThanOrEqual:
                    result = InvokeMethod("<=", arg).As<bool>();
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
