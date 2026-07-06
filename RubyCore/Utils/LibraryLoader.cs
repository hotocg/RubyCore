using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RubyCore
{
    /// <summary>
    /// Windows 库加载
    /// </summary>
    internal class WindowsLoader
    {
        private const string NativeDll = "kernel32.dll";

        [DllImport(NativeDll, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport(NativeDll, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport(NativeDll)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("Psapi.dll", SetLastError = true)]
        internal static extern bool EnumProcessModules(IntPtr hProcess, [In, Out] IntPtr[] lphModule, uint lphModuleByteCount, out uint byteCountNeeded);


        internal static IntPtr Load(string DllPath)
        {
            if (DllPath is null) return IntPtr.Zero;
            var Module = LoadLibrary(DllPath);
            if (Module == IntPtr.Zero) throw new DllNotFoundException($"无法加载: {DllPath}", new Win32Exception());
            return Module;
        }

        /// <summary>
        /// 获取函数指针
        /// </summary>
        /// <param name="hModule"></param>
        /// <param name="lpProcName"></param>
        /// <returns></returns>
        /// <exception cref="MissingMethodException"></exception>
        internal static IntPtr GetFunction(IntPtr hModule, string lpProcName)
        {
            if (hModule == IntPtr.Zero)
            {
                foreach (var Module in GetAllModules())
                {
                    var Func = GetProcAddress(Module, lpProcName);
                    if (Func != IntPtr.Zero) return Func;
                }
            }

            var Proc = GetProcAddress(hModule, lpProcName);
            if (Proc == IntPtr.Zero) throw new MissingMethodException($"未找到函数: {lpProcName}", new Win32Exception());
            return Proc;
        }

        /// <summary>
        /// 获取指定函数的委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="FuncName"></param>
        /// <param name="DllPath"></param>
        /// <returns></returns>
        internal static T GetFuncByName<T>(string FuncName, string DllPath) where T : Delegate
        {
            return Marshal.GetDelegateForFunctionPointer<T>(GetFunction(Load(DllPath), FuncName));
        }

        internal static VALUE GetValueByName(string name, string dllPath)
        {
            var address = GetFunction(Load(dllPath), name);
            return new VALUE(Marshal.ReadIntPtr(address));
        }

        internal void Free(IntPtr hModule) => FreeLibrary(hModule);

        internal static IntPtr[] GetAllModules()
        {
            using (var self = Process.GetCurrentProcess())
            {
                uint Bytes = 0;
                var Result = new IntPtr[0];
                if (!EnumProcessModules(self.Handle, Result, Bytes, out var NeedsBytes)) throw new Win32Exception();

                while (Bytes < NeedsBytes)
                {
                    Bytes = NeedsBytes;
                    Result = new IntPtr[Bytes / IntPtr.Size];
                    if (!EnumProcessModules(self.Handle, Result, Bytes, out NeedsBytes)) throw new Win32Exception();
                }

                return Result.Take((int)(NeedsBytes / IntPtr.Size)).ToArray();
            }
        }
    }
}
