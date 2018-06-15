// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a switch case section with one or more case clauses to match and one or more operations to execute within the section.
    /// <para>
    /// Current usage:
    ///  (1) C# switch section for one or more case clause and set of statements to execute.
    ///  (2) VB case block with a case statement for one or more case clause and set of statements to execute.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISwitchCaseOperation : IOperation
    {
        /// <summary>
        /// Clauses of the case.
        /// </summary>
        ImmutableArray<ICaseClauseOperation> Clauses { get; }
        /// <summary>
        /// One or more operations to execute within the switch section.
        /// </summary>
        ImmutableArray<IOperation> Body { get; }
        /// <summary>
        /// Locals declared within the switch case section scoped to the section.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
}

