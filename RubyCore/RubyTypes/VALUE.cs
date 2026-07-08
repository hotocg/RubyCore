using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RubyCore
{
    public struct VALUE : IEquatable<VALUE>
    {
        public IntPtr Pointer = IntPtr.Zero;
        public bool IsNull => Pointer == IntPtr.Zero;
        public RbObject Obj => RbObject.Wrap(this);

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

        public static implicit operator VALUE(int val) => new(val);
        public static bool operator ==(VALUE left, VALUE right) => left.Equals(right);
        public static bool operator !=(VALUE left, VALUE right) => !left.Equals(right);

        public bool Equals(VALUE other) => Pointer == other.Pointer;

        public override bool Equals(object obj) => obj is VALUE other && Equals(other);

        public override int GetHashCode() => Pointer.GetHashCode();
    }
}
