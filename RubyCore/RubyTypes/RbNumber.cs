using System;

namespace RubyCore
{
    /// <summary>
    /// Ruby 数值对象
    /// <para>运算符通过 Ruby 方法调用执行，并返回 Ruby 对象包装</para>
    /// </summary>
    public abstract class RbNumber : RbObject
    {
        /// <summary>
        /// 使用已有 Ruby VALUE 包装数值对象
        /// </summary>
        protected RbNumber(VALUE refVal) : base(refVal)
        {
        }

        /// <summary>
        /// 调用 Ruby 的加法运算
        /// </summary>
        public static RbObject operator +(RbNumber left, RbNumber right) => InvokeOperator(left, "+", right);

        /// <summary>
        /// 调用 Ruby 的减法运算
        /// </summary>
        public static RbObject operator -(RbNumber left, RbNumber right) => InvokeOperator(left, "-", right);

        /// <summary>
        /// 调用 Ruby 的乘法运算
        /// </summary>
        public static RbObject operator *(RbNumber left, RbNumber right) => InvokeOperator(left, "*", right);

        /// <summary>
        /// 调用 Ruby 的除法运算
        /// </summary>
        public static RbObject operator /(RbNumber left, RbNumber right) => InvokeOperator(left, "/", right);

        private static RbObject InvokeOperator(RbNumber left, string operatorName, RbNumber right)
        {
            if (left is null) throw new ArgumentNullException(nameof(left));
            if (right is null) throw new ArgumentNullException(nameof(right));

            return left.InvokeMethod(operatorName, right);
        }
    }
}
