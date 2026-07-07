using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RubyCore
{
    public class RbEngine
    {
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

        /// <summary>
        /// <inheritdoc cref="Runtime.Initialize"/>
        /// </summary>
        public static void Initialize()
        {
            Runtime.Initialize();

        }

        /// <summary>
        /// <inheritdoc cref="Runtime.Initialize"/>
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

        #region 常用方法
        /// <summary>
        /// 执行脚本
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static RbObject Exec(string code)
        {
            Runtime.AutoInit();

            //var result = Runtime.rb_eval_string(new StrPtr(code));
            var result = Runtime.rb_eval_string_protect(new StrPtr(code), out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return result.Obj;
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="values"></param>
        public static void Print(params object[] values)
        {
            Runtime.AutoInit();

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
            Runtime.AutoInit();

            var rubyPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');
            var rbPath = new RbString(rubyPath);
            Runtime.rb_load_protect(rbPath.Ref, wrap ? 1 : 0, out int state);
            if (state != 0) RbException.CatchThrowToCLR();
        }

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
            Runtime.AutoInit();

            Runtime.AddLoadPath(fullPath);
        }

        /// <summary>
        /// 加载 Ruby feature
        /// <para>使用 rb_protect 包装 rb_require，Ruby require 失败时会转换为 CLR 异常</para>
        /// </summary>
        /// <param name="feature">Ruby feature 名称，例如 set 或 json</param>
        /// <returns>首次加载成功返回 true，已加载过返回 false</returns>
        public static bool Require(string feature)
        {
            if (string.IsNullOrWhiteSpace(feature)) throw new ArgumentException("Ruby feature 名称不能为空", nameof(feature));
            Runtime.AutoInit();

            var rbFeature = new RbString(feature);
            var result = Runtime.rb_require_protect(rbFeature.Ref, out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return new RbBool(result).Value;
        }

        /// <summary>
        /// 加载 Ruby feature 并按同名常量获取对象
        /// <para>仅适用于 feature 名称和顶层常量名称完全一致的场景</para>
        /// </summary>
        /// <param name="feature">Ruby feature 名称，同时作为顶层常量名称</param>
        /// <param name="constant">获取到的 Ruby 常量对象</param>
        /// <returns>首次加载成功返回 true，已加载过返回 false</returns>
        public static bool Require(string feature, out RbObject constant)
        {
            return Require(feature, feature, out constant);
        }

        /// <summary>
        /// 加载 Ruby feature 并获取指定常量
        /// <para>require 本身只返回是否首次加载，模块或类需要通过常量名再查找</para>
        /// </summary>
        /// <param name="feature">Ruby feature 名称，例如 set 或 json</param>
        /// <param name="constantName">加载后要获取的顶层常量名称，例如 Set 或 JSON</param>
        /// <param name="constant">获取到的 Ruby 常量对象</param>
        /// <returns>首次加载成功返回 true，已加载过返回 false</returns>
        public static bool Require(string feature, string constantName, out RbObject constant)
        {
            var result = Require(feature);
            constant = GetConstant(constantName);
            return result;
        }

        /// <summary>
        /// 获取 Ruby 顶层常量
        /// <para>常量不存在时 Ruby NameError 会转换为 CLR 异常</para>
        /// </summary>
        /// <param name="name">顶层常量名称，例如 Object、File、JSON 或自定义模块名</param>
        public static RbObject GetConstant(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruby 常量名称不能为空", nameof(name));
            Runtime.AutoInit();

            var symbol = new RbSymbol(name);
            var result = Runtime.rb_const_get_protect(Runtime.rb_cObject(), symbol.Ref, out int state);
            if (state != 0) RbException.CatchThrowToCLR();

            return result.Obj;
        }

        #endregion
    }
}
