using System.Collections;
using System.Reflection;

namespace RubyCore
{
    /// <summary>
    /// Ruby 哈希对象
    /// </summary>
    public class RbHash : RbIterable
    {
        /// <summary>
        /// 使用已有 Ruby VALUE 创建哈希包装
        /// </summary>
        public RbHash(VALUE refVal) : base(refVal)
        {
        }

        /// <summary>
        /// 创建空 Ruby Hash
        /// </summary>
        public RbHash() : base(Runtime.rb_hash_new())
        {
        }

        /// <summary>
        /// 使用字典创建 Ruby Hash
        /// <para>字典 key 会按原始 CLR 类型转换，例如 string key 会生成 Ruby 字符串 key</para>
        /// </summary>
        public RbHash(IDictionary dictionary) : this()
        {
            foreach (DictionaryEntry item in dictionary)
            {
                SetItem(item.Key, item.Value);
            }
        }

        /// <summary>
        /// 使用对象公开属性创建 Ruby Hash
        /// <para>默认生成 Symbol key，适合 Ruby API 常见的 options hash；传入 false 可生成字符串 key</para>
        /// </summary>
        public RbHash(object obj, bool symbolKey = true) : this()
        {
            var objType = obj.GetType();
            foreach (var prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var key = symbolKey ? (object)new RbSymbol(prop.Name) : prop.Name;
                SetItem(key, prop.GetValue(obj));
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
