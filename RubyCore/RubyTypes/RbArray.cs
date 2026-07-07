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

        /// <summary>
        /// 获取指定索引的元素
        /// </summary>
        public override RbObject GetItem(params RbObject[] keys)
        {
            if (keys is null || keys.Length != 1) return base.GetItem(keys);

            var index = keys[0].As<long>();
            return Runtime.rb_ary_entry(this.Ref, index).Obj;
        }

        /// <summary>
        /// 设置指定索引的元素
        /// </summary>
        public override RbObject SetItem(RbObject key, RbObject value)
        {
            var index = key.As<long>();
            Runtime.rb_ary_store(this.Ref, index, value.Ref);
            return value;
        }
    }
}
