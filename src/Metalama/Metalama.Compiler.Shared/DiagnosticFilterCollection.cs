// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler
{

    /// <summary>
    /// A collection of <see cref="DiagnosticFilter"/>, indexed by file path and diagnostic id. This collection is not thread-safe.
    /// </summary>
    public sealed class DiagnosticFilterCollection : ICollection<DiagnosticFilter>
    {
        private int _count;
        private readonly Dictionary<DiagnosticFilterKey, List<DiagnosticFilter>> _dictionary = new();
        private readonly HashSet<SuppressionDescriptor> _suppressionDescriptors = new HashSet<SuppressionDescriptor>();
        private bool _frozen;

        public int Count => _count;

        public bool IsReadOnly => _frozen;

        public IReadOnlyCollection<SuppressionDescriptor> SuppressionDescriptors => _suppressionDescriptors;

        public void Freeze() => _frozen = true;

        public void Add(DiagnosticFilter diagnosticFilter)
        public void Add(in DiagnosticFilter diagnosticFilter)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException();
            }

            var key = new DiagnosticFilterKey(diagnosticFilter.FilePath, diagnosticFilter.Descriptor.SuppressedDiagnosticId);
            if (!_dictionary.TryGetValue(key, out var list))
            {
                list = new List<DiagnosticFilter>();
                _dictionary.Add(key, list);
            }

            list.Add(diagnosticFilter);

            Interlocked.Increment(ref _count);
            _suppressionDescriptors.Add(diagnosticFilter.Descriptor);
        }

        public void UnionWith(DiagnosticFilterCollection other)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException();
            }

            other.Freeze();

            foreach ( var group in other._dictionary )
            {
                if ( this._dictionary.TryGetValue( group.Key, out var list ) )
                {
                    list.AddRange( group.Value );
                }
                else
                {
                    this._dictionary.Add( group.Key, group.Value );
                }
            }

            _count += other._count;
            _suppressionDescriptors.UnionWith( other._suppressionDescriptors );
        }

        public bool TryGetFilters(string filePath, string diagnosticId, out IReadOnlyList<DiagnosticFilter> filters)
        {
            var key = new DiagnosticFilterKey(filePath, diagnosticId);

            if (_dictionary.TryGetValue(key, out var list))
            {
                filters = list;
                return true;
            }
            else
            {
                filters = Array.Empty<DiagnosticFilter>();
                return false;
            }

         
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(DiagnosticFilter item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(DiagnosticFilter[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public IEnumerator<DiagnosticFilter> GetEnumerator()
        {
            foreach (var list in _dictionary.Values)
            {
                foreach (var item in list)
                {
                    yield return item;
                }
            }
        }

        public bool Remove(DiagnosticFilter item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

}
