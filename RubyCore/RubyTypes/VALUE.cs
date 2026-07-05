using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RubyCore
{
    public  struct VALUE
    {
        public IntPtr Pointer = IntPtr.Zero;
        public bool IsNull => Pointer == IntPtr.Zero;
        public RbObject Obj => new RbObject(this);

        public VALUE(IntPtr pointer)
        {
            this.Pointer = pointer;
            //RbException.CatchThrowToCLR();
        }

        public VALUE(long pointer)
        {
            this.Pointer = (IntPtr)pointer;
        }

        //public void Dispose()
        //{
        //    if (IsNull || !Runtime.IsInitialized) return;

        //}

        public static bool operator ==(VALUE a, VALUE b) => a.Pointer == b.Pointer;
        public static bool operator !=(VALUE a, VALUE b) => a.Pointer != b.Pointer;

        public static implicit operator VALUE(int val) => new(val);

    }
}
