// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a reference to a member of a class, struct, or module that is dynamically bound.
    /// <para>
    /// Current usage:
    ///  (1) C# dynamic member reference expression.
    ///  (2) VB late bound member reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicMemberReferenceOperation : IOperation
    {
        /// <summary>
        /// Instance receiver. In VB, this can be null.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// Referenced member.
        /// </summary>
        string MemberName { get; }
        /// <summary>
        /// Type arguments.
        /// </summary>
        ImmutableArray<ITypeSymbol> TypeArguments { get; }
        /// <summary>
        /// The containing type of the referenced member, if different from type of the <see cref="Instance" />.
        /// </summary>
        ITypeSymbol ContainingType { get; }
    }
}
