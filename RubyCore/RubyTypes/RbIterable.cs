namespace RubyCore
{
    /// <summary>
    /// Ruby 可迭代对象
    /// </summary>
    public class RbIterable : RbObject
    {
        /// <summary>
        /// 使用已有 Ruby VALUE 创建可迭代对象包装
        /// </summary>
        public RbIterable(VALUE refVal) : base(refVal)
        {
        }
    }
}
