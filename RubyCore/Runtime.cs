using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RubyCore
{
    public unsafe partial class Runtime
    {
        /// <summary>
        /// Api DLL 路径
        /// </summary>
        internal static string _ApiDll = RbEngine.GetDefaultApiDll();

        /// <summary>
        /// Api 版本
        /// </summary>
        internal static Version _ApiVersion
        {
            get
            {
                var VerInfo = FileVersionInfo.GetVersionInfo(_ApiDll);
                return new Version(VerInfo.ProductMajorPart, VerInfo.ProductMinorPart, VerInfo.ProductBuildPart, VerInfo.ProductPrivatePart);
            }
        }

        /// <summary>
        /// 判断模块是否存在
        /// </summary>
        internal static bool IsExistModule
        {
            get
            {
                foreach (var module in WindowsLoader.GetAllModules())
                {
                    if (WindowsLoader.GetProcAddress(module, "ruby_init") != IntPtr.Zero) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 自动初始化
        /// </summary>
        internal static void AutoInit()
        {
            if (!IsInitialized)
            {
                IsInitialized = IsExistModule;
                if (!IsInitialized)
                {
                    Initialize();
                }
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        internal static bool IsInitialized = false;

        /// <summary>
        /// 初始化引擎
        /// </summary>
        internal static void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;

            if (_ApiVersion.Major >= 2 && _ApiVersion.Minor >= 7)
            {
                var argc = 0;
                var argvPtr = IntPtr.Zero;
                ruby_sysinit(ref argc, ref argvPtr);
            }

            ruby_init();
            //ruby_setup();
            ruby_init_loadpath();

            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"DllPath: {_ApiDll}");
            Console.Write("Version: ");
            ruby_show_version();
            Console.WriteLine(new string('-', 50));
            //RbEngine.ShowConfigInfo();

            //Console.WriteLine($">>>>>>>>>>>>>>>>>> {new VALUE(0x04).Obj} | {new RbObject(0x04)}");

        }

        internal static void Shutdown()
        {
            if (!IsInitialized) return;

            ruby_finalize();
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            IsInitialized = false;
        }

        // ==================================================
        #region Api

        #region 引擎
        internal static void ruby_sysinit(ref int argc, ref IntPtr argv) => Delegates.ruby_sysinit(ref argc, ref argv);

        internal static void ruby_setup() => Delegates.ruby_setup();
        internal static void ruby_init() => Delegates.ruby_init();
        /// <summary>
        /// 初始化标准库加载路径
        /// </summary>
        internal static void ruby_init_loadpath() => Delegates.ruby_init_loadpath();
        internal static void ruby_finalize() => Delegates.ruby_finalize();

        internal static void ruby_show_version() => Delegates.ruby_show_version();
        
        internal static VALUE rb_eval_string(StrPtr str) => Delegates.rb_eval_string(str);
        internal static VALUE rb_eval_string_protect(StrPtr str, out int state) => Delegates.rb_eval_string_protect(str, out state);

        internal static void rb_p(VALUE obj) => Delegates.rb_p(obj);
        internal static void rb_io_puts(int argc, IntPtr[] argv, VALUE io) => Delegates.rb_io_puts(argc, argv, io);
        
        #endregion

        #region 模块
        /// <summary>
        /// 定义模块
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static VALUE rb_define_module(string name) => Delegates.rb_define_module(name);

        /// <summary>
        /// 定义模块函数
        /// </summary>
        /// <param name="klass">模块</param>
        /// <param name="mid">函数名称</param>
        /// <param name="func">函数委托</param>
        /// <param name="arity">参数数量</param>
        internal static void rb_define_module_function(VALUE klass, string mid, Delegate func, int arity) => Delegates.rb_define_module_function(klass, mid, func, arity);

        #endregion

        #region 对象
        /// <summary>
        /// Ruby 对象转字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static VALUE rb_obj_as_string(VALUE obj) => Delegates.rb_obj_as_string(obj);
        /// <summary>
        /// 获取对象类别
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static VALUE rb_obj_class(VALUE obj) => Delegates.rb_obj_class(obj);
        //internal static string rb_obj_classname(VALUE obj) => Delegates.rb_obj_classname(obj);

        /// <summary>
        /// Ruby 字符串对象转 C 字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static IntPtr rb_string_value_cstr(ref VALUE str) => Delegates.rb_string_value_cstr(ref str);

        internal static VALUE rb_str_new_cstr(string str) => Delegates.rb_str_new_cstr(new(str));
        internal static VALUE rb_utf8_str_new_cstr(string str) => Delegates.rb_utf8_str_new_cstr(new(str));
        

        internal static VALUE rb_hash(VALUE obj) => Delegates.rb_hash(obj);
        internal static nint rb_num2int(VALUE num) => Delegates.rb_num2int(num);

        /// <summary>
        /// 获取全局变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static VALUE rb_gv_get(string name) => Delegates.rb_gv_get(name);

        #endregion

        #region 调用
        //internal static VALUE rb_funcall(VALUE recv, string mid, int argc, params VALUE[] argv)
        //{
        //    var methodId = RbEngine.rb_intern(mid);
        //    return RbEngine.rb_funcallv(recv, methodId, argc, argv);
        //}
        #endregion

        #region 异常
        /// <summary>
        /// 获取异常信息
        /// </summary>
        /// <returns></returns>
        internal static VALUE rb_errinfo() => Delegates.rb_errinfo();
        /// <summary>
        /// 设置异常信息
        /// </summary>
        /// <param name="err"></param>
        internal static void rb_set_errinfo(VALUE err) => Delegates.rb_set_errinfo(err);
        #endregion

        #region 类
        /// <summary>
        /// 获取类名
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static VALUE rb_class_name(VALUE obj) => Delegates.rb_class_name(obj);

        #endregion


        #endregion

        #region 初始化委托
        internal static class Delegates
        {
            static Delegates()
            {
                if (!File.Exists(_ApiDll))
                {
                    throw new FileNotFoundException($"Api Dll 不存在: {_ApiDll}");
                }


                ruby_sysinit = WindowsLoader.GetFuncByName<Delegate_ruby_sysinit>(nameof(ruby_sysinit), _ApiDll);
                ruby_setup = WindowsLoader.GetFuncByName<Delegate_ruby_setup>(nameof(ruby_setup), _ApiDll);
                ruby_init = WindowsLoader.GetFuncByName<Delegate_ruby_init>(nameof(ruby_init), _ApiDll);
                ruby_init_loadpath = WindowsLoader.GetFuncByName<Delegate_ruby_init_loadpath>(nameof(ruby_init_loadpath), _ApiDll);
                ruby_finalize = WindowsLoader.GetFuncByName<Delegate_ruby_finalize>(nameof(ruby_finalize), _ApiDll);

                ruby_show_version = WindowsLoader.GetFuncByName<Delegate_ruby_show_version>(nameof(ruby_show_version), _ApiDll);
                
                rb_eval_string = WindowsLoader.GetFuncByName<Delegate_rb_eval_string>(nameof(rb_eval_string), _ApiDll);
                rb_eval_string_protect = WindowsLoader.GetFuncByName<Delegate_rb_eval_string_protect>(nameof(rb_eval_string_protect), _ApiDll);
                rb_p = WindowsLoader.GetFuncByName<Delegate_rb_p>(nameof(rb_p), _ApiDll);
                rb_io_puts = WindowsLoader.GetFuncByName<Delegate_rb_io_puts>(nameof(rb_io_puts), _ApiDll);
                

                rb_define_module = WindowsLoader.GetFuncByName<Delegate_rb_define_module>(nameof(rb_define_module), _ApiDll);
                rb_define_module_function = WindowsLoader.GetFuncByName<Delegate_rb_define_module_function>(nameof(rb_define_module_function), _ApiDll);


                rb_obj_as_string = WindowsLoader.GetFuncByName<Delegate_rb_obj_as_string>(nameof(rb_obj_as_string), _ApiDll);
                rb_obj_class = WindowsLoader.GetFuncByName<Delegate_rb_obj_class>(nameof(rb_obj_class), _ApiDll);
                rb_obj_classname = WindowsLoader.GetFuncByName<Delegate_rb_obj_classname>(nameof(rb_obj_classname), _ApiDll);


                rb_string_value_cstr = WindowsLoader.GetFuncByName<Delegate_rb_string_value_cstr>(nameof(rb_string_value_cstr), _ApiDll);

                rb_str_new_cstr = WindowsLoader.GetFuncByName<Delegate_rb_str_new_cstr>(nameof(rb_str_new_cstr), _ApiDll);
                rb_utf8_str_new_cstr = WindowsLoader.GetFuncByName<Delegate_rb_utf8_str_new_cstr>(nameof(rb_utf8_str_new_cstr), _ApiDll);

                rb_hash = WindowsLoader.GetFuncByName<Delegate_rb_hash>(nameof(rb_hash), _ApiDll);
                rb_num2int = WindowsLoader.GetFuncByName<Delegate_rb_num2int>(nameof(rb_num2int), _ApiDll);

                rb_gv_get = WindowsLoader.GetFuncByName<Delegate_rb_gv_get>(nameof(rb_gv_get), _ApiDll);


                rb_class_name = WindowsLoader.GetFuncByName<Delegate_rb_class_name>(nameof(rb_class_name), _ApiDll);


                rb_errinfo = WindowsLoader.GetFuncByName<Delegate_rb_errinfo>(nameof(rb_errinfo), _ApiDll);
                rb_set_errinfo = WindowsLoader.GetFuncByName<Delegate_rb_set_errinfo>(nameof(rb_set_errinfo), _ApiDll);

            }
            
            internal delegate void Delegate_ruby_sysinit(ref int argc, ref IntPtr argv);
            internal static Delegate_ruby_sysinit ruby_sysinit;

            internal delegate void Delegate_ruby_setup();
            internal static Delegate_ruby_setup ruby_setup;
            internal delegate void Delegate_ruby_init();
            internal static Delegate_ruby_init ruby_init;
            internal delegate void Delegate_ruby_init_loadpath();
            internal static Delegate_ruby_init_loadpath ruby_init_loadpath;
            internal delegate void Delegate_ruby_finalize();
            internal static Delegate_ruby_finalize ruby_finalize;

            internal delegate void Delegate_ruby_show_version();
            internal static Delegate_ruby_show_version ruby_show_version;
            
            internal delegate VALUE Delegate_rb_eval_string(StrPtr str);
            internal static Delegate_rb_eval_string rb_eval_string;
            internal delegate VALUE Delegate_rb_eval_string_protect(StrPtr str, out int state);
            internal static Delegate_rb_eval_string_protect rb_eval_string_protect;
            internal delegate VALUE Delegate_rb_p(VALUE obj);
            internal static Delegate_rb_p rb_p;
            internal delegate VALUE Delegate_rb_io_puts(int argc, IntPtr[] argv, VALUE io);
            internal static Delegate_rb_io_puts rb_io_puts;
            

            internal delegate VALUE Delegate_rb_define_module(string name);
            internal static Delegate_rb_define_module rb_define_module;

            internal delegate void Delegate_rb_define_module_function(VALUE klass, string mid, Delegate func, int arity);
            internal static Delegate_rb_define_module_function rb_define_module_function;


            internal delegate VALUE Delegate_rb_obj_as_string(VALUE obj);
            internal static Delegate_rb_obj_as_string rb_obj_as_string;
            internal delegate VALUE Delegate_rb_obj_class(VALUE obj);
            internal static Delegate_rb_obj_class rb_obj_class;
            internal delegate string Delegate_rb_obj_classname(VALUE obj);
            internal static Delegate_rb_obj_classname rb_obj_classname;


            internal delegate IntPtr Delegate_rb_string_value_cstr(ref VALUE str);
            internal static Delegate_rb_string_value_cstr rb_string_value_cstr;

            internal delegate VALUE Delegate_rb_str_new_cstr(StrPtr str);
            internal static Delegate_rb_str_new_cstr rb_str_new_cstr;
            internal delegate VALUE Delegate_rb_utf8_str_new_cstr(StrPtr str);
            internal static Delegate_rb_utf8_str_new_cstr rb_utf8_str_new_cstr;
            

            internal delegate VALUE Delegate_rb_hash(VALUE obj);
            internal static Delegate_rb_hash rb_hash;

            internal delegate nint Delegate_rb_num2int(VALUE num);
            internal static Delegate_rb_num2int rb_num2int;

            internal delegate VALUE Delegate_rb_gv_get(string name);
            internal static Delegate_rb_gv_get rb_gv_get;
            

            internal delegate VALUE Delegate_rb_class_name(VALUE obj);
            internal static Delegate_rb_class_name rb_class_name;


            internal delegate VALUE Delegate_rb_errinfo();
            internal static Delegate_rb_errinfo rb_errinfo;
            internal delegate void Delegate_rb_set_errinfo(VALUE err);
            internal static Delegate_rb_set_errinfo rb_set_errinfo;

        }

        #endregion

    }


    /// <summary>
    /// 编码
    /// </summary>
    internal static class Encodings
    {
        public static System.Text.Encoding UTF8 = new UTF8Encoding(false, true);
        public static System.Text.Encoding UTF16 = new UnicodeEncoding(!BitConverter.IsLittleEndian, false, true);
        public static System.Text.Encoding UTF32 = new UTF32Encoding(!BitConverter.IsLittleEndian, false, true);
    }


    /// <summary>
    /// 字符串指针
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct StrPtr : IDisposable
    {
        public IntPtr Pointer { get; set; }
        unsafe byte* Bytes => (byte*)Pointer;

        public StrPtr(string value) : this(value, Encodings.UTF8) { }

        public unsafe StrPtr(string value, Encoding encoding)
        {
            // 将字符串使用指定编码转换为字节数组（不包含 '\0' 终止符）
            var Bytes = encoding.GetBytes(value);

            // 为字符串数据在非托管内存中分配空间，比实际字节数多分配 1 个字节，用于存放结尾的 '\0'（C 风格字符串终止符）
            Pointer = Marshal.AllocHGlobal(checked(Bytes.Length + 1));

            try
            {
                // 将托管字节数组内容复制到非托管内存（Pointer 指向的位置）
                Marshal.Copy(Bytes, 0, Pointer, Bytes.Length);

                // 在非托管内存的最后一个字节写入 0（即 '\0'），确保生成的字符串以 null 结尾，兼容 C/C++、Python 等原生接口
                this.Bytes[Bytes.Length] = 0;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public unsafe string? ToString(Encoding encoding)
        {
            if (encoding is null) throw new ArgumentNullException(nameof(encoding));
            if (this.Pointer == IntPtr.Zero) return null;

            //return encoding.GetString((byte*)this.Pointer, byteCount: checked((int)this.ByteCount));
            return StrPtr.PtrToString(this.Pointer, encoding.IsSingleByte);
        }

        public unsafe nuint ByteCount
        {
            get
            {
                if (this.Pointer == IntPtr.Zero) throw new NullReferenceException();

                nuint zeroIndex = 0;
                while (this.Bytes[zeroIndex] != 0)
                {
                    zeroIndex++;
                }
                return zeroIndex;
            }
        }

        public void Dispose()
        {
            if (Pointer == IntPtr.Zero) return;
            Marshal.FreeHGlobal(Pointer);
            Pointer = IntPtr.Zero;
        }

        /// <summary>
        /// 指针转字符串
        /// </summary>
        /// <param name="Ptr"></param>
        /// <returns></returns>
        public static string PtrToString(IntPtr Ptr, bool IsWChar = false)
        {
            int Length = 0;
            if (!IsWChar)
            {
                while (Marshal.ReadByte(Ptr, Length) != 0) Length++; // 按 1 个字节长度读取
            }
            else
            {
                //return Marshal.PtrToStringUni(Ptr);
                while (Marshal.ReadInt16(Ptr, Length) != 0) Length += 2; // 按 2 个字节长度读取
            }
            byte[] Bytes = new byte[Length];
            Marshal.Copy(Ptr, Bytes, 0, Bytes.Length);

            var encoding = !IsWChar ? Encodings.UTF8 : Encodings.UTF16;

            return encoding.GetString(Bytes);
        }

    }

}
