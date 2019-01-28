// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Describes different kinds of instance references for an operation used as instance.
    /// </summary>
    public enum InstanceReferenceKind
    {
        /// <summary>
        /// Reference to no instance or an object instance through a symbol, i.e. instance is not an <see cref="IInstanceReferenceOperation"/>.
        /// </summary>
        None,

        /// <summary>
        /// Reference to 'this' or 'Me' instance through an <see cref="IInstanceReferenceOperation"/>.
        /// </summary>
        This,

        /// <summary>
        /// Reference to 'base' or 'MyBase' instance through an <see cref="IInstanceReferenceOperation"/>.
        /// </summary>
        Base,

        /// <summary>
        /// Reference to 'MyClass' instance in VB through an <see cref="IInstanceReferenceOperation"/>.
        /// </summary>
        MyClass,

        /// <summary>
        /// Reference to object or anonymous type instance being created through an <see cref="IInstanceReferenceOperation"/>.
        /// </summary>
        Creation,
    }
}
