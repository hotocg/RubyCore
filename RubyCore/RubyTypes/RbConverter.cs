using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RubyCore
{
    /// <summary>
    /// Ruby 与 CLR 对象转换
    /// </summary>
    public static class RbConverter
    {
        /// <summary>
        /// 将托管对象转换为 Ruby 对象
        /// </summary>
        public static RbObject ToRubyValue(object value)
        {
            if (value is null) return RbTypeMap.Qnil;
            if (value is RbObject rbObject) return rbObject;
            if (value is VALUE rbValue) return rbValue.Obj;

            var objType = value.GetType();

            if (objType.IsGenericType && objType.Name.StartsWith("<>f__AnonymousType"))
            {
                return new RbHash(value);
            }

            if (value is IDictionary dictionary)
            {
                return new RbHash(dictionary);
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                return new RbArray(enumerable.Cast<object>().ToArray());
            }

            switch (Type.GetTypeCode(objType))
            {
                case TypeCode.Boolean:
                    return (bool)value ? RbTypeMap.Qtrue : RbTypeMap.Qfalse;
                case TypeCode.Char:
                    return new RbString(value.ToString());
                case TypeCode.String:
                    return new RbString((string)value);
                case TypeCode.Byte:
                    return new RbInt((byte)value);
                case TypeCode.SByte:
                    return new RbInt((sbyte)value);
                case TypeCode.Int16:
                    return new RbInt((short)value);
                case TypeCode.Int32:
                    return new RbInt((int)value);
                case TypeCode.Int64:
                    return new RbInt((long)value);
                case TypeCode.UInt16:
                    return new RbInt((ushort)value);
                case TypeCode.UInt32:
                    return new RbInt((long)(uint)value);
                case TypeCode.UInt64:
                    var ulongValue = (ulong)value;
                    return ulongValue <= long.MaxValue ? new RbInt((long)ulongValue) : new RbFloat(ulongValue);
                case TypeCode.Single:
                    return new RbFloat((float)value);
                case TypeCode.Double:
                    return new RbFloat((double)value);
                case TypeCode.Decimal:
                    return new RbFloat((double)(decimal)value);
                default:
                    return RbTypeMap.Qnil;
            }
        }

        /// <summary>
        /// 将 Ruby 对象转换为托管对象
        /// </summary>
        public static bool ToManagedValue(RbObject value, Type type, out object result)
        {
            result = null;
            if (value is null || value.IsNil) return true;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var array = value.Invoke("to_a");
                var length = array.Length();
                var managedArray = Array.CreateInstance(elementType, length);

                for (int i = 0; i < length; i++)
                {
                    ToManagedValue(array.GetItem(i), elementType, out var item);
                    managedArray.SetValue(item, i);
                }

                result = managedArray;
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(type);
                var array = value.Invoke("to_a");
                var length = array.Length();

                for (int i = 0; i < length; i++)
                {
                    ToManagedValue(array.GetItem(i), elementType, out var item);
                    list.Add(item);
                }

                result = list;
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var genericArgs = type.GetGenericArguments();
                var keyType = genericArgs[0];
                var valueType = genericArgs[1];
                var dictionary = (IDictionary)Activator.CreateInstance(type);
                var pairs = value.Invoke("to_a");
                var length = pairs.Length();

                for (int i = 0; i < length; i++)
                {
                    var pair = pairs.GetItem(i);
                    ToManagedValue(pair.GetItem(0), keyType, out var key);
                    ToManagedValue(pair.GetItem(1), valueType, out var item);
                    dictionary.Add(key, item);
                }

                result = dictionary;
                return true;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    result = !value.IsNil && value.Pointer != RbTypeMap.Qfalse.Pointer;
                    return true;
                case TypeCode.String:
                    result = value.ToString();
                    return true;
                case TypeCode.Byte:
                    result = (byte)Runtime.rb_num2int(value.Ref);
                    return true;
                case TypeCode.SByte:
                    result = (sbyte)Runtime.rb_num2int(value.Ref);
                    return true;
                case TypeCode.Int16:
                    result = (short)Runtime.rb_num2int(value.Ref);
                    return true;
                case TypeCode.Int32:
                    result = Runtime.rb_num2int(value.Ref);
                    return true;
                case TypeCode.Int64:
                    result = Runtime.rb_num2ll(value.Ref);
                    return true;
                case TypeCode.UInt16:
                    result = (ushort)Runtime.rb_num2int(value.Ref);
                    return true;
                case TypeCode.UInt32:
                    result = (uint)Runtime.rb_num2ll(value.Ref);
                    return true;
                case TypeCode.UInt64:
                    result = (ulong)Runtime.rb_num2ll(value.Ref);
                    return true;
                case TypeCode.Single:
                    result = (float)Runtime.rb_num2dbl(value.Ref);
                    return true;
                case TypeCode.Double:
                    result = Runtime.rb_num2dbl(value.Ref);
                    return true;
                case TypeCode.Decimal:
                    result = (decimal)Runtime.rb_num2dbl(value.Ref);
                    return true;
                case TypeCode.Object:
                    result = value;
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// 托管对象扩展方法
    /// </summary>
    public static class ManagedObjectExtensions
    {
        /// <summary>
        /// 转为 Ruby 对象
        /// </summary>
        public static RbObject ToRuby(this object obj)
        {
            return RbConverter.ToRubyValue(obj);
        }
    }
}
