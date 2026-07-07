using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubyCore
{
    public class RbException
    {
        /// <summary>
        /// 将 CLR 异常转换为 Ruby RuntimeError 并抛回 Ruby
        /// </summary>
        public static VALUE RaiseClrExceptionToRuby(Exception ex)
        {
            var message = ex?.Message ?? "CLR 回调异常";
            Runtime.rb_exc_raise(Runtime.rb_new_runtime_error(message));
            return RbTypeMap.Qnil.Ref;
        }

        /// <summary>
        /// 捕获异常，如果有则抛出到 CLR 异常
        /// </summary>
        public static void CatchThrowToCLR()
        {
            // 获取异常
            var err = Runtime.rb_errinfo();
            //Console.WriteLine($"{err.Pointer} | {RbTypeMap.Qnil.Pointer}");
            if (!err.Obj.IsNil)
            {
                var Info = new StringBuilder();
                //Info.AppendLine();
                //Info.AppendLine(new string('-', 50));

                // 异常信息
                Info.AppendLine(err.Obj.ToString());

                // 清除异常
                //Runtime.rb_set_errinfo(new(UIntPtr.Zero));
                Runtime.rb_set_errinfo(RbTypeMap.Qnil.Ref);
                //Console.WriteLine($"{Runtime.rb_errinfo().Obj}");

                //Info.Append(new string('-', 50));

                throw new Exception(Info.ToString());
            }
        }

    }

}
