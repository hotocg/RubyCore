using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RubyCore
{
    public class RbObject
    {
        public IntPtr Pointer;
        public VALUE Ref => new(Pointer);
        /// <summary>
        /// 是否空指针
        /// </summary>
        public bool IsNull => Pointer == IntPtr.Zero;
        /// <summary>
        /// 是否空值
        /// </summary>
        public bool IsNil => Pointer == RbTypeMap.Qnil.Pointer;
        /// <summary>
        /// Ruby 类型
        /// </summary>
        public RbClass Class => new RbClass(Runtime.rb_obj_class(this.Ref));

        public RbObject(VALUE refVal)
        {
            this.Pointer = refVal.Pointer;
        }

        public RbObject(ID refVal)
        {
            this.Pointer = refVal.Pointer;
        }

        public override string ToString()
        {
            var strObj = Runtime.rb_obj_as_string(new(Pointer));
            var ptr = Runtime.rb_string_value_cstr(ref strObj);

            return StrPtr.PtrToString(ptr);
        }


        #region 比较
        /// <summary>
        /// 获取对象的哈希值
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hash = Runtime.rb_hash(this.Ref);
            //Console.WriteLine($"{(long)hash.Pointer} | {hash.Obj} | {Runtime.rb_num2int(hash)}");
            //return (int)Runtime.rb_num2int(hash);
            return hash.Pointer.GetHashCode();
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// <para>用于 LINQ、集合类、字典 ... 比较</para>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;

            var rbObj = obj as RbObject;
            if (rbObj is null) return false;
            if (Pointer == rbObj.Pointer) return true;

            return false;
        }
        #endregion

        public RbObject Invoke(string methodName, params RbObject[] args)
        {
            var methodId = Runtime.rb_intern(methodName);
            return Runtime.rb_funcallv(this.Ref, methodId, args.Select(x => x.Ref).ToArray()).Obj;
        }

    }
}
