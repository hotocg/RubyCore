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
                RbEngine.Initialize(@"C:\Program Files\SketchUp\SketchUp 2018\x64-msvcrt-ruby220.dll");
                var result = RbEngine.Exec("1+1");
                Console.WriteLine(result);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
