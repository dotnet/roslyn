// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a catch clause.
    /// <para>
    /// Current usage:
    ///  (1) C# catch clause.
    ///  (2) VB Catch clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICatchClauseOperation : IOperation
    {
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        IBlockOperation Handler { get; }
        /// <summary>
        /// Locals declared by the <see cref="ExceptionDeclarationOrExpression" /> and/or <see cref="Filter" /> clause.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Type of the exception handled by the catch clause.
        /// </summary>
        ITypeSymbol ExceptionType { get; }
        /// <summary>
        /// Optional source for exception. This could be any of the following operation:
        /// 1. Declaration for the local catch variable bound to the caught exception (C# and VB) OR
        /// 2. Null, indicating no declaration or expression (C# and VB)
        /// 3. Reference to an existing local or parameter (VB) OR
        /// 4. Other expression for error scenarios (VB)
        /// </summary>
        IOperation ExceptionDeclarationOrExpression { get; }
        /// <summary>
        /// Filter operation to be executed to determine whether to handle the exception.
        /// </summary>
        IOperation Filter { get; }
    }
}
