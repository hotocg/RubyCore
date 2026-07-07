using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RubyCore
{
    public class RbModule : RbObject
    {
        private static readonly object DelegateKeepAliveLock = new object();
        private static readonly List<Delegate> DelegateKeepAlive = new List<Delegate>();

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

            lock (DelegateKeepAliveLock)
            {
                DelegateKeepAlive.Add(del); // Ruby 只保存 C 函数指针，必须让托管 delegate 跟随 Ruby VM 生命周期保活
            }

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
        /// 将 Ruby 的 argv 指针解析为 RbObject 数组
        /// </summary>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static RbObject[] ParseArgIntPtr(int argc, IntPtr argv)
        {
            if (argc <= 0) return new RbObject[0];

            RbObject[] args = new RbObject[argc];

            unsafe
            {
                // 将指针转换为 VALUE 数组
                VALUE* ptr = (VALUE*)argv.ToPointer();
                for (int i = 0; i < argc; i++) args[i] = ptr[i].Obj;
            }

            return args;
        }

        /// <summary>
        /// 函数委托：不定长参数
        /// </summary>
        /// <param name="argc">参数数量</param>
        /// <param name="argv">参数数组的指针</param>
        /// <param name="self">接收者对象（模块自身）</param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate VALUE VarArgDelegate(int argc, IntPtr argv, VALUE self);

        /// <summary>
        /// <inheritdoc cref="RegisterFunc"/> 返回值
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <param name="func">函数</param>
        public void DefineFunction(string name, Func<RbObject, RbObject[], RbObject> func)
        {
            if (func is null) throw new ArgumentNullException(nameof(func));

            VarArgDelegate wrapper = (argc, argv, self) =>
            {
                try
                {
                    var args = ParseArgIntPtr(argc, argv);
                    return (func(self.Obj, args) ?? RbTypeMap.Qnil).Ref;
                }
                catch (Exception ex)
                {
                    return RbException.RaiseClrExceptionToRuby(ex);
                }
            };
            RegisterFunc(name, wrapper, -1);
        }

        /// <summary>
        /// 在 Ruby 模块中定义一个不返回值的模块方法 (void)
        /// 包装器内部会自动向 Ruby 解释器返回 Qnil 以确保栈平衡
        /// </summary>
        /// <param name="name">方法名</param>
        /// <param name="action">
        /// C# 委托逻辑：
        /// 参数 1 (RbObject): Ruby 的 self 对象
        /// 参数 2 (RbObject[]): 传入的参数数组
        /// </param>
        public void DefineFunction(string name, Action<RbObject, RbObject[]> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            VarArgDelegate wrapper = (argc, argv, self) =>
            {
                try
                {
                    var args = ParseArgIntPtr(argc, argv);
                    action(self.Obj, args);
                    return RbTypeMap.Qnil.Ref;
                }
                catch (Exception ex)
                {
                    return RbException.RaiseClrExceptionToRuby(ex);
                }
            };
            RegisterFunc(name, wrapper, -1);
        }

    }

}
