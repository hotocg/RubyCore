namespace RubyCore.UnitTests
{
    public class RbConverterTests
    {
        [Fact]
        public void ToRubyValue_ReturnsSameRbObject()
        {
            var obj = new RbObject(new VALUE(123));

            var result = RbConverter.ToRubyValue(obj);

            Assert.Same(obj, result);
        }

        [Fact]
        public void ToRubyValue_WrapsValuePointer()
        {
            var value = new VALUE(123);

            var result = RbConverter.ToRubyValue(value);

            Assert.Equal(value.Pointer, result.Pointer);
        }

    }
}
