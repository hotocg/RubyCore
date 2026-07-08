using System;

namespace RubyCore
{
    public class RbModule : RbObject
    {
        internal RbModule(VALUE refVal) : base(refVal) { }

        public RbModule(string name) : base(DefineModule(name)) { }

        private static VALUE DefineModule(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 模块名称不能为空", nameof(name));
            return Runtime.rb_define_module(name);
        }

        /// <summary>
        /// <inheritdoc cref="Runtime.rb_define_module_function"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="del"></param>
        /// <param name="arity"></param>
        private void RegisterFunc(string name, Delegate del, int arity)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 模块函数名称不能为空", nameof(name));
            if (del is null) throw new ArgumentNullException(nameof(del));

            RbCallback.KeepAlive(del);
            Runtime.rb_define_module_function(this.Ref, name, del, arity);
        }

        //public void DefineFunction(string name, Delegate func)
        //{
        //    var method = func.Method;
        //    var parameters = method.GetParameters();
        //    int arity = 0;

        //    // 判断是否符合不定长参数的签名 (int argc, IntPtr argv, VALUE self)
        //    if (parameters.Length == 3 && parameters[0].ParameterType == typeof(int) && parameters[1].ParameterType == typeof(IntPtr))
        //    {
        //        arity = -1;
        //    }
        //    else
        //    {
        //        // 普通固定参数
        //        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(VALUE))
        //        {
        //            throw new ArgumentException("Delegate 第一个参数必须是 VALUE (表示 self)");
        //        }

        //        arity = parameters.Length - 1;
        //    }

        //    //_delegateKeepAlive.Add(func);

        //    Runtime.rb_define_module_function(this.Ref, name.ToLower(), func, arity);
        //}

        ///// <summary>
        ///// 定义方法：无参数
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="func"></param>
        //public void DefineFunction(string name, Func<VALUE, VALUE> func) => Register(name, func, 0);

        ///// <summary>
        ///// 定义方法：1 个参数
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="func"></param>
        //public void DefineFunction(string name, Func<VALUE, VALUE, VALUE> func) => Register(name, func, 1);

        /// <summary>
        /// 定义 Ruby 模块函数
        /// <para>Ruby 调用该函数时会进入 C# 委托，CLR 异常会转换为 Ruby 异常</para>
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <param name="func">函数</param>
        public void DefineFunction(string name, Func<RbObject, RbObject[], RbObject> func)
        {
            RegisterFunc(name, RbCallback.Create(func), -1);
        }

        /// <summary>
        /// <inheritdoc cref="DefineFunction(string, Func{RbObject, RbObject[], RbObject})"/>
        /// </summary>
        public void DefineFunction(string name, Action<RbObject, RbObject[]> action)
        {
            RegisterFunc(name, RbCallback.Create(action), -1);
        }

    }

}
