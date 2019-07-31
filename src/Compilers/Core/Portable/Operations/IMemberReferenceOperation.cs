// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// <para>
    /// Current usage:
    ///  (1) C# member reference expression.
    ///  (2) VB member reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMemberReferenceOperation : IOperation
    {
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// Referenced member.
        /// </summary>
        ISymbol Member { get; }
    }
}
