using System;

namespace RubyCore.UnitTests
{
    public class VALUETests
    {
        [Fact]
        public void DefaultValue_IsNull()
        {
            var value = default(VALUE);

            Assert.True(value.IsNull);
            Assert.Equal(IntPtr.Zero, value.Pointer);
        }

        [Fact]
        public void Equality_UsesPointerValue()
        {
            var left = new VALUE(123);
            var same = new VALUE(123);
            var different = new VALUE(456);

            Assert.True(left == same);
            Assert.False(left != same);
            Assert.False(left == different);
            Assert.True(left != different);
        }

        [Fact]
        public void Equals_UsesPointerValue()
        {
            var value = new VALUE(123);
            var same = new VALUE(123);
            var different = new VALUE(456);

            Assert.True(value.Equals(same));
            Assert.False(value.Equals(different));
            Assert.False(value.Equals("123"));
        }

        [Fact]
        public void GetHashCode_UsesPointerHashCode()
        {
            var value = new VALUE(123);

            Assert.Equal(((IntPtr)123).GetHashCode(), value.GetHashCode());
        }

        [Fact]
        public void Obj_CreatesRbObjectWithSamePointer()
        {
            var value = new VALUE(123);

            var obj = value.Obj;

            Assert.Equal(value.Pointer, obj.Pointer);
        }
    }
}
