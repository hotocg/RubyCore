using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RubyCore
{
    /// <summary>
    /// Ruby C API 回调辅助
    /// <para>统一处理托管委托包装、参数转换、异常转换和 delegate 生命周期保活</para>
    /// </summary>
    internal static class RbCallback
    {
        private static readonly object DelegateKeepAliveLock = new object();
        private static readonly List<Delegate> DelegateKeepAlive = new List<Delegate>();

        /// <summary>
        /// Ruby C API 不定长参数函数签名
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate VALUE VarArgDelegate(int argc, IntPtr argv, VALUE self);

        /// <summary>
        /// 创建带返回值的 Ruby 回调包装器
        /// </summary>
        internal static VarArgDelegate Create(Func<RbObject, RbObject[], RbObject> func)
        {
            if (func is null) throw new ArgumentNullException(nameof(func));

            return (argc, argv, self) =>
            {
                try
                {
                    var args = ParseArguments(argc, argv);
                    return (func(self.Obj, args) ?? RbTypeMap.Qnil).Ref;
                }
                catch (Exception ex)
                {
                    return RbException.RaiseClrExceptionToRuby(ex);
                }
            };
        }

        /// <summary>
        /// 创建不带返回值的 Ruby 回调包装器
        /// </summary>
        internal static VarArgDelegate Create(Action<RbObject, RbObject[]> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            return (argc, argv, self) =>
            {
                try
                {
                    var args = ParseArguments(argc, argv);
                    action(self.Obj, args);
                    return RbTypeMap.Qnil.Ref;
                }
                catch (Exception ex)
                {
                    return RbException.RaiseClrExceptionToRuby(ex);
                }
            };
        }

        /// <summary>
        /// 保持托管 delegate 存活
        /// <para>Ruby 只保存 C 函数指针，托管 delegate 需要跟随 Ruby VM 生命周期保活</para>
        /// </summary>
        internal static void KeepAlive(Delegate del)
        {
            if (del is null) throw new ArgumentNullException(nameof(del));

            lock (DelegateKeepAliveLock)
            {
                DelegateKeepAlive.Add(del);
            }
        }

        private static RbObject[] ParseArguments(int argc, IntPtr argv)
        {
            if (argc <= 0) return new RbObject[0];

            var args = new RbObject[argc];

            unsafe
            {
                var ptr = (VALUE*)argv.ToPointer();
                for (var i = 0; i < argc; i++) args[i] = ptr[i].Obj;
            }

            return args;
        }
    }
}
