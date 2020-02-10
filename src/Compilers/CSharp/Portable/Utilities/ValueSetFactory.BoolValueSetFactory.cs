// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set factory for boolean values.
        /// </summary>
        private class BoolValueSetFactory : IValueSetFactory<bool>
        {
            public static readonly BoolValueSetFactory Instance = new BoolValueSetFactory();

            private BoolValueSetFactory() { }

            IValueSet<bool> IValueSetFactory<bool>.All => BoolValueSet.AllValues;

            IValueSet IValueSetFactory.All => BoolValueSet.AllValues;

            IValueSet<bool> IValueSetFactory<bool>.None => BoolValueSet.None;

            IValueSet IValueSetFactory.None => BoolValueSet.None;

            public IValueSet<bool> Related(BinaryOperatorKind relation, bool value)
            {
                switch (relation, value)
                {
                    case (Equal, true):
                        return BoolValueSet.OnlyTrue;
                    case (Equal, false):
                        return BoolValueSet.OnlyFalse;
                    default:
                        throw new ArgumentException("relation");
                }
            }

            IValueSet<bool> IValueSetFactory<bool>.Random(int expectedSize, Random random) => random.Next(4) switch
            {
                0 => BoolValueSet.None,
                1 => BoolValueSet.OnlyFalse,
                2 => BoolValueSet.OnlyTrue,
                3 => BoolValueSet.AllValues,
                _ => throw ExceptionUtilities.UnexpectedValue("random"),
            };

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value)
            {
                Debug.Assert(value.IsBoolean);
                return Related(relation, value.BooleanValue);
            }

            IValueSet<bool> IValueSetFactory<bool>.Related(BinaryOperatorKind relation, bool value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
