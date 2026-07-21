using System;
using System.Collections;
using System.Collections.Generic;

namespace RubyCore
{
    /// <summary>
    /// Ruby 对象迭代器
    /// <para>优先通过 Ruby C API 调用 <c>each</c> 取得快照，再按索引遍历</para>
    /// </summary>
    public class RbIterator : IEnumerator<RbObject>
    {
        private readonly RbObject _array;
        private readonly int _length;
        private int _index = -1;

        /// <summary>
        /// 为 Ruby 对象创建 CLR 迭代器
        /// <para>对象需要响应 <c>to_a</c> 或 <c>each</c></para>
        /// </summary>
        public RbIterator(RbObject obj)
        {
            if (obj is null) throw new ArgumentNullException(nameof(obj));

            if (Runtime.HasBlockCall && obj.RespondTo("each"))
            {
                var array = Runtime.rb_each_to_array_protect(obj.Ref, out int state);
                if (state != 0) RbException.CatchThrowToCLR();

                _array = array.Obj;
            }
            else if (obj.RespondTo("to_a"))
            {
                _array = obj.InvokeMethod("to_a");
            }
            else if (obj.RespondTo("each"))
            {
                _array = obj.InvokeMethod("enum_for", new RbSymbol("each")).InvokeMethod("to_a");
            }
            else
            {
                throw new InvalidOperationException($"Ruby 对象不可迭代: {obj.Class}");
            }

            _length = _array.Length();
        }

        /// <summary>
        /// 当前迭代到的 Ruby 对象
        /// </summary>
        public RbObject Current { get; private set; }

        object IEnumerator.Current => Current;

        /// <summary>
        /// 移动到下一个 Ruby 对象
        /// </summary>
        public bool MoveNext()
        {
            _index++;
            if (_index >= _length)
            {
                Current = null;
                return false;
            }

            Current = _array.GetItem(_index);
            return true;
        }

        /// <summary>
        /// 重置迭代位置
        /// </summary>
        public void Reset()
        {
            _index = -1;
            Current = null;
        }

        /// <summary>
        /// 释放迭代器状态
        /// </summary>
        public void Dispose()
        {
            Current = null;
        }
    }
}
