// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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
        /// Type of exception to be handled.
        /// </summary>
        ITypeSymbol CaughtType { get; }
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        IOperation Filter { get; }
        /// <summary>
        /// Symbol for the local catch variable bound to the caught exception.
        /// </summary>
        ILocalSymbol ExceptionLocal { get; }
    }
}

