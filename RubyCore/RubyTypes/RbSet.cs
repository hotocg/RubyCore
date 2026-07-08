using System.Linq;

namespace RubyCore
{
    /// <summary>
    /// Ruby Set 对象
    /// <para>基于 Ruby 标准库 set，创建前会自动 require set</para>
    /// </summary>
    public class RbSet : RbIterable
    {
        /// <summary>
        /// 包装已有 Ruby Set VALUE
        /// </summary>
        public RbSet(VALUE refVal) : base(refVal)
        {
        }

        /// <summary>
        /// 创建空 Ruby Set
        /// </summary>
        public RbSet() : base(CreateSet())
        {
        }

        /// <summary>
        /// 使用 Ruby 对象创建 Ruby Set
        /// </summary>
        public RbSet(params RbObject[] values) : this()
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        /// <summary>
        /// 使用托管对象创建 Ruby Set
        /// </summary>
        public RbSet(params object[] values) : this(values.Select(RbConverter.ToRubyValue).ToArray())
        {
        }

        private static VALUE CreateSet()
        {
            RbEngine.Require("set", "Set", out var setClass);
            return setClass.InvokeMethod("new").Ref;
        }

        /// <summary>
        /// 添加元素
        /// <para>等价于 Ruby 的 add</para>
        /// </summary>
        public RbSet Add(RbObject value)
        {
            InvokeMethod("add", value);
            return this;
        }

        /// <summary>
        /// 添加元素
        /// <para>等价于 Ruby 的 add</para>
        /// </summary>
        public RbSet Add(object value)
        {
            return Add(RbConverter.ToRubyValue(value));
        }

        /// <summary>
        /// 合并可枚举对象
        /// <para>等价于 Ruby 的 merge</para>
        /// </summary>
        public RbSet Merge(object value)
        {
            InvokeMethod("merge", value);
            return this;
        }

        /// <summary>
        /// 删除元素
        /// <para>等价于 Ruby 的 delete</para>
        /// </summary>
        public RbSet Delete(object value)
        {
            InvokeMethod("delete", value);
            return this;
        }

        /// <summary>
        /// 清空集合
        /// <para>等价于 Ruby 的 clear</para>
        /// </summary>
        public RbSet Clear()
        {
            InvokeMethod("clear");
            return this;
        }
    }
}
