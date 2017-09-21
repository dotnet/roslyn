// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPropertyReferenceExpression : IMemberReferenceExpression, IHasArgumentsExpression
    {
        /// <summary>
        /// Referenced property.
        /// </summary>
        IPropertySymbol Property { get; }
    }
}

