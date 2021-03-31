// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public class ValueSources
    {
        //TODO: really, we need *two* value sources.
        //      one that we pass to the generators to register from
        //      and a second that we can update as we go.
        //      when the graph runs, we replace the ones we passed to the generator
        //      with the 'real' latest ones.

        //TODO: we can also track which sources are actually
        //      used, and not bother running when only an unused
        //      source has changed

        public IncrementalValueSource<Compilation> CompilationSource { get; }

        public IncrementalValueSource<string> Strings { get; }

        public IncrementalValueSource<SyntaxTree> SyntaxTrees { get; }

        public IncrementalValueSource<T> CreateSyntaxTransform<T>(Func<GeneratorSyntaxContext, IEnumerable<T>> func) => throw null!;


        private ValueSources(Compilation compilation, string[] additionalTexts)
        {
            this.CompilationSource = new IncrementalValueSource<Compilation>(new SingleItemValueProvider<Compilation>(compilation));
            this.Strings = new IncrementalValueSource<string>(new MultiItemValueProvider<string>(additionalTexts));
        }


        internal static ValueSources Create(Compilation compilation)
        {
            return new ValueSources(compilation, new string[] { "abc", "def", "ghi", "jkl", "mno", "pqr", "stu", "vwx", "yz" });
        }

    }

    class SingleItemValueProvider<T> : INode<T>
    {
        private T _value;

        private EntryState _state;

        internal SingleItemValueProvider(T initialValue)
        {
            _value = initialValue;
            _state = EntryState.Added;
        }

        public StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<T> previousTable)
        {
            var tableBuilder = new StateTable<T>.Builder();
            tableBuilder.AddEntries(ImmutableArray.Create(_value), _state);
            _state = EntryState.Cached;
            return tableBuilder.ToImmutableAndFree();
        }

        public INode<T> WithComparer(IEqualityComparer<T> comparer)
        {
            throw new NotImplementedException();
        }

        internal void UpdateValue(T newValue)
        {
            _value = newValue;
            _state = EntryState.Modified;
        }
    }

    class MultiItemValueProvider<T> : INode<T>
    {
        StateTable<T>.Builder _currentBuilder = new StateTable<T>.Builder();

        bool hasChanged = false;

        internal MultiItemValueProvider(IEnumerable<T> initialValue)
        {
            FillBuilder(initialValue, EntryState.Added);
        }

        //TODO: is there a way we can add a 'dummy' entry to the state table that will hold our own state?

        public StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<T> previousTable)
        {
            if (!hasChanged)
                return previousTable;

            //PROTOYPE: threading. This can race. need to atomically free and replace
            var newTable = _currentBuilder.ToImmutableAndFree();
            _currentBuilder = new StateTable<T>.Builder();
            foreach (var entry in newTable)
            {
                if (entry.state != EntryState.Removed)
                {
                    _currentBuilder.AddEntries(ImmutableArray.Create(entry.item), EntryState.Cached);
                }
            }
            hasChanged = false;
            return newTable;
        }

        internal void ReplaceValues(IEnumerable<T> values)
        {
            //TODO: need to mark all the current entries as removed,
            //      then add all the new ones as added
        }

        internal void AddValue(T newValue)
        {
            _currentBuilder.AddEntries(ImmutableArray.Create(newValue), EntryState.Added);
            hasChanged = true;
        }

        internal void RemoveValue(int index)
        {
            _currentBuilder.RemoveEntries(index);
            hasChanged = true;
        }

        internal void UpdateValue(int index, T updatedValue)
        {
            _currentBuilder.SetEntries(index, ImmutableArray.Create((updatedValue, EntryState.Modified)));
            hasChanged = true;
        }

        internal void FillBuilder(IEnumerable<T> values, EntryState state)
        {
            foreach (var value in values)
            {
                _currentBuilder.AddEntries(ImmutableArray.Create(value), state);
            }
            hasChanged = true;
        }

        public INode<T> WithComparer(IEqualityComparer<T> comparer)
        {
            throw new NotImplementedException();
        }
    }
}
