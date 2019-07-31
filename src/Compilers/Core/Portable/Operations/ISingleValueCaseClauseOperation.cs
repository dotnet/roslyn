// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a case clause with a single value for comparison.
    /// <para>
    /// Current usage:
    ///  (1) C# case clause of the form "case x"
    ///  (2) VB case clause of the form "Case x".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISingleValueCaseClauseOperation : ICaseClauseOperation
    {
        /// <summary>
        /// Case value.
        /// </summary>
        IOperation Value { get; }
    }
}
