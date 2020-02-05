// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

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
            private BoolValueSetFactory() { }
            public static readonly BoolValueSetFactory Instance = new BoolValueSetFactory();
            IValueSet<bool> IValueSetFactory<bool>.All => BoolValueSet.AllValues;
            IValueSet<bool> IValueSetFactory<bool>.None => BoolValueSet.None;

            IValueSet IValueSetFactory.All => BoolValueSet.AllValues;
            IValueSet IValueSetFactory.None => BoolValueSet.None;

            public IValueSet<bool> Related(BinaryOperatorKind relation, bool value) => (relation, value) switch
            {
                (Equal, true) => BoolValueSet.OnlyTrue,
                (Equal, false) => BoolValueSet.OnlyFalse,
                var _ => throw new ArgumentException("relation"),
            };

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value)
            {
                Debug.Assert(value.IsBoolean);
                return Related(relation, value.BooleanValue);
            }
        }
    }
}
