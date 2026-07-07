namespace RubyCore
{
    /// <summary>
    /// Ruby 布尔对象
    /// </summary>
    public class RbBool : RbObject
    {
        public RbBool(VALUE refVal) : base(refVal)
        {
        }

        public RbBool(bool value) : base(value ? RbTypeMap.Qtrue.Ref : RbTypeMap.Qfalse.Ref)
        {
        }

        /// <summary>
        /// 转为 CLR 布尔值
        /// </summary>
        public bool Value => !IsNil && Pointer != RbTypeMap.Qfalse.Pointer;
    }
}
