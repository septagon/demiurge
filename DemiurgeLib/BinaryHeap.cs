using System;

namespace DemiurgeLib
{
    public class BinaryHeap<T> where T : IComparable<T>
    {
        private T[] heap;
        public int Count { get; private set; }

        public BinaryHeap(int startingSize = 64)
        {
            this.heap = new T[64];
            this.Count = 0;
        }

        public void Push(T t)
        {
            this.Count++;

            if (this.Count >= this.heap.Length)
            {
                T[] newHeap = new T[this.heap.Length * 2];
                Array.Copy(this.heap, newHeap, this.heap.Length);
                this.heap = newHeap;
            }

            this.heap[this.Count] = t;
            PercolateUp(this.Count);
        }

        public T Peek()
        {
            return this.heap[1];
        }

        public T Pop()
        {
            T ret = this.heap[1];

            this.heap[1] = this.heap[this.Count];
            this.heap[this.Count] = default(T);
            this.Count--;
            PercolateDown();

            return ret;
        }

        private void PercolateUp(int idx)
        {
            int p = idx / 2;

            if (this.heap[idx].CompareTo(this.heap[p]) < 0)
            {
                Swap(idx, p);
                PercolateUp(p);
            }
        }

        private void PercolateDown(int idx = 1)
        {
            int l = 2 * idx;
            int r = l + 1;

            if (l <= this.Count && r <= this.Count)
            {
                if (this.heap[l].CompareTo(this.heap[r]) < 0)
                {
                    if (this.heap[l].CompareTo(this.heap[idx]) < 0)
                    {
                        Swap(idx, l);
                        PercolateDown(l);
                    }
                }
                else
                {
                    if (this.heap[r].CompareTo(this.heap[idx]) < 0)
                    {
                        Swap(idx, r);
                        PercolateDown(r);
                    }
                }
            }
            else if (l <= this.Count)
            {
                if (this.heap[l].CompareTo(this.heap[idx]) < 0)
                {
                    Swap(idx, l);
                    PercolateDown(l);
                }
            }
        }

        private void Swap(int idx1, int idx2)
        {
            T buf = this.heap[idx1];
            this.heap[idx1] = this.heap[idx2];
            this.heap[idx2] = buf;
        }
    }
}
