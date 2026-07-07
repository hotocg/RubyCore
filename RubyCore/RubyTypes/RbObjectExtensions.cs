namespace RubyCore
{
    /// <summary>
    /// Ruby 对象类型化扩展
    /// </summary>
    public static class RbObjectExtensions
    {
        public static RbArray AsRbArray(this RbObject obj) => new RbArray(obj.Ref);

        public static RbBool AsRbBool(this RbObject obj) => new RbBool(obj.Ref);

        public static RbFloat AsRbFloat(this RbObject obj) => new RbFloat(obj.Ref);

        public static RbHash AsRbHash(this RbObject obj) => new RbHash(obj.Ref);

        public static RbInt AsRbInt(this RbObject obj) => new RbInt(obj.Ref);

        public static RbIterable AsRbIterable(this RbObject obj) => new RbIterable(obj.Ref);

        public static RbString AsRbString(this RbObject obj) => new RbString(obj.Ref);

        public static RbSymbol AsRbSymbol(this RbObject obj) => new RbSymbol(obj.Ref);
    }
}
