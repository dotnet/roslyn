﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISyntheticLocalReferenceExpression : IOperation
    {
        /// <summary>
        /// Kind of the synthetic local.
        /// </summary>
        SyntheticLocalKind SyntheticLocalKind { get; }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        IOperation ContainingStatement { get; }
    }
}

