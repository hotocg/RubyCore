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
        #region 配置

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

        #endregion

        #region 生命周期

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

            if (_ApiVersion >= new Version("1.9"))
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

        #endregion

        #region 加载路径

        /// <summary>
        /// 添加 Ruby 加载路径
        /// </summary>
        internal static void AddLoadPath(string directory)
        {
            var rubyPath = directory.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
            using (var pathPtr = new StrPtr(rubyPath))
            {
                ruby_incpush(pathPtr.Pointer);
            }

            // 旧实现：直接操作 Ruby 的 $LOAD_PATH
            // var loadPath = rb_gv_get("$LOAD_PATH");
            // var pathValue = rb_utf8_str_new_cstr(rubyPath);
            // var exists = rb_funcallv(loadPath, rb_intern("include?"), pathValue);
            //
            // if (!new RbBool(exists).Value)
            // {
            //     rb_funcallv(loadPath, rb_intern("unshift"), pathValue);
            // }
        }

        /// <summary>
        /// 获取 Ruby 加载路径
        /// </summary>
        internal static VALUE LoadPath => rb_gv_get("$LOAD_PATH");

        #endregion

        #region 类型辅助

        /// <summary>
        /// 获取 Ruby 对象的类名
        /// </summary>
        internal static string GetClassName(VALUE value)
        {
            var klass = rb_obj_class(value);
            var name = rb_class_name(klass);
            var ptr = rb_string_value_cstr(ref name);
            return StrPtr.PtrToString(ptr);
        }

        #endregion

        #region Api

        #region 引擎
        /// <summary>
        /// 初始化 Ruby 运行时参数
        /// </summary>
        internal static void ruby_sysinit(ref int argc, ref IntPtr argv) => Delegates.ruby_sysinit(ref argc, ref argv);

        /// <summary>
        /// 设置 Ruby 运行时
        /// </summary>
        internal static void ruby_setup() => Delegates.ruby_setup();

        /// <summary>
        /// 初始化 Ruby 运行时
        /// </summary>
        internal static void ruby_init() => Delegates.ruby_init();

        /// <summary>
        /// 初始化标准库加载路径
        /// </summary>
        internal static void ruby_init_loadpath() => Delegates.ruby_init_loadpath();

        /// <summary>
        /// 添加 Ruby require 搜索路径
        /// </summary>
        internal static void ruby_incpush(IntPtr path) => Delegates.ruby_incpush(path);

        /// <summary>
        /// 结束 Ruby 运行时
        /// </summary>
        internal static void ruby_finalize() => Delegates.ruby_finalize();

        /// <summary>
        /// 输出 Ruby 版本信息
        /// </summary>
        internal static void ruby_show_version() => Delegates.ruby_show_version();

        /// <summary>
        /// 执行 Ruby 脚本
        /// </summary>
        internal static VALUE rb_eval_string(StrPtr str) => Delegates.rb_eval_string(str);

        /// <summary>
        /// 以保护模式执行 Ruby 脚本
        /// </summary>
        internal static VALUE rb_eval_string_protect(StrPtr str, out int state) => Delegates.rb_eval_string_protect(str, out state);

        /// <summary>
        /// 以保护模式加载 Ruby 脚本文件
        /// </summary>
        internal static void rb_load_protect(VALUE filePath, int wrap, out int state) => Delegates.rb_load_protect(filePath, wrap, out state);

        /// <summary>
        /// 以保护模式调用 Ruby C API
        /// </summary>
        internal static VALUE rb_protect(Delegates.Delegate_rb_protect_func proc, VALUE data, out int state) => Delegates.rb_protect(proc, data, out state);

        /// <summary>
        /// 调用 Ruby C API rb_require
        /// </summary>
        internal static VALUE rb_require(IntPtr feature) => Delegates.rb_require(feature);

        /// <summary>
        /// 以保护模式加载 Ruby feature
        /// </summary>
        internal static VALUE rb_require_protect(VALUE feature, out int state) => rb_protect(RequireProtectFunc, feature, out state);

        /// <summary>
        /// 调用 Ruby 的 p 输出对象
        /// </summary>
        internal static void rb_p(VALUE obj) => Delegates.rb_p(obj);

        /// <summary>
        /// 向 Ruby IO 写入多行输出
        /// </summary>
        internal static void rb_io_puts(int argc, IntPtr[] argv, VALUE io) => Delegates.rb_io_puts(argc, argv, io);
        
        #endregion

        #region 模块
        /// <summary>
        /// 定义模块
        /// </summary>
        /// <param name="name">模块名称</param>
        /// <returns>模块对象</returns>
        internal static VALUE rb_define_module(string name) => Delegates.rb_define_module(name);

        /// <summary>
        /// 定义模块函数
        /// </summary>
        /// <param name="klass">模块</param>
        /// <param name="mid">函数名称</param>
        /// <param name="func">函数委托</param>
        /// <param name="arity">参数数量</param>
        internal static void rb_define_module_function(VALUE klass, string mid, Delegate func, int arity) => Delegates.rb_define_module_function(klass, mid, func, arity);

        /// <summary>
        /// 定义全局函数
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <param name="func">函数委托</param>
        /// <param name="arity">参数数量</param>
        internal static void rb_define_global_function(string name, Delegate func, int arity) => Delegates.rb_define_global_function(name, func, arity);

        /// <summary>
        /// 获取顶层 Object 类
        /// </summary>
        internal static VALUE rb_cObject() => WindowsLoader.GetValueByName("rb_cObject", _ApiDll);

        /// <summary>
        /// 获取 Ruby 常量
        /// </summary>
        internal static VALUE rb_const_get(VALUE klass, ID id) => Delegates.rb_const_get(klass, id);

        /// <summary>
        /// 设置 Ruby 常量
        /// </summary>
        internal static void rb_const_set(VALUE klass, ID id, VALUE value) => Delegates.rb_const_set(klass, id, value);

        /// <summary>
        /// 以保护模式获取 Ruby 常量
        /// </summary>
        internal static VALUE rb_const_get_protect(VALUE klass, VALUE name, out int state)
        {
            var args = rb_ary_new();
            rb_ary_push(args, klass);
            rb_ary_push(args, name);

            return rb_protect(ConstGetProtectFunc, args, out state);
        }

        /// <summary>
        /// 以保护模式设置 Ruby 常量
        /// </summary>
        internal static VALUE rb_const_set_protect(VALUE klass, VALUE name, VALUE value, out int state)
        {
            var args = rb_ary_new();
            rb_ary_push(args, klass);
            rb_ary_push(args, name);
            rb_ary_push(args, value);

            return rb_protect(ConstSetProtectFunc, args, out state);
        }

        #endregion

        #region 对象
        /// <summary>
        /// Ruby 对象转字符串
        /// </summary>
        internal static VALUE rb_obj_as_string(VALUE obj) => Delegates.rb_obj_as_string(obj);

        /// <summary>
        /// 获取对象类别
        /// </summary>
        internal static VALUE rb_obj_class(VALUE obj) => Delegates.rb_obj_class(obj);
        //internal static string rb_obj_classname(VALUE obj) => Delegates.rb_obj_classname(obj);

        /// <summary>
        /// 获取 Ruby 对象哈希值
        /// </summary>
        internal static VALUE rb_hash(VALUE obj) => Delegates.rb_hash(obj);

        #endregion

        #region 字符串
        /// <summary>
        /// Ruby 字符串对象转 C 字符串
        /// </summary>
        internal static IntPtr rb_string_value_cstr(ref VALUE str) => Delegates.rb_string_value_cstr(ref str);

        /// <summary>
        /// 创建 Ruby 字符串
        /// </summary>
        internal static VALUE rb_str_new_cstr(string str) => Delegates.rb_str_new_cstr(new(str));

        /// <summary>
        /// 创建 UTF-8 Ruby 字符串
        /// </summary>
        internal static VALUE rb_utf8_str_new_cstr(string str) => Delegates.rb_utf8_str_new_cstr(new(str));

        #endregion

        #region 数值
        /// <summary>
        /// Ruby 数值转 32 位整数
        /// </summary>
        internal static int rb_num2int(VALUE num) => Delegates.rb_num2int(num);

        /// <summary>
        /// Ruby 数值转 64 位整数
        /// </summary>
        internal static long rb_num2ll(VALUE num) => Delegates.rb_num2ll(num);

        /// <summary>
        /// Ruby 数值转双精度浮点数
        /// </summary>
        internal static double rb_num2dbl(VALUE num) => Delegates.rb_num2dbl(num);

        /// <summary>
        /// 32 位整数转 Ruby 整数
        /// </summary>
        internal static VALUE rb_int2inum(int num) => Delegates.rb_int2inum(num);

        /// <summary>
        /// 64 位整数转 Ruby 整数
        /// </summary>
        internal static VALUE rb_ll2inum(long num) => Delegates.rb_ll2inum(num);

        /// <summary>
        /// 创建 Ruby 浮点数
        /// </summary>
        internal static VALUE rb_float_new(double num) => Delegates.rb_float_new(num);

        #endregion

        #region 全局变量
        /// <summary>
        /// 获取全局变量
        /// </summary>
        internal static VALUE rb_gv_get(string name) => Delegates.rb_gv_get(name);

        /// <summary>
        /// 设置全局变量
        /// </summary>
        internal static VALUE rb_gv_set(string name, VALUE value) => Delegates.rb_gv_set(name, value);

        #endregion

        #region 调用
        /// <summary>
        /// 查找或创建指定名称的符号
        /// </summary>
        internal static ID rb_intern(string name) => Delegates.rb_intern(name);

        /// <summary>
        /// ID 转 Ruby Symbol 对象
        /// </summary>
        internal static VALUE rb_id2sym(ID id) => Delegates.rb_id2sym(id);

        /// <summary>
        /// Ruby Symbol 对象转 ID
        /// </summary>
        internal static ID rb_sym2id(VALUE symbol) => Delegates.rb_sym2id(symbol);

        /// <summary>
        /// 调用对象方法
        /// </summary>
        internal static VALUE rb_funcall(VALUE recv, ID mid, params VALUE[] argv) => Delegates.rb_funcall(recv, mid, argv.Length, argv);

        /// <summary>
        /// 使用参数数组调用对象方法
        /// </summary>
        internal static VALUE rb_funcallv(VALUE recv, ID mid, params VALUE[] argv)
        {
            if (argv is null || argv.Length == 0)
            {
                return Delegates.rb_funcallv(recv, mid, 0, IntPtr.Zero);
            }

            fixed (VALUE* argvPtr = argv)
            {
                return Delegates.rb_funcallv(recv, mid, argv.Length, (IntPtr)argvPtr);
            }
        }

        /// <summary>
        /// 以保护模式调用对象方法
        /// </summary>
        internal static VALUE rb_funcallv_protect(VALUE recv, ID mid, VALUE[] argv, out int state)
        {
            var data = new FuncallProtectData(recv, mid, argv ?? new VALUE[0]);
            var handle = GCHandle.Alloc(data);

            try
            {
                return rb_protect(FuncallProtectFunc, new VALUE(GCHandle.ToIntPtr(handle)), out state);
            }
            finally
            {
                handle.Free();
            }
        }

        #endregion

        #region 异常
        /// <summary>
        /// 获取异常信息
        /// </summary>
        internal static VALUE rb_errinfo() => Delegates.rb_errinfo();

        /// <summary>
        /// 设置异常信息
        /// </summary>
        internal static void rb_set_errinfo(VALUE err) => Delegates.rb_set_errinfo(err);

        /// <summary>
        /// 抛出 Ruby 异常对象
        /// </summary>
        internal static void rb_exc_raise(VALUE err) => Delegates.rb_exc_raise(err);

        /// <summary>
        /// 创建 Ruby RuntimeError 异常对象
        /// </summary>
        internal static VALUE rb_new_runtime_error(string message)
        {
            var exceptionClass = WindowsLoader.GetValueByName("rb_eRuntimeError", _ApiDll);
            var messageValue = rb_utf8_str_new_cstr(message ?? string.Empty);
            return rb_funcallv(exceptionClass, rb_intern("new"), messageValue);
        }

        #endregion

        #region 数组
        /// <summary>
        /// 创建 Ruby 数组
        /// </summary>
        internal static VALUE rb_ary_new() => Delegates.rb_ary_new();

        /// <summary>
        /// 向 Ruby 数组追加元素
        /// </summary>
        internal static VALUE rb_ary_push(VALUE array, VALUE value) => Delegates.rb_ary_push(array, value);

        /// <summary>
        /// 获取 Ruby 数组指定索引的元素
        /// </summary>
        internal static VALUE rb_ary_entry(VALUE array, long index) => Delegates.rb_ary_entry(array, index);

        /// <summary>
        /// 设置 Ruby 数组指定索引的元素
        /// </summary>
        internal static void rb_ary_store(VALUE array, long index, VALUE value) => Delegates.rb_ary_store(array, index, value);
        #endregion

        #region 哈希
        /// <summary>
        /// 创建 Ruby 哈希
        /// </summary>
        internal static VALUE rb_hash_new() => Delegates.rb_hash_new();

        /// <summary>
        /// 获取 Ruby 哈希指定键的值
        /// </summary>
        internal static VALUE rb_hash_aref(VALUE hash, VALUE key) => Delegates.rb_hash_aref(hash, key);

        /// <summary>
        /// 设置 Ruby 哈希指定键的值
        /// </summary>
        internal static VALUE rb_hash_aset(VALUE hash, VALUE key, VALUE value) => Delegates.rb_hash_aset(hash, key, value);

        /// <summary>
        /// 判断 Ruby 哈希是否包含指定键
        /// </summary>
        internal static VALUE rb_hash_has_key(VALUE hash, VALUE key) => Delegates.rb_hash_has_key(hash, key);

        /// <summary>
        /// 获取 Ruby 哈希键数组
        /// </summary>
        internal static VALUE rb_hash_keys(VALUE hash) => Delegates.rb_hash_keys(hash);

        /// <summary>
        /// 获取 Ruby 哈希值数组
        /// </summary>
        internal static VALUE rb_hash_values(VALUE hash) => Delegates.rb_hash_values(hash);

        #endregion

        #region 类
        /// <summary>
        /// 获取类名
        /// </summary>
        internal static VALUE rb_class_name(VALUE obj) => Delegates.rb_class_name(obj);

        #endregion


        #endregion

        #region 保护回调

        private static readonly Delegates.Delegate_rb_protect_func RequireProtectFunc = data => {
            var feature = data;
            var featurePtr = rb_string_value_cstr(ref feature);
            return rb_require(featurePtr);
        };

        private static readonly Delegates.Delegate_rb_protect_func ConstGetProtectFunc = data => {
            var klass = rb_ary_entry(data, 0);
            var name = rb_ary_entry(data, 1);
            return rb_const_get(klass, rb_sym2id(name));
        };

        private static readonly Delegates.Delegate_rb_protect_func ConstSetProtectFunc = data => {
            var klass = rb_ary_entry(data, 0);
            var name = rb_ary_entry(data, 1);
            var value = rb_ary_entry(data, 2);
            rb_const_set(klass, rb_sym2id(name), value);
            return value;
        };

        private static readonly Delegates.Delegate_rb_protect_func FuncallProtectFunc = data => {
            var handle = GCHandle.FromIntPtr(data.Pointer);
            var callData = (FuncallProtectData)handle.Target;
            return rb_funcallv(callData.Recv, callData.Mid, callData.Args);
        };

        private sealed class FuncallProtectData
        {
            internal readonly VALUE Recv;
            internal readonly ID Mid;
            internal readonly VALUE[] Args;

            internal FuncallProtectData(VALUE recv, ID mid, VALUE[] args)
            {
                Recv = recv;
                Mid = mid;
                Args = args;
            }
        }

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

                #region 引擎
                ruby_sysinit = WindowsLoader.GetFuncByName<Delegate_ruby_sysinit>(nameof(ruby_sysinit), _ApiDll);
                ruby_setup = WindowsLoader.GetFuncByName<Delegate_ruby_setup>(nameof(ruby_setup), _ApiDll);
                ruby_init = WindowsLoader.GetFuncByName<Delegate_ruby_init>(nameof(ruby_init), _ApiDll);
                ruby_init_loadpath = WindowsLoader.GetFuncByName<Delegate_ruby_init_loadpath>(nameof(ruby_init_loadpath), _ApiDll);
                ruby_incpush = WindowsLoader.GetFuncByName<Delegate_ruby_incpush>(nameof(ruby_incpush), _ApiDll);
                ruby_finalize = WindowsLoader.GetFuncByName<Delegate_ruby_finalize>(nameof(ruby_finalize), _ApiDll);
                ruby_show_version = WindowsLoader.GetFuncByName<Delegate_ruby_show_version>(nameof(ruby_show_version), _ApiDll);
                rb_eval_string = WindowsLoader.GetFuncByName<Delegate_rb_eval_string>(nameof(rb_eval_string), _ApiDll);
                rb_eval_string_protect = WindowsLoader.GetFuncByName<Delegate_rb_eval_string_protect>(nameof(rb_eval_string_protect), _ApiDll);
                rb_load_protect = WindowsLoader.GetFuncByName<Delegate_rb_load_protect>(nameof(rb_load_protect), _ApiDll);
                rb_protect = WindowsLoader.GetFuncByName<Delegate_rb_protect>(nameof(rb_protect), _ApiDll);
                rb_require = WindowsLoader.GetFuncByName<Delegate_rb_require>(nameof(rb_require), _ApiDll);
                rb_p = WindowsLoader.GetFuncByName<Delegate_rb_p>(nameof(rb_p), _ApiDll);
                rb_io_puts = WindowsLoader.GetFuncByName<Delegate_rb_io_puts>(nameof(rb_io_puts), _ApiDll);
                #endregion

                #region 模块
                rb_define_module = WindowsLoader.GetFuncByName<Delegate_rb_define_module>(nameof(rb_define_module), _ApiDll);
                rb_define_module_function = WindowsLoader.GetFuncByName<Delegate_rb_define_module_function>(nameof(rb_define_module_function), _ApiDll);
                rb_define_global_function = WindowsLoader.GetFuncByName<Delegate_rb_define_global_function>(nameof(rb_define_global_function), _ApiDll);
                rb_const_get = WindowsLoader.GetFuncByName<Delegate_rb_const_get>(nameof(rb_const_get), _ApiDll);
                rb_const_set = WindowsLoader.GetFuncByName<Delegate_rb_const_set>(nameof(rb_const_set), _ApiDll);
                #endregion

                #region 对象
                rb_obj_as_string = WindowsLoader.GetFuncByName<Delegate_rb_obj_as_string>(nameof(rb_obj_as_string), _ApiDll);
                rb_obj_class = WindowsLoader.GetFuncByName<Delegate_rb_obj_class>(nameof(rb_obj_class), _ApiDll);
                rb_obj_classname = WindowsLoader.GetFuncByName<Delegate_rb_obj_classname>(nameof(rb_obj_classname), _ApiDll);
                rb_hash = WindowsLoader.GetFuncByName<Delegate_rb_hash>(nameof(rb_hash), _ApiDll);
                #endregion

                #region 字符串
                rb_string_value_cstr = WindowsLoader.GetFuncByName<Delegate_rb_string_value_cstr>(nameof(rb_string_value_cstr), _ApiDll);
                rb_str_new_cstr = WindowsLoader.GetFuncByName<Delegate_rb_str_new_cstr>(nameof(rb_str_new_cstr), _ApiDll);
                rb_utf8_str_new_cstr = WindowsLoader.GetFuncByName<Delegate_rb_utf8_str_new_cstr>(nameof(rb_utf8_str_new_cstr), _ApiDll);
                #endregion

                #region 数值
                rb_num2int = WindowsLoader.GetFuncByName<Delegate_rb_num2int>(nameof(rb_num2int), _ApiDll);
                rb_num2ll = WindowsLoader.GetFuncByName<Delegate_rb_num2ll>(nameof(rb_num2ll), _ApiDll);
                rb_num2dbl = WindowsLoader.GetFuncByName<Delegate_rb_num2dbl>(nameof(rb_num2dbl), _ApiDll);
                rb_int2inum = WindowsLoader.GetFuncByName<Delegate_rb_int2inum>(nameof(rb_int2inum), _ApiDll);
                rb_ll2inum = WindowsLoader.GetFuncByName<Delegate_rb_ll2inum>(nameof(rb_ll2inum), _ApiDll);
                rb_float_new = WindowsLoader.GetFuncByName<Delegate_rb_float_new>(nameof(rb_float_new), _ApiDll);
                #endregion

                #region 全局变量
                rb_gv_get = WindowsLoader.GetFuncByName<Delegate_rb_gv_get>(nameof(rb_gv_get), _ApiDll);
                rb_gv_set = WindowsLoader.GetFuncByName<Delegate_rb_gv_set>(nameof(rb_gv_set), _ApiDll);
                #endregion

                #region 调用
                rb_intern = WindowsLoader.GetFuncByName<Delegate_rb_intern>(nameof(rb_intern), _ApiDll);
                rb_id2sym = WindowsLoader.GetFuncByName<Delegate_rb_id2sym>(nameof(rb_id2sym), _ApiDll);
                rb_sym2id = WindowsLoader.GetFuncByName<Delegate_rb_sym2id>(nameof(rb_sym2id), _ApiDll);
                rb_funcall = WindowsLoader.GetFuncByName<Delegate_rb_funcall>(nameof(rb_funcall), _ApiDll);
                rb_funcallv = WindowsLoader.GetFuncByName<Delegate_rb_funcallv>(nameof(rb_funcallv), _ApiDll);
                #endregion

                #region 异常
                rb_errinfo = WindowsLoader.GetFuncByName<Delegate_rb_errinfo>(nameof(rb_errinfo), _ApiDll);
                rb_set_errinfo = WindowsLoader.GetFuncByName<Delegate_rb_set_errinfo>(nameof(rb_set_errinfo), _ApiDll);
                rb_exc_raise = WindowsLoader.GetFuncByName<Delegate_rb_exc_raise>(nameof(rb_exc_raise), _ApiDll);
                #endregion

                #region 数组
                rb_ary_new = WindowsLoader.GetFuncByName<Delegate_rb_ary_new>(nameof(rb_ary_new), _ApiDll);
                rb_ary_push = WindowsLoader.GetFuncByName<Delegate_rb_ary_push>(nameof(rb_ary_push), _ApiDll);
                rb_ary_entry = WindowsLoader.GetFuncByName<Delegate_rb_ary_entry>(nameof(rb_ary_entry), _ApiDll);
                rb_ary_store = WindowsLoader.GetFuncByName<Delegate_rb_ary_store>(nameof(rb_ary_store), _ApiDll);
                #endregion

                #region 哈希
                rb_hash_new = WindowsLoader.GetFuncByName<Delegate_rb_hash_new>(nameof(rb_hash_new), _ApiDll);
                rb_hash_aref = WindowsLoader.GetFuncByName<Delegate_rb_hash_aref>(nameof(rb_hash_aref), _ApiDll);
                rb_hash_aset = WindowsLoader.GetFuncByName<Delegate_rb_hash_aset>(nameof(rb_hash_aset), _ApiDll);
                rb_hash_has_key = WindowsLoader.GetFuncByName<Delegate_rb_hash_has_key>(nameof(rb_hash_has_key), _ApiDll);
                rb_hash_keys = WindowsLoader.GetFuncByName<Delegate_rb_hash_keys>(nameof(rb_hash_keys), _ApiDll);
                rb_hash_values = WindowsLoader.GetFuncByName<Delegate_rb_hash_values>(nameof(rb_hash_values), _ApiDll);
                #endregion

                #region 类
                rb_class_name = WindowsLoader.GetFuncByName<Delegate_rb_class_name>(nameof(rb_class_name), _ApiDll);
                #endregion
            }

            #region 引擎
            internal delegate void Delegate_ruby_sysinit(ref int argc, ref IntPtr argv);
            internal static Delegate_ruby_sysinit ruby_sysinit;
            internal delegate void Delegate_ruby_setup();
            internal static Delegate_ruby_setup ruby_setup;
            internal delegate void Delegate_ruby_init();
            internal static Delegate_ruby_init ruby_init;
            internal delegate void Delegate_ruby_init_loadpath();
            internal static Delegate_ruby_init_loadpath ruby_init_loadpath;
            internal delegate void Delegate_ruby_incpush(IntPtr path);
            internal static Delegate_ruby_incpush ruby_incpush;
            internal delegate void Delegate_ruby_finalize();
            internal static Delegate_ruby_finalize ruby_finalize;

            internal delegate void Delegate_ruby_show_version();
            internal static Delegate_ruby_show_version ruby_show_version;
            internal delegate VALUE Delegate_rb_eval_string(StrPtr str);
            internal static Delegate_rb_eval_string rb_eval_string;
            internal delegate VALUE Delegate_rb_eval_string_protect(StrPtr str, out int state);
            internal static Delegate_rb_eval_string_protect rb_eval_string_protect;
            internal delegate void Delegate_rb_load_protect(VALUE filePath, int wrap, out int state);
            internal static Delegate_rb_load_protect rb_load_protect;
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate VALUE Delegate_rb_protect_func(VALUE data);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate VALUE Delegate_rb_protect(Delegate_rb_protect_func proc, VALUE data, out int state);
            internal static Delegate_rb_protect rb_protect;
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate VALUE Delegate_rb_require(IntPtr feature);
            internal static Delegate_rb_require rb_require;
            internal delegate VALUE Delegate_rb_p(VALUE obj);
            internal static Delegate_rb_p rb_p;
            internal delegate VALUE Delegate_rb_io_puts(int argc, IntPtr[] argv, VALUE io);
            internal static Delegate_rb_io_puts rb_io_puts;
            #endregion

            #region 模块
            internal delegate VALUE Delegate_rb_define_module(string name);
            internal static Delegate_rb_define_module rb_define_module;
            internal delegate void Delegate_rb_define_module_function(VALUE klass, string mid, Delegate func, int arity);
            internal static Delegate_rb_define_module_function rb_define_module_function;
            internal delegate void Delegate_rb_define_global_function(string name, Delegate func, int arity);
            internal static Delegate_rb_define_global_function rb_define_global_function;
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate VALUE Delegate_rb_const_get(VALUE klass, ID id);
            internal static Delegate_rb_const_get rb_const_get;
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate void Delegate_rb_const_set(VALUE klass, ID id, VALUE value);
            internal static Delegate_rb_const_set rb_const_set;
            #endregion

            #region 对象
            internal delegate VALUE Delegate_rb_obj_as_string(VALUE obj);
            internal static Delegate_rb_obj_as_string rb_obj_as_string;
            internal delegate VALUE Delegate_rb_obj_class(VALUE obj);
            internal static Delegate_rb_obj_class rb_obj_class;
            internal delegate string Delegate_rb_obj_classname(VALUE obj);
            internal static Delegate_rb_obj_classname rb_obj_classname;
            internal delegate VALUE Delegate_rb_hash(VALUE obj);
            internal static Delegate_rb_hash rb_hash;
            #endregion

            #region 字符串
            internal delegate IntPtr Delegate_rb_string_value_cstr(ref VALUE str);
            internal static Delegate_rb_string_value_cstr rb_string_value_cstr;
            internal delegate VALUE Delegate_rb_str_new_cstr(StrPtr str);
            internal static Delegate_rb_str_new_cstr rb_str_new_cstr;
            internal delegate VALUE Delegate_rb_utf8_str_new_cstr(StrPtr str);
            internal static Delegate_rb_utf8_str_new_cstr rb_utf8_str_new_cstr;
            #endregion

            #region 数值
            internal delegate int Delegate_rb_num2int(VALUE num);
            internal static Delegate_rb_num2int rb_num2int;
            internal delegate long Delegate_rb_num2ll(VALUE num);
            internal static Delegate_rb_num2ll rb_num2ll;
            internal delegate double Delegate_rb_num2dbl(VALUE num);
            internal static Delegate_rb_num2dbl rb_num2dbl;
            internal delegate VALUE Delegate_rb_int2inum(int num);
            internal static Delegate_rb_int2inum rb_int2inum;
            internal delegate VALUE Delegate_rb_ll2inum(long num);
            internal static Delegate_rb_ll2inum rb_ll2inum;
            internal delegate VALUE Delegate_rb_float_new(double num);
            internal static Delegate_rb_float_new rb_float_new;
            #endregion

            #region 全局变量
            internal delegate VALUE Delegate_rb_gv_get(string name);
            internal static Delegate_rb_gv_get rb_gv_get;
            internal delegate VALUE Delegate_rb_gv_set(string name, VALUE value);
            internal static Delegate_rb_gv_set rb_gv_set;
            #endregion

            #region 调用
            internal delegate ID Delegate_rb_intern(string name);
            internal static Delegate_rb_intern rb_intern;
            internal delegate VALUE Delegate_rb_id2sym(ID id);
            internal static Delegate_rb_id2sym rb_id2sym;
            internal delegate ID Delegate_rb_sym2id(VALUE symbol);
            internal static Delegate_rb_sym2id rb_sym2id;
            internal delegate VALUE Delegate_rb_funcall(VALUE recv, ID mid, int argc, params VALUE[] argv);
            internal static Delegate_rb_funcall rb_funcall;
            internal delegate VALUE Delegate_rb_funcallv(VALUE recv, ID mid, int argc, IntPtr argv);
            internal static Delegate_rb_funcallv rb_funcallv;
            #endregion

            #region 异常
            internal delegate VALUE Delegate_rb_errinfo();
            internal static Delegate_rb_errinfo rb_errinfo;
            internal delegate void Delegate_rb_set_errinfo(VALUE err);
            internal static Delegate_rb_set_errinfo rb_set_errinfo;
            internal delegate void Delegate_rb_exc_raise(VALUE err);
            internal static Delegate_rb_exc_raise rb_exc_raise;
            #endregion

            #region 数组
            internal delegate VALUE Delegate_rb_ary_new();
            internal static Delegate_rb_ary_new rb_ary_new;
            internal delegate VALUE Delegate_rb_ary_push(VALUE array, VALUE value);
            internal static Delegate_rb_ary_push rb_ary_push;
            internal delegate VALUE Delegate_rb_ary_entry(VALUE array, long index);
            internal static Delegate_rb_ary_entry rb_ary_entry;
            internal delegate void Delegate_rb_ary_store(VALUE array, long index, VALUE value);
            internal static Delegate_rb_ary_store rb_ary_store;
            #endregion

            #region 哈希
            internal delegate VALUE Delegate_rb_hash_new();
            internal static Delegate_rb_hash_new rb_hash_new;
            internal delegate VALUE Delegate_rb_hash_aref(VALUE hash, VALUE key);
            internal static Delegate_rb_hash_aref rb_hash_aref;
            internal delegate VALUE Delegate_rb_hash_aset(VALUE hash, VALUE key, VALUE value);
            internal static Delegate_rb_hash_aset rb_hash_aset;
            internal delegate VALUE Delegate_rb_hash_has_key(VALUE hash, VALUE key);
            internal static Delegate_rb_hash_has_key rb_hash_has_key;
            internal delegate VALUE Delegate_rb_hash_keys(VALUE hash);
            internal static Delegate_rb_hash_keys rb_hash_keys;
            internal delegate VALUE Delegate_rb_hash_values(VALUE hash);
            internal static Delegate_rb_hash_values rb_hash_values;
            #endregion

            #region 类
            internal delegate VALUE Delegate_rb_class_name(VALUE obj);
            internal static Delegate_rb_class_name rb_class_name;
            #endregion

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
