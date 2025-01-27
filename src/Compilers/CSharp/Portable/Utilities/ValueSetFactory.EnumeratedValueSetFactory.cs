// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set factory that only supports equality and works by including or excluding specific values.
        /// </summary>
        private sealed class EnumeratedValueSetFactory<T> : IValueSetFactory<T> where T : notnull
        {
            private readonly IEquatableValueTC<T> _tc;

            IValueSet IValueSetFactory.AllValues => EnumeratedValueSet<T>.AllValues(_tc);

            IValueSet IValueSetFactory.NoValues => EnumeratedValueSet<T>.NoValues(_tc);

            public EnumeratedValueSetFactory(IEquatableValueTC<T> tc) { _tc = tc; }

            public IValueSet<T> Related(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case Equal:
                        return EnumeratedValueSet<T>.Including(value, _tc);
                    default:
                        return EnumeratedValueSet<T>.AllValues(_tc); // supported for error recovery
                }
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad || value.IsNull ? EnumeratedValueSet<T>.AllValues(_tc) : this.Related(relation, _tc.FromConstantValue(value));

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                Debug.Assert(relation == BinaryOperatorKind.Equal);
                return _tc.FromConstantValue(left).Equals(_tc.FromConstantValue(right));
            }

            public IValueSet Random(int expectedSize, Random random)
            {
                T[] values = _tc.RandomValues(expectedSize, random, expectedSize * 2);
                IValueSet<T> result = EnumeratedValueSet<T>.NoValues(_tc);
                Debug.Assert(result.IsEmpty);
                foreach (T value in values)
                    result = result.Union(Related(Equal, value));

                return result;
            }

            ConstantValue IValueSetFactory.RandomValue(Random random)
            {
                return _tc.ToConstantValue(_tc.RandomValues(1, random, 100)[0]);
            }
        }
    }
}
