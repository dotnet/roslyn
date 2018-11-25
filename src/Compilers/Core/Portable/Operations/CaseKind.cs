// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kinds of cases.
    /// </summary>
    public enum CaseKind
    {
        /// <summary>
        /// Represents unknown case kind.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Indicates an <see cref="ISingleValueCaseClauseOperation"/> in C# or VB.
        /// </summary>
        SingleValue = 0x1,

        /// <summary>
        /// Indicates an <see cref="IRelationalCaseClauseOperation"/> in VB.
        /// </summary>
        Relational = 0x2,

        /// <summary>
        /// Indicates an <see cref="IRangeCaseClauseOperation"/> in VB.
        /// </summary>
        Range = 0x3,

        /// <summary>
        /// Indicates an <see cref="IDefaultCaseClauseOperation"/> in C# or VB.
        /// </summary>
        Default = 0x4,

        /// <summary>
        /// Indicates an <see cref="IPatternCaseClauseOperation" /> in C#.
        /// </summary>
        Pattern = 0x5
    }
}

