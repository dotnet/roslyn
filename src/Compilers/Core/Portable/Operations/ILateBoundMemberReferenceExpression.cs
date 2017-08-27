// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILateBoundMemberReferenceExpression : IOperation
    {
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// Name of the member.
        /// </summary>
        string MemberName { get; }
    }
}

