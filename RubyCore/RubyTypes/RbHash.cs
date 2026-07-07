using System.Collections;
using System.Reflection;

namespace RubyCore
{
    /// <summary>
    /// Ruby 哈希对象
    /// </summary>
    public class RbHash : RbIterable
    {
        public RbHash(VALUE refVal) : base(refVal)
        {
        }

        public RbHash() : base(RbEngine.Exec("{}").Ref)
        {
        }

        public RbHash(IDictionary dictionary) : this()
        {
            foreach (DictionaryEntry item in dictionary)
            {
                SetItem(item.Key, item.Value);
            }
        }

        public RbHash(object obj) : this()
        {
            var objType = obj.GetType();
            foreach (var prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SetItem(prop.Name, prop.GetValue(obj));
            }
        }

        /// <summary>
        /// 判断是否包含指定键
        /// </summary>
        public bool HasKey(object key)
        {
            return Invoke("key?", key).As<bool>();
        }

        /// <summary>
        /// 返回键集合
        /// </summary>
        public RbArray Keys()
        {
            return new RbArray(Invoke("keys").Ref);
        }

        /// <summary>
        /// 返回值集合
        /// </summary>
        public RbArray Values()
        {
            return new RbArray(Invoke("values").Ref);
        }
    }
}
