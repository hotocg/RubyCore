namespace RubyCore
{
    /// <summary>
    /// Ruby 符号对象
    /// </summary>
    public class RbSymbol : RbObject
    {
        public RbSymbol(VALUE refVal) : base(refVal)
        {
        }

        public RbSymbol(string name) : base(new RbString(name).Invoke("to_sym").Ref)
        {
        }
    }
}
