using System.Collections;
using System.Collections.Generic;

namespace RubyCore
{
    /// <summary>
    /// Ruby 可迭代对象
    /// </summary>
    public class RbIterable : RbObject, IEnumerable<RbObject>
    {
        public RbIterable(VALUE refVal) : base(refVal)
        {
        }

        public RbIterator GetEnumerator()
        {
            return new RbIterator(this);
        }

        IEnumerator<RbObject> IEnumerable<RbObject>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
