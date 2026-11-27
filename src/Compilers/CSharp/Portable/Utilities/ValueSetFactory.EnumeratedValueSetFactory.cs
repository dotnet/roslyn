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
        private sealed class EnumeratedValueSetFactory<T> : IConstantValueSetFactory<T> where T : notnull
        {
            private readonly IEquatableValueTC<T> _tc;

            IConstantValueSet IConstantValueSetFactory.AllValues => EnumeratedValueSet<T>.AllValues(_tc);

            IConstantValueSet IConstantValueSetFactory.NoValues => EnumeratedValueSet<T>.NoValues(_tc);

            public EnumeratedValueSetFactory(IEquatableValueTC<T> tc) { _tc = tc; }

            public IConstantValueSet<T> Related(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case Equal:
                        return EnumeratedValueSet<T>.Including(value, _tc);
                    default:
                        return EnumeratedValueSet<T>.AllValues(_tc); // supported for error recovery
                }
            }

            IConstantValueSet IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad || value.IsNull ? EnumeratedValueSet<T>.AllValues(_tc) : this.Related(relation, _tc.FromConstantValue(value));

            bool IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                Debug.Assert(relation == BinaryOperatorKind.Equal);
                return _tc.FromConstantValue(left).Equals(_tc.FromConstantValue(right));
            }

            public IConstantValueSet Random(int expectedSize, Random random)
            {
                T[] values = _tc.RandomValues(expectedSize, random, expectedSize * 2);
                IConstantValueSet<T> result = EnumeratedValueSet<T>.NoValues(_tc);
                Debug.Assert(result.IsEmpty);
                foreach (T value in values)
                    result = result.Union(Related(Equal, value));

                return result;
            }

            ConstantValue IConstantValueSetFactory.RandomValue(Random random)
            {
                return _tc.ToConstantValue(_tc.RandomValues(1, random, 100)[0]);
            }
        }
    }
}
