using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubyCore
{
    public class RbType : RbObject
    {
        #region 类型码常量
        public const int T_NONE     = 0x00;

        public const int T_OBJECT   = 0x01;
        public const int T_CLASS    = 0x02;
        public const int T_MODULE   = 0x03;
        public const int T_FLOAT    = 0x04;
        public const int T_STRING   = 0x05;
        public const int T_REGEXP   = 0x06;
        public const int T_ARRAY    = 0x07;
        public const int T_HASH     = 0x08;
        public const int T_STRUCT   = 0x09;
        public const int T_BIGNUM   = 0x0a;
        public const int T_FILE     = 0x0b;
        public const int T_DATA     = 0x0c;
        public const int T_MATCH    = 0x0d;
        public const int T_COMPLEX  = 0x0e;
        public const int T_RATIONAL = 0x0f;

        public const int T_NIL      = 0x11;
        public const int T_TRUE     = 0x12;
        public const int T_FALSE    = 0x13;
        public const int T_SYMBOL   = 0x14;
        public const int T_FIXNUM   = 0x15;
        public const int T_UNDEF    = 0x16;

        public const int T_IMEMO    = 0x1a;
        public const int T_NODE     = 0x1b;
        public const int T_ICLASS   = 0x1c;
        public const int T_ZOMBIE   = 0x1d;
        public const int T_MOVED    = 0x1e;

        public const int RUBY_T_MASK = 0x1f;
        #endregion

        public RbType(VALUE refVal) : base(refVal)
        {
        }

    }

    public class RbClass : RbObject
    {
        public RbClass(VALUE refVal) : base(refVal)
        {
        }
    }

    public class RbTypeMap
    {
        public static RbObject Qfalse;
        public static RbObject Qnil;
        public static RbObject Qtrue;
        public static RbObject Qundef;

        static RbTypeMap()
        {
            Qfalse = new RbObject(0x00);
            
            // 2.x 版本是 0x08，3.x 版本是 0x04
            //Qnil = new RbObject(0x08);
            Qnil = RbEngine.Exec("nil");

            Qtrue = new RbObject(0x14);
            Qundef = new RbObject(0x34);
        }

    }

}
