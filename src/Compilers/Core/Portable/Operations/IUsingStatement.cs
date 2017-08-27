﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IUsingStatement : IOperation
    {
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        IOperation Body { get; }

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        IVariableDeclarationStatement Declaration { get; }

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        IOperation Value { get; }
    }
}

