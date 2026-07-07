using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RubyCore.UnitTests
{
    /// <summary>
    /// 真实 Ruby 运行时测试集合
    /// <para>Ruby VM 是进程级运行时，这里禁用并行避免多个样例同时初始化或修改全局对象</para>
    /// </summary>
    [CollectionDefinition(nameof(RubyRuntimeCollection), DisableParallelization = true)]
    public class RubyRuntimeCollection
    {
    }

    /// <summary>
    /// 依赖真实 Ruby API DLL 的样例测试
    /// <para>本机没有可自动发现的 Ruby DLL 时跳过，避免普通单元测试环境失败</para>
    /// </summary>
    public sealed class RubyRuntimeFactAttribute : FactAttribute
    {
        public RubyRuntimeFactAttribute()
        {
            // 样例测试依赖真实 Ruby DLL；本机没有 SketchUp/Ruby DLL 时跳过，而不是让普通构建失败
            var apiDll = RbEngine.GetDefaultApiDll();
            if (string.IsNullOrWhiteSpace(apiDll) || !File.Exists(apiDll))
            {
                Skip = "未找到可用的 Ruby API DLL，跳过真实 Ruby 环境样例测试";
            }
        }
    }

    /// <summary>
    /// RubyCore 当前公开类型的使用样例测试
    /// <para>这些测试既验证真实 Ruby 环境行为，也作为 API 使用方式示例</para>
    /// </summary>
    [Collection(nameof(RubyRuntimeCollection))]
    public class RbTypeUsageSamplesTests
    {
        private static void EnsureRuby()
        {
            // Runtime 内部会自动防重入；测试里统一走公开入口，模拟正常使用方式
            RbEngine.Initialize();
        }

        /// <summary>
        /// RbEngine.Exec 和 RbObject 的核心用法
        /// <para>覆盖 Ruby 脚本执行、对象创建、方法调用、属性访问、索引访问、类型转换以及 dynamic 命名参数调用</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbEngine_Exec_And_RbObject_ShowMethodAttributeIndexAndDynamicUsage()
        {
            EnsureRuby();

            // 用 Ruby 脚本准备一个带属性、普通方法、索引器和命名参数的样例类
            RbEngine.Exec(@"
                class RubyCoreSampleUser
                  attr_accessor :name

                  def initialize
                    @name = 'origin'
                    @items = ['first', 'second', 'third']
                  end

                  def greet(prefix)
                    ""#{prefix}, #{@name}""
                  end

                  def [](index)
                    @items[index]
                  end

                  def []=(index, value)
                    @items[index] = value
                  end

                  def describe(options = {})
                    ""#{options[:name]}:#{options[:age]}""
                  end
                end
            ");

            // RbEngine.Exec 返回 Ruby 对象包装，后续都围绕 RbObject 使用
            var user = RbEngine.Exec("RubyCoreSampleUser.new");
            Assert.False(user.IsNull);
            Assert.False(user.IsNil);
            Assert.True(user.HasAttr("name"));
            Assert.Equal("RubyCoreSampleUser", user.Class.Invoke("name").As<string>());

            // 普通属性访问和方法调用：SetAttr/GetAttr/Invoke
            user.SetAttr("name", "Tom");
            Assert.Equal("Tom", user.GetAttr("name").As<string>());
            Assert.Equal("Hi, Tom", user.Invoke("greet", "Hi").As<string>());

            // 索引访问既可以走 GetItem/SetItem，也可以走 C# 索引器
            Assert.Equal("second", user.GetItem(1).As<string>());
            user.SetItem(1, "changed");
            Assert.Equal("changed", user[1].As<string>());

            // dynamic 调用会映射到 Ruby 方法、属性、索引；命名参数会转成最后一个 Symbol-keyed Hash
            dynamic dynamicUser = user;
            dynamicUser.name = "Jerry";
            RbObject dynamicName = dynamicUser.name;
            RbObject dynamicGreeting = dynamicUser.greet("Hello");
            RbObject dynamicItem = dynamicUser[1];
            RbObject dynamicNamedArgs = dynamicUser.describe(name: "Alice", age: 18);

            Assert.Equal("Jerry", dynamicName.As<string>());
            Assert.Equal("Hello, Jerry", dynamicGreeting.As<string>());
            Assert.Equal("changed", dynamicItem.As<string>());
            Assert.Equal("Alice:18", dynamicNamedArgs.As<string>());
        }

        /// <summary>
        /// RbEngine.LoadFile 加载并执行 Ruby 脚本文件
        /// <para>覆盖文件路径加载、脚本定义内容生效，以及 rb_load_protect 将 Ruby 异常转换为 CLR 异常</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbEngine_LoadFile_ShowScriptFileLoadAndRubyErrorUsage()
        {
            EnsureRuby();

            var moduleName = "RubyCoreLoadedFileSample" + Guid.NewGuid().ToString("N");
            var scriptPath = Path.Combine(Path.GetTempPath(), moduleName + ".rb");
            var errorScriptPath = Path.Combine(Path.GetTempPath(), moduleName + "_error.rb");

            try
            {
                // 写入一个真实 Ruby 脚本文件，模拟用户把扩展逻辑放在 .rb 文件中
                File.WriteAllText(scriptPath, $@"
module {moduleName}
  def self.add(left, right)
    left + right
  end
end
", System.Text.Encoding.UTF8);

                // LoadFile 执行文件后，文件中定义的模块和方法应能被后续 Ruby/C# 调用看到
                RbEngine.Load(scriptPath);
                Assert.Equal(7, RbEngine.Exec($"{moduleName}.add(3, 4)").As<int>());

                // rb_load_protect 会捕获 Ruby raise，RbException 再把它转成 CLR Exception
                File.WriteAllText(errorScriptPath, "raise 'load file error sample'", System.Text.Encoding.UTF8);
                var exception = Assert.Throws<Exception>(() => RbEngine.Load(errorScriptPath));
                Assert.Contains("load file error sample", exception.Message);
            }
            finally
            {
                if (File.Exists(scriptPath)) File.Delete(scriptPath);
                if (File.Exists(errorScriptPath)) File.Delete(errorScriptPath);
            }
        }

        /// <summary>
        /// RbEngine.Require 加载 Ruby feature
        /// <para>覆盖 require 成功加载、获取模块常量、重复加载返回 false，以及 rb_protect 将 Ruby require 异常转换为 CLR 异常</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbEngine_Require_ShowFeatureLoadAndRubyErrorUsage()
        {
            EnsureRuby();

            var moduleName = "RubyCoreRequiredFileSample" + Guid.NewGuid().ToString("N");
            var scriptPath = Path.Combine(Path.GetTempPath(), moduleName + ".rb");
            var rubyFeaturePath = scriptPath.Replace(Path.DirectorySeparatorChar, '/');
            var sameNameModuleName = "RubyCoreSameNameFeatureSample" + Guid.NewGuid().ToString("N");
            var sameNameScriptPath = Path.Combine(Path.GetTempPath(), sameNameModuleName + ".rb");

            try
            {
                // require 走 Ruby 的 feature 加载机制；这里使用唯一临时文件，避免标准库已加载导致返回值不稳定
                File.WriteAllText(scriptPath, $@"
module {moduleName}
  def self.value
    123
  end
end
", System.Text.Encoding.UTF8);

                Assert.True(RbEngine.Require(rubyFeaturePath, moduleName, out var module));
                Assert.Equal(123, module.Invoke("value").As<int>());

                File.WriteAllText(sameNameScriptPath, $@"
module {sameNameModuleName}
  def self.value
    456
  end
end
", System.Text.Encoding.UTF8);

                // feature 名和顶层常量名完全一致时，可以使用默认 out 重载
                RbEngine.AddLoadPath(Path.GetTempPath());
                var tempLoadPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');
                var loadPaths = RbEngine.LoadPath.Select(path => path.TrimEnd('/', '\\')).ToArray();
                Assert.Contains(tempLoadPath, loadPaths);
                Assert.True(RbEngine.Require(sameNameModuleName, out var sameNameModule));
                Assert.Equal(456, sameNameModule.Invoke("value").As<int>());

                // require 不会返回模块对象；需要模块或类时可以用 GetConstant 走 Ruby C API 获取顶层常量
                var constant = RbEngine.GetConstant(moduleName);
                Assert.Equal(123, constant.Invoke("value").As<int>());

                // Ruby require 对同一个 feature 只加载一次，重复加载会返回 false
                Assert.False(RbEngine.Require(rubyFeaturePath, moduleName, out module));
                Assert.Equal(123, module.Invoke("value").As<int>());

                // Require 内部使用 rb_protect 包住 rb_require，因此 Ruby LoadError 会转换成 CLR Exception
                var exception = Assert.Throws<Exception>(() => RbEngine.Require(moduleName + "_missing_feature"));
                Assert.Contains(moduleName + "_missing_feature", exception.Message);

                // GetConstant 内部也使用 rb_protect 包住 rb_const_get，因此常量不存在同样会转换成 CLR Exception
                var nameException = Assert.Throws<Exception>(() => RbEngine.GetConstant(moduleName + "MissingConstant"));
                Assert.Contains(moduleName + "MissingConstant", nameException.Message);
            }
            finally
            {
                if (File.Exists(scriptPath)) File.Delete(scriptPath);
                if (File.Exists(sameNameScriptPath)) File.Delete(sameNameScriptPath);
            }
        }

        /// <summary>
        /// Ruby 基础值包装类型的创建和使用
        /// <para>覆盖 RbString、RbBool、RbInt、RbFloat、RbSymbol、Ruby 方法调用、数值运算符和 dynamic 运算</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbString_RbBool_RbInt_RbFloat_RbSymbol_ShowCreationInvokeOperatorAndConversionUsage()
        {
            EnsureRuby();

            // 直接从 CLR 值创建 Ruby 包装对象
            var text = new RbString("hello");
            var truthy = new RbBool(true);
            var falsy = new RbBool(false);
            var left = new RbInt(10);
            var right = new RbInt(5);
            var pi = new RbFloat(3.5);
            var symbol = new RbSymbol("name");

            // As<T>() 用于把 Ruby VALUE 转回常见 CLR 类型
            Assert.Equal("hello", text.As<string>());
            Assert.Equal("HELLO", text.Invoke("upcase").As<string>());
            Assert.Equal("String", text.Class.Invoke("name").As<string>());
            Assert.True(truthy.Value);
            Assert.False(falsy.Value);
            Assert.Equal(10, left.Int32);
            Assert.Equal(10L, left.Int64);
            Assert.Equal(3.5, pi.Value, 6);
            Assert.Equal("name", symbol.Invoke("to_s").As<string>());
            Assert.False(symbol.Id.IsNull);

            // RbNumber 运算符本质是调用 Ruby 的 + - * /，结果仍然是 RbObject
            Assert.Equal(15, (left + right).As<int>());
            Assert.Equal(5, (left - right).As<int>());
            Assert.Equal(50, (left * right).As<int>());
            Assert.Equal(2, (left / right).As<int>());

            // dynamic 数值运算同样会派发到 Ruby 运算符方法
            dynamic dynamicNumber = left;
            RbObject dynamicSum = dynamicNumber + 7;
            RbObject dynamicNegative = -dynamicNumber;

            Assert.Equal(17, dynamicSum.As<int>());
            Assert.Equal(-10, dynamicNegative.As<int>());
        }

        /// <summary>
        /// Ruby 数组和可迭代对象的使用
        /// <para>覆盖 RbArray 构造、Add、索引读写、LINQ/foreach 读取以及转换为 CLR 数组和 List</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbArray_And_RbIterable_ShowAddIndexSetForeachAndManagedConversionUsage()
        {
            EnsureRuby();

            // RbArray 支持 params 构造、Add 追加和索引写入
            var array = new RbArray(1, 2, 3);
            array.Add(4);
            array[1] = new RbInt(20);

            Assert.Equal(4, array.Length());
            Assert.Equal(1, array.GetItem(0).As<int>());
            Assert.Equal(20, array[1].As<int>());

            // RbIterable 让 Ruby 集合可以被 LINQ/foreach 读取
            var values = array.Select(item => item.As<int>()).ToArray();
            Assert.Equal(new[] { 1, 20, 3, 4 }, values);

            // As<T>() 可把 Ruby 数组映射为 CLR 数组或 List<T>
            var managedArray = array.As<int[]>();
            var managedList = array.As<List<int>>();

            Assert.Equal(new[] { 1, 20, 3, 4 }, managedArray);
            Assert.Equal(new List<int> { 1, 20, 3, 4 }, managedList);

            // dynamic 索引读写会走 TryGetIndex/TrySetIndex
            dynamic dynamicArray = array;
            RbObject dynamicItem = dynamicArray[2];
            dynamicArray[2] = 30;

            Assert.Equal(3, dynamicItem.As<int>());
            Assert.Equal(30, array[2].As<int>());
        }

        /// <summary>
        /// Ruby Hash 的 key 类型和常用访问方式
        /// <para>覆盖字符串 key、Symbol key、HasKey、Keys、Values、IDictionary 构造和匿名对象构造</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbHash_ShowStringKeySymbolKeyObjectDictionaryKeysAndValuesUsage()
        {
            EnsureRuby();

            // Ruby Hash 里字符串 key 和 Symbol key 是两种不同 key，这里分别演示
            var hash = new RbHash();
            hash["name"] = new RbString("Tom");
            hash[new RbSymbol("age")] = new RbInt(18);

            Assert.True(hash.HasKey("name"));
            Assert.True(hash.HasKey(new RbSymbol("age")));
            Assert.Equal("Tom", hash["name"].As<string>());
            Assert.Equal(18, hash[new RbSymbol("age")].As<int>());

            // Keys/Values 返回 Ruby 数组，因此可以继续按 RbArray/RbIterable 使用
            var keys = hash.Keys().Select(key => key.ToString()).ToArray();
            var values = hash.Values().Select(value => value.ToString()).ToArray();

            Assert.Contains("name", keys);
            Assert.Contains("age", keys);
            Assert.Contains("Tom", values);
            Assert.Contains("18", values);

            // IDictionary 和匿名对象都会被转换成 Ruby Hash；匿名对象属性名目前按字符串 key 写入
            var fromDictionary = new RbHash(new Dictionary<string, object>
            {
                ["title"] = "Book",
                ["count"] = 2
            });

            var fromObject = new RbHash(new
            {
                category = "Tool",
                enabled = true
            });

            Assert.Equal("Book", fromDictionary["title"].As<string>());
            Assert.Equal(2, fromDictionary["count"].As<int>());
            Assert.Equal("Tool", fromObject["category"].As<string>());
            Assert.True(fromObject["enabled"].As<bool>());
        }

        /// <summary>
        /// CLR 与 Ruby 对象之间的映射转换
        /// <para>覆盖 ToRuby、RbConverter.ToRubyValue、As&lt;T&gt; 以及 AsRb* 类型化包装扩展</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbConverter_And_Extensions_ShowClrRubyMappingUsage()
        {
            EnsureRuby();

            // ToRuby()/ToRubyValue 负责 CLR -> Ruby 的基础类型、数组和字典转换
            RbObject rubyString = "abc".ToRuby();
            RbObject rubyInt = 42.ToRuby();
            RbObject rubyFloat = 2.5.ToRuby();
            RbObject rubyBool = true.ToRuby();
            RbObject rubyNil = RbConverter.ToRubyValue(null);
            var rubyArray = RbConverter.ToRubyValue(new[] { 1, 2, 3 }).AsRbArray();
            var rubyHash = RbConverter.ToRubyValue(new Dictionary<string, object>
            {
                ["x"] = 1,
                ["y"] = "two"
            }).AsRbHash();

            Assert.Equal("abc", rubyString.As<string>());
            Assert.Equal(42, rubyInt.As<int>());
            Assert.Equal(2.5, rubyFloat.As<double>(), 6);
            Assert.True(rubyBool.As<bool>());
            Assert.True(rubyNil.IsNil);
            Assert.Equal(3, rubyArray.Length());
            Assert.Equal(1, rubyHash["x"].As<int>());
            Assert.Equal("two", rubyHash["y"].As<string>());

            // AsRb* 扩展用于把通用 RbObject 包装成更具体的 Ruby 类型包装
            Assert.IsType<RbString>(rubyString.AsRbString());
            Assert.IsType<RbInt>(rubyInt.AsRbInt());
            Assert.IsType<RbFloat>(rubyFloat.AsRbFloat());
            Assert.IsType<RbBool>(rubyBool.AsRbBool());
            Assert.IsType<RbHash>(rubyHash.AsRbHash());
            Assert.IsType<RbIterable>(rubyArray.AsRbIterable());
        }

        /// <summary>
        /// RbModule.DefineFunction 的模块方法注册
        /// <para>覆盖 Ruby 调 C# 回调、C# 返回 Ruby 对象、Action 自动返回 nil，以及 C# 侧继续调用模块方法</para>
        /// </summary>
        [RubyRuntimeFact]
        public void RbModule_DefineFunction_ShowRubyCallingClrAndClrCallingRubyUsage()
        {
            EnsureRuby();

            // 每次测试生成唯一模块名，避免重复定义模块方法互相影响
            var moduleName = "RubyCoreSampleModule" + Guid.NewGuid().ToString("N");
            var module = new RbModule(moduleName);
            var lastMessage = string.Empty;

            // DefineFunction 返回 RbObject：Ruby 调用 C#，C# 再把结果包装回 Ruby
            module.DefineFunction("join_args", (self, args) => {
                var text = string.Join("|", args.Select(arg => arg.ToString()));
                return new RbString(text);
            });

            // DefineFunction 也支持 Action：无返回值时自动返回 Qnil
            module.DefineFunction("remember", (self, args) => {
                lastMessage = args.Length == 0 ? string.Empty : args[0].As<string>();
            });

            // C# 回调里抛出的 CLR 异常会转换成 Ruby RuntimeError，Ruby begin/rescue 可以捕获
            module.DefineFunction("raise_clr_error", (self, args) => {
                throw new InvalidOperationException("clr callback error sample");
            });

            // Ruby 脚本侧可以直接调用 C# 注册的模块方法
            Assert.Equal("a|2|true", RbEngine.Exec($"{moduleName}.join_args('a', 2, true)").As<string>());
            Assert.True(RbEngine.Exec($"{moduleName}.remember('from ruby').nil?").As<bool>());
            Assert.Equal("from ruby", lastMessage);
            Assert.Equal("clr callback error sample", RbEngine.Exec($@"
                begin
                  {moduleName}.raise_clr_error
                rescue => e
                  e.message
                end
            ").As<string>());

            var exception = Assert.Throws<Exception>(() => RbEngine.Exec($"{moduleName}.raise_clr_error"));
            Assert.Contains("clr callback error sample", exception.Message);

            // C# 侧也可以拿回模块对象后继续 Invoke 模块方法
            var moduleFromRuby = RbEngine.Exec(moduleName);
            Assert.Equal("x|y", moduleFromRuby.Invoke("join_args", "x", "y").As<string>());

            DefineTemporaryModuleFunction(moduleName + "KeepAlive");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Ruby VM 只保存 C 函数指针；即使 C# 的模块包装对象已离开作用域，注册函数仍应可调用
            Assert.Equal("still alive", RbEngine.Exec($"{moduleName}KeepAlive.ping").As<string>());
        }

        private static void DefineTemporaryModuleFunction(string moduleName)
        {
            var module = new RbModule(moduleName);
            module.DefineFunction("ping", (self, args) => new RbString("still alive"));
        }

        /// <summary>
        /// Ruby Proc、自身调用、输出和异常转换
        /// <para>覆盖 Invoke 调 call、dynamic 对象自身调用、RbEngine.Print，以及 Ruby raise 转 CLR Exception</para>
        /// </summary>
        [RubyRuntimeFact]
        public void Proc_Call_And_Exception_ShowInvokeSelfPrintAndRubyErrorUsage()
        {
            EnsureRuby();

            // Invoke(params object[]) 用于调用对象自身，等价于 Ruby 的 call
            var proc = RbEngine.Exec("Proc.new { |value| value * 2 }");
            Assert.Equal(10, proc.Invoke(5).As<int>());

            // dynamic 对象自身调用也会映射到 Ruby 的 call
            dynamic dynamicProc = proc;
            RbObject dynamicResult = dynamicProc(6);
            Assert.Equal(12, dynamicResult.As<int>());

            // Print 走 Ruby $stdout 输出，适合简单调试信息
            RbEngine.Print("RubyCore sample print", 123);

            // Exec 使用 protect API，Ruby raise 会被转换为 CLR Exception
            var exception = Assert.Throws<Exception>(() => RbEngine.Exec("raise 'sample error'"));
            Assert.Contains("sample error", exception.Message);
        }
    }
}
