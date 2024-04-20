using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FrameGraph
{
    public class PriorityQueue<T> : IEnumerable<T> where T : IComparable<T>
    {
        private readonly List<T> _queue;
        private readonly IEnumerator<T> _enumerator;

        public PriorityQueue()
        {
            _queue = new List<T>();
            _enumerator = new Enumerator(_queue, true);
        }

        public PriorityQueue(int initCapacity)
        {
        }

        public int Count => _queue.Count;

        public void Enqueue(T item)
        {
            _queue.Add(item);
            int index = _queue.Count - 1;
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                var parent = _queue[parentIndex];
                var current = _queue[index];
                if (parent.CompareTo(current) >= 0)
                {
                    break;
                }

                _queue[index] = parent;
                _queue[parentIndex] = current;
                index = parentIndex;
            }
        }

        public T Dequeue()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            T item = _queue[0];
            _queue[0] = _queue[_queue.Count - 1];
            _queue.RemoveAt(_queue.Count - 1);
            Collapse(_queue, 0);
            return item;
        }

        public IEnumerator<T> GetEnumerator()
        {
            _enumerator.Reset();
            return _enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Enumerator Inverse()
        {
            var enumerator = new Enumerator(_queue, false);
            return enumerator;
        }

        private void Collapse(List<T> queue, int index)
        {
            while (index < queue.Count - 1)
            {
                int leftChildIndex = (2 * index) + 1;
                int rightChildIndex = (2 * index) + 2;
                int smallestIndex = index;
                if (leftChildIndex < queue.Count && queue[leftChildIndex].CompareTo(queue[smallestIndex]) < 0)
                {
                    smallestIndex = leftChildIndex;
                }

                if (rightChildIndex < queue.Count && queue[rightChildIndex].CompareTo(queue[smallestIndex]) < 0)
                {
                    smallestIndex = rightChildIndex;
                }

                if (smallestIndex != index)
                {
                    (queue[index], queue[smallestIndex]) = (queue[smallestIndex], queue[index]);
                    index = smallestIndex;
                }
                else
                {
                    break;
                }
            }
        }

        public struct Enumerator : IEnumerator<T>, IEnumerable<T>
        {
            private List<T> _queue;
            private readonly bool _forward;
            private T _value;
            private int _index;

            public Enumerator(List<T> queue, bool forward = true)
            {
                _forward = forward;
                _queue = queue;
                _value = default;

                if (_forward)
                {
                    _index = -1;
                }
                else
                {
                    _index = queue.Count;
                }
            }

            public void Dispose()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_forward)
                {
                    _index++;
                }
                else
                {
                    _index--;
                }

                if (0 <= _index && _index < _queue.Count)
                {
                    _value = _queue[_index];
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                if (_forward)
                {
                    _index = -1;
                }
                else
                {
                    _index = _queue.Count;
                }
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _value;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Current;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }
}
