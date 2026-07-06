namespace RubyCore
{
    /// <summary>
    /// Ruby 整数对象
    /// </summary>
    public class RbInt : RbNumber
    {
        /// <summary>
        /// 使用已有 Ruby VALUE 包装整数对象
        /// </summary>
        public RbInt(VALUE refVal) : base(refVal)
        {
        }

        /// <summary>
        /// 由 32 位整数创建 Ruby 整数对象
        /// </summary>
        public RbInt(int value) : base(Runtime.rb_int2inum(value))
        {
        }

        /// <summary>
        /// 由 64 位整数创建 Ruby 整数对象
        /// </summary>
        public RbInt(long value) : base(Runtime.rb_ll2inum(value))
        {
        }

        /// <summary>
        /// 转为 32 位整数
        /// </summary>
        public int Int32 => Runtime.rb_num2int(this.Ref);

        /// <summary>
        /// 转为 64 位整数
        /// </summary>
        public long Int64 => Runtime.rb_num2ll(this.Ref);
    }
}
