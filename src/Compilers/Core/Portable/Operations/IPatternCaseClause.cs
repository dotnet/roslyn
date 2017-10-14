// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# pattern case clause.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPatternCaseClause : ICaseClause
    {
        /// <summary>
        /// Label associated with the case clause.
        /// </summary>
        ILabelSymbol Label { get; }

        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        IPattern Pattern { get; }

        /// <summary>
        /// Guard expression associated with the pattern case clause.
        /// </summary>
        IOperation GuardExpression { get; }
    }
}

