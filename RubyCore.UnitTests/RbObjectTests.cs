namespace RubyCore.UnitTests
{
    public class RbObjectTests
    {
        [Fact]
        public void IsNull_ReturnsTrueForZeroPointer()
        {
            var obj = new RbObject(new VALUE(0));

            Assert.True(obj.IsNull);
        }

        [Fact]
        public void Equals_ReturnsTrueForSamePointer()
        {
            var left = new RbObject(new VALUE(123));
            var right = new RbObject(new VALUE(123));

            Assert.True(left.Equals(right));
        }

        [Fact]
        public void Equals_ReturnsFalseForDifferentPointer()
        {
            var left = new RbObject(new VALUE(123));
            var right = new RbObject(new VALUE(456));

            Assert.False(left.Equals(right));
        }

        [Fact]
        public void Equals_ReturnsFalseForDifferentObjectType()
        {
            var obj = new RbObject(new VALUE(123));

            Assert.False(obj.Equals("123"));
        }
    }
}
