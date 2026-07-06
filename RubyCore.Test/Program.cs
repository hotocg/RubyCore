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

        public static void Test3(RbObject self, RbObject[] args)
        {
            foreach (var arg in args)
            {
                Console.WriteLine($"参数 {arg}");
            }
        }

        static void RubyTest()
        {
            try
            {
                //Console.WriteLine("Hello, World!");

                var HostDir = @"C:\Program Files\SketchUp\SketchUp 2018";
                var DllPath = Path.Combine(HostDir, "x64-msvcrt-ruby220.dll");

                //HostDir = @"C:\Program Files\SketchUp\SketchUp 2023";
                //DllPath = Path.Combine(HostDir, "x64-msvcrt-ruby270.dll");

                //DllPath = @"C:\Users\Administrator\Desktop\SU Ruby\Dll\x64-msvcrt-ruby220.dll";
                //DllPath = @"C:\Users\Administrator\Desktop\SU Ruby\Dll\x64-msvcrt-ruby260.dll";
                //DllPath = @"C:\Users\Administrator\Desktop\SU Ruby\Dll\x64-msvcrt-ruby270.dll";
                //DllPath = @"C:\Users\Administrator\Desktop\SU Ruby\Dll\x64-ucrt-ruby320.dll";

                //HostDir = @"C:\Program Files\SketchUp\SketchUp 2025\SketchUp";
                //DllPath = Path.Combine(HostDir, "x64-ucrt-ruby320.dll");
                //DllPath = @"C:\Program Files\SketchUp\SketchUp 2025\SketchUp\x64-ucrt-ruby320.dll";

                //Console.WriteLine($"CurrentDirectory: {Environment.CurrentDirectory}");
                //Environment.CurrentDirectory = Path.GetDirectoryName(DllPath);
                //Console.WriteLine($"CurrentDirectory: {Environment.CurrentDirectory}");

                RbEngine.Initialize(DllPath);

                var module = new RbModule("Su66Core");
                //module.DefineFunction(nameof(Test), new Delegate_Test(Test));
                //module.DefineFunction(nameof(Test1), new Delegate_Test1(Test1));
                //module.DefineFunction(nameof(Test2), new Delegate_Test2(Test2));

                module.DefineFunction("Test2", (self, args) =>
                {
                    Console.WriteLine($"{self} {args.Length}");
                    foreach (var arg in args)
                    {
                        Console.WriteLine($"参数: {arg}");
                    }
                    return new RbString("啊哈哈");
                });

                Console.WriteLine($"Invoke: {module.Invoke("Test2", new RbString("1"), RbTypeMap.Qtrue)}");

                //Console.WriteLine($"Invoke: {module.Invoke("methods", new RbString(":test2"))}");
                //Console.WriteLine($"Invoke: {new RbString("123").Invoke("class")}");
                //Console.WriteLine($"Invoke: {module.Invoke("test22")}");
                //Console.WriteLine($"Invoke: {module.Invoke("test22", RbTypeMap.Qtrue)}");
                //Console.WriteLine($"Invoke: {module.Invoke("test22", new RbString("123"))}");

                RbEngine.Exec($@"
module TmpModule
    def self.Test(show = '123')
        p show
    end
end
");
                //Console.WriteLine($"Invoke: {RbEngine.Exec("TmpModule").Invoke("Test", new RbString("1"), new RbString("1"))}");
                //Console.WriteLine($"Invoke: {new RbString("123").Invoke("split", new RbString("1"))}");
                //Console.WriteLine($"Invoke: {new RbString("123")}");

                return;
                module.DefineFunction("Test3", Test3);


                var testDir = @"E:\临时\待整理";
                RbEngine.Exec($@"
                    $LOAD_PATH << '{HostDir}\Tools\RubyStdLib'
                    $LOAD_PATH << '{HostDir}\Tools\RubyStdLib\platform_specific'
                    
                    puts '{testDir}'
                    Encoding.default_external = 'UTF-8'
                    
                    p Su66Core
                    #Su66Core.test
                    #Su66Core.test1 '啊哈哈'
                    a = Su66Core.test2(678, '蛋炒饭', [1, 2], {{a: 1 }})
                    b = Su66Core.test3(789, '烧卖', [3, 4], {{b: 2 }})
                    p ('test2 返回值: '.force_encoding('UTF-8') + a)
                    p ('test3 返回值: '.force_encoding('UTF-8') + b.to_s)
                    
                    #require 'json'
                    #JSON.generate(b)
                ");
                Console.WriteLine();
                Console.WriteLine($"{module} | {module.Class} | {module.GetHashCode()}");

                var num = RbEngine.Exec("123");
                Console.WriteLine($"{num} | {num.Class} | {num.GetHashCode()}");



                //return;


                //var code = "open('RubyTestHello.txt', 'w') {|fp| fp.write(\"Hello, World!\\n\")}";
                //var code = "1.to_s";
                //var code = "啊哈哈";
                //Console.WriteLine(IntPtr.Size);

                var code = $@"
                    #raise ""测试异常""
                    def ht_test
                        # UI.messagebox(""{DateTime.Now.ToString()}"")
                        # FuncAPI.CaptureView(true)
                        
                        begin
                            $LOAD_PATH << '{HostDir}\Tools\RubyStdLib'
                            $LOAD_PATH << '{HostDir}\Tools\RubyStdLib\platform_specific'
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
