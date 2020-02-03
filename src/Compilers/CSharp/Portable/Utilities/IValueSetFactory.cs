// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal interface IValueSetFactory
    {
        /// <summary>
        /// Returns a value set that includes all values of its type.
        /// </summary>
        IValueSet All { get; }
        /// <summary>
        /// Returns a value set that includes no values of its type.
        /// </summary>
        IValueSet None { get; }
        /// <summary>
        /// Returns a value set that includes any values that satisfy the given relation when compared to the given value.
        /// </summary>
        IValueSet Related(BinaryOperatorKind relation, ConstantValue value);
    }

    /// <summary>
    /// A value set factory, which can be used to create a value set instance.
    /// </summary>
    internal interface IValueSetFactory<T> : IValueSetFactory
    {
        /// <summary>
        /// Returns a value set that includes all values of type <typeparamref name="T"/>
        /// </summary>
        new IValueSet<T> All { get; }
        /// <summary>
        /// Returns a value set that includes no values.
        /// </summary>
        new IValueSet<T> None { get; }
        /// <summary>
        /// Returns a value set that includes any values that satisfy the given relation when compared to the given value.
        /// </summary>
        IValueSet<T> Related(BinaryOperatorKind relation, T value);
    }
}
