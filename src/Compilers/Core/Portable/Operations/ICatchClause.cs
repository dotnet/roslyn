// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICatchClause : IOperation
    {
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        IBlockStatement Handler { get; }

        /// <summary>
        /// Type of the exception handled by the catch clause.
        /// </summary>
        ITypeSymbol ExceptionType { get; }

        /// <summary>
        /// Optional source for exception. This could be any of the following operation:
        /// 1. Declaration for the local catch variable bound to the caught exception (C# and VB) OR
        /// 2. Null, indicating no declaration or expression (C# and VB)
        /// 3. Reference to an existing local or parameter (VB) OR
        /// 4. An error expression (VB)
        /// </summary>
        IOperation ExceptionDeclarationOrExpression { get; }

        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        IOperation Filter { get; }
    }
}

