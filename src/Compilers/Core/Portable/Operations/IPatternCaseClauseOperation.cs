// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a case clause with a pattern and an optional guard operation.
    /// <para>
    /// Current usage:
    ///  (1) C# pattern case clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPatternCaseClauseOperation : ICaseClauseOperation
    {
        /// <summary>
        /// Label associated with the case clause.
        /// https://github.com/dotnet/roslyn/issues/27602: Similar property was added to the base interface, consider if we can remove this one.
        /// </summary>
        new ILabelSymbol Label { get; }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        IPatternOperation Pattern { get; }
        /// <summary>
        /// Guard associated with the pattern case clause.
        /// </summary>
        IOperation Guard { get; }
    }
}
