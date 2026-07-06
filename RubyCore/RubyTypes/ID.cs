using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubyCore
{
    public struct ID : IEquatable<ID>
    {
        public IntPtr Pointer = IntPtr.Zero;
        public bool IsNull => Pointer == IntPtr.Zero;
        public RbObject Obj => new RbObject(this);

        public ID(IntPtr pointer)
        {
            this.Pointer = pointer;
            //RbException.CatchThrowToCLR();
        }

        public ID(long pointer)
        {
            this.Pointer = (IntPtr)pointer;
        }

        //public void Dispose()
        //{
        //    if (IsNull || !Runtime.IsInitialized) return;

        //}

        //public static implicit operator ID(int val) => new(val);
        public static bool operator ==(ID left, ID right) => left.Equals(right);
        public static bool operator !=(ID left, ID right) => !left.Equals(right);

        public bool Equals(ID other) => Pointer == other.Pointer;

        public override bool Equals(object obj) => obj is ID other && Equals(other);

        public override int GetHashCode() => Pointer.GetHashCode();
    }
}
