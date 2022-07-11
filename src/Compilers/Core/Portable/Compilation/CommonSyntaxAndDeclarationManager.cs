// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    public readonly struct SyntaxTreeList : IEnumerable<SyntaxTree>
    {
        /// <summary>
        /// Items before the <see cref="_middle"/> item.  Can be empty.
        /// </summary>
        private readonly ImmutableArray<SyntaxTree> _before;

        /// <summary>
        /// The middle item.  Special cased so we can efficiently replace it, producing a new list that can point at the
        /// _before/_after arrays.  If null, then _after must be empty.
        /// </summary>
        private readonly SyntaxTree? _middle;

        /// <summary>
        /// Items after the <see cref="_middle"/> item.  Can be empty.  If non-empty, then _middle must be non-null.
        /// </summary>
        private readonly ImmutableArray<SyntaxTree> _after;

        public static readonly SyntaxTreeList Empty = new(ImmutableArray<SyntaxTree>.Empty, middle: null, ImmutableArray<SyntaxTree>.Empty);

        private SyntaxTreeList(ImmutableArray<SyntaxTree> before, SyntaxTree? middle, ImmutableArray<SyntaxTree> after)
        {
            if (middle == null)
                Debug.Assert(after.Length == 0);

            if (after.Length > 0)
                Debug.Assert(middle != null);

            _before = before;
            _middle = middle;
            _after = after;
        }

        public bool IsDefault
            => _before.IsDefault;

        public bool IsEmpty
            => this.Count == 0;

        public bool IsDefaultOrEmpty
            => IsDefault || IsEmpty;

        public int Count
            => (_before.Length + _after.Length) + (_middle == null ? 0 : 1);

        public SyntaxTree this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Count)
                    throw new IndexOutOfRangeException(nameof(index));

                if (index < _before.Length)
                    return _before[index];

                index -= _before.Length;
                if (index == 0)
                {
                    Debug.Assert(_middle != null);
                    return _middle;
                }

                index--;
                return _after[index];
            }
        }

        public ImmutableArray<T> SelectAsArray<T>(Func<SyntaxTree, T> func)
        {
            var result = ArrayBuilder<T>.GetInstance(this.Count);
            foreach (var tree in this)
                result.Add(func(tree));

            Debug.Assert(result.Count == this.Count);
            return result.ToImmutableAndFree();
        }

        internal SyntaxTreeList AddRange(IEnumerable<SyntaxTree> trees)
        {
            if (_middle != null)
            {
                // we already have a middle, so just add all the new trees into the after section.
                return new(_before, _middle, _after.AddRange(trees));
            }
            else
            {
                // we don't have a middle (and thus we don't have an 'after' either).  So we can just add this to the
                // before section.
                Debug.Assert(_after.IsEmpty);
                return new(_before.AddRange(trees), middle: null, after: ImmutableArray<SyntaxTree>.Empty);
            }
        }

        public SyntaxTree? FirstOrDefault()
            => this.IsEmpty ? null : this[0];

        public SyntaxTree? SingleOrDefault()
            => this.IsEmpty ? null : this.Count == 1 ? this[0] : throw new InvalidOperationException();

        public bool Any(Func<SyntaxTree, bool> func)
        {
            foreach (var tree in this)
            {
                if (func(tree))
                    return true;
            }

            return false;
        }

        public bool All(Func<SyntaxTree, bool> func)
        {
            foreach (var tree in this)
            {
                if (!func(tree))
                    return false;
            }

            return true;
        }

        public int IndexOf(SyntaxTree tree)
        {
            int index = 0;
            foreach (var child in this)
            {
                if (child == tree)
                    return index;

                index++;
            }

            return -1;
        }

        public bool Contains(SyntaxTree tree)
        {
            foreach (var child in this)
            {
                if (child == tree)
                    return true;
            }

            return false;
        }

        public SyntaxTreeList RemoveAll(Func<SyntaxTree, bool> predicate)
        {
            var builder = new Builder();
            foreach (var tree in this)
            {
                if (!predicate(tree))
                    builder.Add(tree);
            }

            return builder.ToImmutableAndFree();
        }

        public SyntaxTreeList SetItem(int index, SyntaxTree tree)
        {
            int count = this.Count;
            if (index < 0 || index >= count)
                throw new IndexOutOfRangeException(nameof(index));

            if (index == _before.Length)
            {
                Debug.Assert(_middle != null);
                return new(_before, tree, _after);
            }

            var before = ArrayBuilder<SyntaxTree>.GetInstance(index);
            var after = ArrayBuilder<SyntaxTree>.GetInstance(count - (1 + index));

            for (var i = 0; i < index; i++)
                before.Add(this[i]);

            for (var i = index + 1; i < count; i++)
                after.Add(this[i]);

            Debug.Assert(before.Count == index);
            Debug.Assert(after.Count == count - (1 + index));

            return new(before.ToImmutableAndFree(), tree, after.ToImmutableAndFree());
        }

        public SyntaxTreeList Replace(SyntaxTree old, SyntaxTree @new)
        {
            var index = 0;
            foreach (var tree in this)
            {
                if (tree == old)
                    return this.SetItem(index, @new);

                index++;
            }

            return this;
        }

        public static Builder CreateBuilder()
            => new Builder();

        public struct Builder
        {
            private readonly ArrayBuilder<SyntaxTree> _builder = ArrayBuilder<SyntaxTree>.GetInstance();

            public Builder()
            {
            }

            public SyntaxTreeList ToImmutableAndFree()
            {
                return new SyntaxTreeList(_builder.ToImmutableAndFree(), middle: null, ImmutableArray<SyntaxTree>.Empty);
            }

            public void Add(SyntaxTree tree)
                => _builder.Add(tree);

            public void AddRange(SyntaxTreeList syntaxTrees)
            {
                _builder.AddRange(syntaxTrees._before);
                _builder.AddIfNotNull(syntaxTrees._middle);
                _builder.AddRange(syntaxTrees._after);
            }
        }

        public Enumerator GetEnumerator()
            => new(this);

        IEnumerator<SyntaxTree> IEnumerable<SyntaxTree>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public struct Enumerator : IEnumerator<SyntaxTree>
        {
            private readonly SyntaxTreeList _syntaxTreeList;
            private int _index = -1;

            public Enumerator(SyntaxTreeList syntaxTreeList)
            {
                _syntaxTreeList = syntaxTreeList;
            }

            public bool MoveNext()
                => ++_index < _syntaxTreeList.Count;

            public SyntaxTree Current
                => _syntaxTreeList[_index];

            object? IEnumerator.Current
                => this.Current;

            public void Reset()
                => _index = -1;

            public void Dispose()
            {
            }
        }
    }

    internal abstract class CommonSyntaxAndDeclarationManager
    {
        internal readonly SyntaxTreeList ExternalSyntaxTrees;
        internal readonly string ScriptClassName;
        internal readonly SourceReferenceResolver Resolver;
        internal readonly CommonMessageProvider MessageProvider;
        internal readonly bool IsSubmission;

        public CommonSyntaxAndDeclarationManager(
            SyntaxTreeList externalSyntaxTrees,
            string scriptClassName,
            SourceReferenceResolver resolver,
            CommonMessageProvider messageProvider,
            bool isSubmission)
        {
            this.ExternalSyntaxTrees = externalSyntaxTrees;
            this.ScriptClassName = scriptClassName ?? "";
            this.Resolver = resolver;
            this.MessageProvider = messageProvider;
            this.IsSubmission = isSubmission;
        }
    }
}
