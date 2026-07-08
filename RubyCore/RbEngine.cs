using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RubyCore
{
    public class RbEngine
    {
        #region 配置

        /// <summary>
        /// <inheritdoc cref="Runtime._ApiDll"/>
        /// </summary>
        public static string ApiDll
        {
            get => Runtime._ApiDll;
            set
            {
                Runtime._ApiDll = value;
            }
        }

        /// <summary>
        /// 获取默认的 Api DLL 路径
        /// </summary>
        /// <returns></returns>
        public static string GetDefaultApiDll(int? version = null)
        {
            var dllPath = "";

            try
            {
                string SearchApiDllFunc(string dir)
                {
                    var dllPath = "";

                    var files = Directory.GetFiles(dir, "*crt-ruby*.dll");
                    if (files.Length == 0)
                    {
                        var dir2 = Path.Combine(dir, "SketchUp");
                        if (Directory.Exists(dir2)) files = Directory.GetFiles(dir2, "*crt-ruby*.dll");
                    }

                    dllPath = files.FirstOrDefault();

                    return dllPath;
                }

                var hostPath = Process.GetCurrentProcess().MainModule?.FileName;
                var hostDir = Path.GetDirectoryName(hostPath);

                dllPath = SearchApiDllFunc(hostDir);
                if (!File.Exists(dllPath))
                {
                    var sketchUpDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SketchUp");
                    var dirs = Directory.GetDirectories(sketchUpDir, $"SketchUp {version?.ToString() ?? "*"}").OrderByDescending(d => d).ToArray();
                    if (dirs.Length > 0)
                    {
                        hostDir = dirs.First();
                        dllPath = SearchApiDllFunc(hostDir);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Api Dll Not Found: {ex}");
            }

            return dllPath;
        }

        /// <summary>
        /// <inheritdoc cref="Runtime._ApiVersion"/>
        /// </summary>
        public static Version ApiVersion => Runtime._ApiVersion;

        #endregion

        #region 生命周期

        /// <summary>
        /// <inheritdoc cref="Runtime.Initialize"/>
        /// </summary>
        public static void Initialize()
        {
            Runtime.Initialize();

        }

        /// <summary>
        /// <inheritdoc cref="Initialize()"/>
        /// </summary>
        public static void Initialize(string dllPath)
        {
            ApiDll = dllPath;
            Runtime.Initialize();
        }

        /// <summary>
        /// 显示配置信息
        /// </summary>
        public static void ShowConfigInfo()
        {
            Exec(@"
                puts RUBY_VERSION
                puts RUBY_RELEASE_DATE
                puts RUBY_PLATFORM
                puts RUBY_PATCHLEVEL
                puts RUBY_REVISION
                puts RUBY_DESCRIPTION
                puts RUBY_ENGINE
                puts
            ");
        }

        #endregion

        #region 脚本执行

        /// <summary>
        /// 执行脚本
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static RbObject Exec(string code)
        {
            //var result = Runtime.rb_eval_string(new StrPtr(code));
            var result = Runtime.rb_eval_string_protect(new StrPtr(code), out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return result.Obj;
        }

        /// <summary>
        /// <inheritdoc cref="Exec(string)"/>
        /// </summary>
        public static T Exec<T>(string code)
        {
            return Exec(code).As<T>();
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="values"></param>
        public static void Print(params object[] values)
        {
            var info = string.Join(" ", values.Select(v => v?.ToString() ?? "Nil"));
            var rbStr = new RbString(info);

            //Runtime.rb_p(rbStr.Ref);

            var rb_stdout = Runtime.rb_gv_get("$stdout");
            var argv = new IntPtr[] { rbStr.Pointer };
            //var argv = values.Select(v => new RbString(v?.ToString() ?? "Nil").Pointer).ToArray();
            Runtime.rb_io_puts(argv.Length, argv, rb_stdout);

        }

        /// <summary>
        /// 加载并执行 Ruby 脚本文件
        /// </summary>
        /// <param name="filePath">Ruby 脚本文件路径</param>
        /// <param name="wrap">是否在匿名模块中加载</param>
        public static void Load(string filePath, bool wrap = false)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("脚本文件路径不能为空", nameof(filePath));
            var fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException($"脚本文件不存在: {fullPath}", fullPath);

            var rubyPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');
            var rbPath = new RbString(rubyPath);
            Runtime.rb_load_protect(rbPath.Ref, wrap ? 1 : 0, out int state);
            if (state != 0) RbException.CatchThrowToCLR();
        }

        #endregion

        #region 加载路径

        /// <summary>
        /// 添加 Ruby feature 搜索目录
        /// <para>用于让 require 可以找到自定义库目录或宿主环境额外库目录</para>
        /// </summary>
        /// <param name="directory">Ruby 库目录</param>
        public static void AddLoadPath(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("加载目录不能为空", nameof(directory));
            var fullPath = Path.GetFullPath(directory);
            if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException($"加载目录不存在: {fullPath}");

            Runtime.AddLoadPath(fullPath);
        }

        /// <summary>
        /// 获取 Ruby feature 搜索目录
        /// </summary>
        public static string[] LoadPath => new RbArray(Runtime.LoadPath).As<string[]>();

        #endregion

        #region 全局函数

        private static void RegisterGlobalFunction(string name, Delegate del, int arity)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 全局函数名称不能为空", nameof(name));
            if (del is null) throw new ArgumentNullException(nameof(del));

            RbCallback.KeepAlive(del);
            Runtime.rb_define_global_function(name, del, arity);
        }

        /// <summary>
        /// 定义 Ruby 全局函数
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <param name="func">函数委托</param>
        public static void DefineGlobalFunction(string name, Func<RbObject, RbObject[], RbObject> func)
        {
            RegisterGlobalFunction(name, RbCallback.Create(func), -1);
        }

        /// <summary>
        /// <inheritdoc cref="DefineGlobalFunction(string, Func{RbObject, RbObject[], RbObject})"/>
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <param name="action">函数委托</param>
        public static void DefineGlobalFunction(string name, Action<RbObject, RbObject[]> action)
        {
            RegisterGlobalFunction(name, RbCallback.Create(action), -1);
        }

        /// <summary>
        /// 获取 Ruby 全局函数对象
        /// </summary>
        public static RbObject GetGlobalFunction(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 全局函数名称不能为空", nameof(name));

            return Runtime.rb_cObject().Obj.InvokeMethod("new").InvokeMethod("method", name);
        }

        /// <summary>
        /// 调用 Ruby 全局函数
        /// </summary>
        public static RbObject InvokeGlobalFunction(string name, params object[] args)
        {
            return GetGlobalFunction(name).Invoke(args);
        }

        #endregion

        #region Require 与常量

        /// <summary>
        /// 加载 Ruby feature
        /// </summary>
        /// <param name="feature">Ruby feature 名称，例如 set 或 json</param>
        /// <returns>首次加载成功返回 true，已加载过返回 false</returns>
        public static bool Require(string feature)
        {
            if (string.IsNullOrWhiteSpace(feature)) throw new ArgumentException("Ruby feature 名称不能为空", nameof(feature));

            var rbFeature = new RbString(feature);
            var result = Runtime.rb_require_protect(rbFeature.Ref, out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return new RbBool(result).Value;
        }

        /// <summary>
        /// <inheritdoc cref="Require(string)"/>
        /// </summary>
        /// <param name="feature">Ruby feature 名称，例如 set 或 json</param>
        /// <param name="constantName">加载后要获取的顶层常量名称，例如 Set 或 JSON</param>
        /// <param name="constant">获取到的 Ruby 常量对象</param>
        public static bool Require(string feature, string constantName, out RbObject constant)
        {
            var result = Require(feature);
            constant = GetConstant(constantName);
            return result;
        }

        /// <summary>
        /// <inheritdoc cref="Require(string, string, out RbObject)"/>
        /// </summary>
        public static bool Require<T>(string feature, string constantName, out T constant)
        {
            var result = Require(feature, constantName, out var rbConstant);
            constant = (T)(object)rbConstant;
            return result;
        }

        /// <summary>
        /// <inheritdoc cref="Require(string, string, out RbObject)"/>
        /// </summary>
        public static bool Require(string feature, out RbObject constant)
        {
            return Require(feature, feature, out constant);
        }

        /// <summary>
        /// <inheritdoc cref="Require(string, out RbObject)"/>
        /// </summary>
        public static bool Require<T>(string feature, out T constant)
        {
            return Require(feature, feature, out constant);
        }

        /// <summary>
        /// 获取 Ruby 顶层常量
        /// </summary>
        /// <param name="name">顶层常量名称，例如 Object、File、JSON 或自定义模块名</param>
        public static RbObject GetConstant(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 常量名称不能为空", nameof(name));

            var symbol = new RbSymbol(name);
            var result = Runtime.rb_const_get_protect(Runtime.rb_cObject(), symbol.Ref, out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return result.Obj;
        }

        #endregion

    }
}
