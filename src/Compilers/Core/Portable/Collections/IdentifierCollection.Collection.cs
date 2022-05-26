// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class IdentifierCollection
    {
        private abstract class CollectionBase : ICollection<string>
        {
            protected readonly IdentifierCollection IdentifierCollection;
            private int _count = -1;

            protected CollectionBase(IdentifierCollection identifierCollection)
            {
                this.IdentifierCollection = identifierCollection;
            }

            public abstract bool Contains(string item);

            public void CopyTo(string[] array, int arrayIndex)
            {
                using (var enumerator = this.GetEnumerator())
                {
                    while (arrayIndex < array.Length && enumerator.MoveNext())
                    {
                        array[arrayIndex] = enumerator.Current;
                        arrayIndex++;
                    }
                }
            }

            public int Count
            {
                get
                {
                    if (_count == -1)
                    {
                        _count = this.IdentifierCollection._map.Values.Sum(o => o is string ? 1 : ((ISet<string>)o).Count);
                    }

                    return _count;
                }
            }

            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                foreach (var obj in this.IdentifierCollection._map.Values)
                {
                    var strs = obj as HashSet<string>;
                    if (strs != null)
                    {
                        foreach (var s in strs)
                        {
                            yield return s;
                        }
                    }
                    else
                    {
                        yield return (string)obj;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #region Unsupported  
            public void Add(string item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Remove(string item)
            {
                throw new NotSupportedException();
            }
            #endregion
        }

        private sealed class CaseSensitiveCollection : CollectionBase
        {
            public CaseSensitiveCollection(IdentifierCollection identifierCollection) : base(identifierCollection)
            {
            }

            public override bool Contains(string item) => IdentifierCollection.CaseSensitiveContains(item);
        }

        private sealed class CaseInsensitiveCollection : CollectionBase
        {
            public CaseInsensitiveCollection(IdentifierCollection identifierCollection) : base(identifierCollection)
            {
            }

            public override bool Contains(string item) => IdentifierCollection.CaseInsensitiveContains(item);
        }
    }
}
