// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Linq;


namespace Microsoft.CodeAnalysis
{
    public class ValueSources
    {
        public SingleValueSource<Compilation> CompilationSource { get; }

        public MultiValueSource<string> Strings { get; }

        private ValueSources(Compilation compilation, string[] additionalTexts)
        {
            this.CompilationSource = new SingleValueSource<Compilation>(new SingleItemValueProvider<Compilation>(compilation));
            this.Strings = new MultiValueSource<string>(new MultiItemValueProvider<string>(additionalTexts));
        }


        internal static ValueSources Create(Compilation compilation)
        {
            return new ValueSources(compilation, new string[] { "abc", "def", "ghi", "jkl", "mno", "pqr", "stu", "vwx", "yz" });
        }

    }

    class SingleItemValueProvider<T> : AbstractNode<T>
    {
        private T _value;

        private EntryState _state;

        internal SingleItemValueProvider(T initialValue)
        {
            _value = initialValue;
            _state = EntryState.Added;
        }

        internal override StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<T> previousTable)
        {
            var tableBuilder = new StateTable<T>.Builder();
            tableBuilder.AddEntries(ImmutableArray.Create(_value), _state);
            _state = EntryState.Cached;
            return tableBuilder.ToImmutableAndFree();
        }

        internal void UpdateValue(T newValue)
        {
            _value = newValue;
            _state = EntryState.Modified;
        }
    }

    class MultiItemValueProvider<T> : AbstractNode<T>
    {
        StateTable<T>.Builder _currentBuilder = new StateTable<T>.Builder();

        bool hasChanged = false;

        bool isAllCached = false;

        internal MultiItemValueProvider(IEnumerable<T> initialValue)
        {
            FillBuilder(initialValue, EntryState.Added);
        }

        internal override StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<T> previousTable)
        {
            if (isAllCached && !hasChanged)
                return previousTable;

            //PROTOYPE: threading. This can race. need to atomically free and replace
            var newTable = _currentBuilder.ToImmutableAndFree();
            _currentBuilder = new StateTable<T>.Builder();
            foreach (var entry in newTable)
            {
                var state = entry.state == EntryState.Removed ? EntryState.Removed : EntryState.Cached;
               _currentBuilder.AddEntries(ImmutableArray.Create(entry.item), state);
            }
            isAllCached = true;
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
    }
}
