using System;
using System.Collections;
using System.Collections.Generic;

namespace RubyCore
{
    /// <summary>
    /// Ruby 对象迭代器
    /// <para>通过 to_a 取得快照后按索引遍历，避免在 CLR 迭代过程中依赖 Ruby block 回调</para>
    /// </summary>
    public class RbIterator : IEnumerator<RbObject>
    {
        private readonly RbObject _array;
        private readonly int _length;
        private int _index = -1;

        public RbIterator(RbObject obj)
        {
            if (obj is null) throw new ArgumentNullException(nameof(obj));

            _array = obj.Invoke("to_a");
            _length = _array.Length();
        }

        public RbObject Current { get; private set; }

        object IEnumerator.Current => Current;

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

        public void Reset()
        {
            _index = -1;
            Current = null;
        }

        public void Dispose()
        {
            Current = null;
        }
    }
}
