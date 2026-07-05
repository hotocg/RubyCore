using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubyCore
{
    public class RbString : RbObject
    {
        public RbString(VALUE refVal) : base(refVal)
        {
        }

        public RbString(string str) : base(Runtime.rb_utf8_str_new_cstr(str))
        {
        }

    }
}
