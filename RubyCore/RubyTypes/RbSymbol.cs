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

        public RbSymbol(string name) : base(Runtime.rb_id2sym(Runtime.rb_intern(name)))
        {
        }

        /// <summary>
        /// Symbol 对应的 Ruby ID
        /// </summary>
        public ID Id => Runtime.rb_sym2id(this.Ref);
    }
}
