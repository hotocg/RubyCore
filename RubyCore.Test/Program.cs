using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RubyCore.Test
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                RubyTest();
                //RbEngine.Initialize(@"C:\Program Files\SketchUp\SketchUp 2018\x64-msvcrt-ruby220.dll");
                //var result = RbEngine.Exec("1+1");
                //Console.WriteLine(result);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        static void RubyTest()
        {
            try
            {
                var hostDir = @"C:\Program Files\SketchUp\SketchUp 2018";
                var dllPath = Path.Combine(hostDir, "x64-msvcrt-ruby220.dll");

                //hostDir = @"C:\Program Files (x86)\SketchUp\SketchUp 2013";
                //dllPath = Path.Combine(hostDir, "msvcrt-ruby18.dll");

                //RbEngine.Initialize(DllPath);
                RbEngine.Initialize();

                // ==================================================
                //dynamic obj = RbEngine.Exec("['a', 'b', 'c']");
                //var first = obj[0];

                //dynamic num1 = new RbInt(1);
                //var sum = num1 + 2;

                //var list = obj.As<List<string>>();

                //dynamic v = new RbInt(123);
                //Console.WriteLine(v.to_s);

                // ==================================================
                RbEngine.AddLoadPath(@"C:\Program Files\SketchUp\SketchUp 2025\SketchUp\Tools\RubyStdLib");
                RbEngine.AddLoadPath(@"C:\Program Files\SketchUp\SketchUp 2025\SketchUp\Tools\RubyStdLib\platform_specific");

                RbEngine.Exec("puts $LOAD_PATH");
                RbEngine.Exec("puts $LOADED_FEATURES");
                //var requireResult = RbEngine.Require(json);
                var requireResult = RbEngine.Require("json", "JSON", out var Json);
                Console.WriteLine(((dynamic)Json).generate(new RbArray(1, "2")));
                //var requireResult = RbEngine.Require("json");

                return;
                var module = new RbModule("RbCore");

                module.DefineFunction("Test2", (self, args) =>
                {
                    Console.WriteLine($"[Info] {self} {args.Length}");
                    foreach (var arg in args)
                    {
                        Console.WriteLine($"[参数] {arg}");
                    }
                    //throw new Exception("Test CLR Exception!");
                    return new RbString("啊哈哈");
                });

                module.DefineFunction("Test3", (self, args) =>
                {
                    foreach (var arg in args)
                    {
                        Console.WriteLine($"参数 {arg}");
                    }
                });


                try
                {
                    Console.WriteLine($"Invoke: {module.Invoke("Test2", new RbString("1"), RbTypeMap.Qtrue)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invoke CLR 捕获: {ex.Message}");
                }


                var testDir = @"E:\临时\待整理";
                RbEngine.Exec($@"
                    begin
                        $LOAD_PATH << '{hostDir}\Tools\RubyStdLib'
                        $LOAD_PATH << '{hostDir}\Tools\RubyStdLib\platform_specific'
                    
                        puts '{testDir}'
                        Encoding.default_external = 'UTF-8'
                    
                        p RbCore
                        #RbCore.Test
                        #RbCore.Test1 '啊哈哈'
                        a = RbCore.Test2(678, '蛋炒饭', [1, 2], {{a: 1 }})
                        b = RbCore.Test3(789, '烧卖', [3, 4], {{b: 2 }})
                        p ('test2 返回值: '.force_encoding('UTF-8') + a)
                        p ('test3 返回值: '.force_encoding('UTF-8') + b.to_s)
                    
                        #require 'json'
                        #JSON.generate(b)
                    rescue => e
                        p e
                    end
                ");
                Console.WriteLine();
                Console.WriteLine($"{module} | {module.Class} | {module.GetHashCode()}");

                var num = RbEngine.Exec("123");
                Console.WriteLine($"{num} | {num.Class} | {num.GetHashCode()}");

                // ==================================================
                var code = $@"
                    #raise ""测试异常""
                    def ht_test
                        # UI.messagebox(""{DateTime.Now.ToString()}"")
                        # FuncAPI.CaptureView(true)
                        
                        begin
                            $LOAD_PATH << '{hostDir}\Tools\RubyStdLib'
                            $LOAD_PATH << '{hostDir}\Tools\RubyStdLib\platform_specific'
                            p $LOAD_PATH
                            #require 'enc/encdb'
                            #require 'enc/trans/transdb'
                            #Encoding.default_external = 'UTF-8'
                            #Encoding.default_internal = 'UTF-8'
                            #p File.expand_path($0)
                            require 'rbconfig'
                            # 获取 Ruby 安装的根目录
                            ruby_root = RbConfig::CONFIG['prefix']
                            # 获取 Ruby 动态库或执行文件所在目录
                            bin_dir = RbConfig::CONFIG['bindir']
                            p ruby_root
                            p bin_dir
                            p Dir.pwd
                            require 'json'
                            person = {{
                                ""name"" => ""哈哈"".force_encoding('UTF-8'),
                                ""age"" => 25,
                                ""hobbies"" => [""2"", ""3""]
                            }}
                            p ""{DateTime.Now.ToString()}""
                            #raise ""测试异常""
                            #model = Sketchup.active_model
                            #materials = model.materials
                            #materials.load('C:\Users\Administrator\Desktop\材料1.skm')
                            p person
                            JSON.generate(person)
                            #return materials.count.to_s
                        rescue => e
                            # UI.messagebox(""Error: #{{e.message}}"")
                            ""Error: #{{e.message}}""
                        end
                    end
                    ht_test
                ";

                var result = RbEngine.Exec(code);
                Console.WriteLine($"{result} | {result.Class}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }
        }
    }
}
