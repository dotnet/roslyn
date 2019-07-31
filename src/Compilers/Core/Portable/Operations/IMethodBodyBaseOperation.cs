// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a method body operation.
    /// <para>
    /// Current usage:
    ///  (1) C# method body
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMethodBodyBaseOperation : IOperation
    {
        /// <summary>
        /// Method body corresponding to BaseMethodDeclarationSyntax.Body or AccessorDeclarationSyntax.Body
        /// </summary>
        IBlockOperation BlockBody { get; }
        /// <summary>
        /// Method body corresponding to BaseMethodDeclarationSyntax.ExpressionBody or AccessorDeclarationSyntax.ExpressionBody
        /// </summary>
        IBlockOperation ExpressionBody { get; }
    }
}
