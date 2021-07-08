// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Roslyn.Test.Utilities
{
    public sealed class EqualityUnit<T>
    {
        private static readonly ReadOnlyCollection<T> s_emptyCollection = new ReadOnlyCollection<T>(new T[] { });

        public readonly T Value;
        public readonly ReadOnlyCollection<T> EqualValues;
        public readonly ReadOnlyCollection<T> NotEqualValues;
        public IEnumerable<T> AllValues
        {
            get { return Enumerable.Repeat(Value, 1).Concat(EqualValues).Concat(NotEqualValues); }
        }

        public EqualityUnit(T value)
        {
            Value = value;
            EqualValues = s_emptyCollection;
            NotEqualValues = s_emptyCollection;
        }

        public EqualityUnit(
            T value,
            ReadOnlyCollection<T> equalValues,
            ReadOnlyCollection<T> notEqualValues)
        {
            Value = value;
            EqualValues = equalValues;
            NotEqualValues = notEqualValues;
        }

        public EqualityUnit<T> WithEqualValues(params T[] equalValues)
        {
            return new EqualityUnit<T>(
                Value,
                EqualValues.Concat(equalValues).ToList().AsReadOnly(),
                NotEqualValues);
        }

        public EqualityUnit<T> WithNotEqualValues(params T[] notEqualValues)
        {
            return new EqualityUnit<T>(
                Value,
                EqualValues,
                NotEqualValues.Concat(notEqualValues).ToList().AsReadOnly());
        }
    }
}
