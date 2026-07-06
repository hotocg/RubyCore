using System;

namespace RubyCore.UnitTests
{
    public class RbNumberTests
    {
        [Fact]
        public void Add_ThrowsWhenLeftIsNull()
        {
            var right = new TestRbNumber(new VALUE(123));

            Assert.Throws<ArgumentNullException>(() => null + right);
        }

        [Fact]
        public void Add_ThrowsWhenRightIsNull()
        {
            var left = new TestRbNumber(new VALUE(123));

            Assert.Throws<ArgumentNullException>(() => left + null);
        }

        private class TestRbNumber : RbNumber
        {
            public TestRbNumber(VALUE refVal) : base(refVal)
            {
            }
        }
    }
}
