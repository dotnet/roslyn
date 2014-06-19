using System.Collections.Generic;

namespace Roslyn.Compilers
{
    /// <summary>
    /// A _very_ simple implementation of the BCL's ConcurrentBag.  In .NET 4.0, there is a bug where
    /// objects in the bag are leaked until the thread where they are added exits.  Because we use
    /// them on ThreadPool based threads (via the TPL), we can't control when the thread exits.
    /// 
    /// The BCL bug is tracked by http://vstfdevdiv:8080/web/wi.aspx?id=14770, and we should
    /// consider removing this type and using the BCL's version once we are on a platform where it
    /// is supported.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SynchronizedBag<T> : IEnumerable<T>
    {
        private readonly List<T> list = new List<T>();

        /// <summary>
        /// NOTE: Be careful using this, because another thread could add/remove an item immediately
        /// after you call access it, which means it's value could be out of date by the time you
        /// act on it.         
        /// </summary>
        public bool DangerousIsEmpty
        {
            get
            {
                lock (list)
                {
                    return list.Count == 0;
                }
            }
        }

        public void Clear()
        {
            lock (list)
            {
                this.list.Clear();
            }
        }

        public void Add(T t)
        {
            lock (list)
            {
                this.list.Add(t);
            }
        }

        public bool TryTake(out T item)
        {
            lock (list)
            {
                if (list.Count > 0)
                {
                    item = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    return true;
                }
                else
                {
                    item = default(T);
                    return false;
                }
            }
        }

        public T[] ToArray()
        {
            lock (list)
            {
                return list.ToArray();
            }
        }

        public int Count
        {
            get
            {
                lock (list)
                {
                    return list.Count;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (list)
            {
                return new List<T>(list).GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
