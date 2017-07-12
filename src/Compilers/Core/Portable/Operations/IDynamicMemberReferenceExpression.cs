// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a dynamic reference to a member of a class, struct, or module.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicMemberReferenceExpression : IOperation
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
        /// The containing type of this expression. In C#, this will always be null.
        /// </summary>
        ITypeSymbol ContainingType { get; }
    }
}
