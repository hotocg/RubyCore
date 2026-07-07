using System.Linq;

namespace RubyCore
{
    /// <summary>
    /// Ruby 数组对象
    /// </summary>
    public class RbArray : RbIterable
    {
        public RbArray(VALUE refVal) : base(refVal)
        {
        }

        public RbArray() : base(Runtime.rb_ary_new())
        {
        }

        public RbArray(params RbObject[] values) : this()
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public RbArray(params object[] values) : this(values.Select(RbConverter.ToRubyValue).ToArray())
        {
        }

        /// <summary>
        /// 追加数组项
        /// </summary>
        public RbArray Add(RbObject value)
        {
            Runtime.rb_ary_push(this.Ref, value.Ref);
            return this;
        }

        /// <summary>
        /// 追加数组项
        /// </summary>
        public RbArray Add(object value)
        {
            return Add(RbConverter.ToRubyValue(value));
        }
    }
}
