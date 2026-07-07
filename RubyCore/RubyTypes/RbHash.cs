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

        public RbHash() : base(Runtime.rb_hash_new())
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
        /// 获取指定键的值
        /// </summary>
        public override RbObject GetItem(params RbObject[] keys)
        {
            if (keys is null || keys.Length != 1) return base.GetItem(keys);

            return Runtime.rb_hash_aref(this.Ref, keys[0].Ref).Obj;
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        public override RbObject SetItem(RbObject key, RbObject value)
        {
            return Runtime.rb_hash_aset(this.Ref, key.Ref, value.Ref).Obj;
        }

        /// <summary>
        /// 判断是否包含指定键
        /// </summary>
        public override bool HasKey(object key)
        {
            var rbKey = RbConverter.ToRubyValue(key);
            return Runtime.rb_hash_has_key(this.Ref, rbKey.Ref).Obj.As<bool>();
        }

        /// <summary>
        /// 返回键集合
        /// </summary>
        public RbArray Keys()
        {
            return new RbArray(Runtime.rb_hash_keys(this.Ref));
        }

        /// <summary>
        /// 返回值集合
        /// </summary>
        public RbArray Values()
        {
            return new RbArray(Runtime.rb_hash_values(this.Ref));
        }
    }
}
