// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
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
        /// Indicates case x in C# or Case x in VB.
        /// </summary>
        SingleValue = 0x1,

        /// <summary>
        /// Indicates Case Is op x in VB.
        /// </summary>
        Relational = 0x2,

        /// <summary>
        /// Indicates Case x To Y in VB.
        /// </summary>
        Range = 0x3,

        /// <summary>
        /// Indicates default in C# or Case Else in VB.
        /// </summary>
        Default = 0x4,

        /// <summary>
        /// Indicates pattern case in C#.
        /// </summary>
        Pattern = 0x5
    }
}

