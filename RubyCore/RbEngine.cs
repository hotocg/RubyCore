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
        /// 加载并执行 Ruby 脚本文件
        /// </summary>
        /// <param name="filePath">Ruby 脚本文件路径</param>
        /// <param name="wrap">是否在匿名模块中加载</param>
        public static void Load(string filePath, bool wrap = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Ruby 脚本文件路径不能为空", nameof(filePath));
            }

            var fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Ruby 脚本文件不存在: {fullPath}", fullPath);
            }

            Runtime.AutoInit();

            var rubyPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');
            var rbPath = new RbString(rubyPath);
            Runtime.rb_load_protect(rbPath.Ref, wrap ? 1 : 0, out int state);
            if (state != 0) RbException.CatchThrowToCLR();
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

    }
}
