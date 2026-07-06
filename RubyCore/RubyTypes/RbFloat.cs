namespace RubyCore
{
    /// <summary>
    /// Ruby 浮点数对象
    /// </summary>
    public class RbFloat : RbNumber
    {
        /// <summary>
        /// 使用已有 Ruby VALUE 包装浮点数对象
        /// </summary>
        public RbFloat(VALUE refVal) : base(refVal)
        {
        }

        /// <summary>
        /// 由双精度浮点数创建 Ruby 浮点数对象
        /// </summary>
        public RbFloat(double value) : base(Runtime.rb_float_new(value))
        {
        }

        /// <summary>
        /// 转为双精度浮点数
        /// </summary>
        public double Value => Runtime.rb_num2dbl(this.Ref);
    }
}
