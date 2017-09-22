// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# is pattern expression. For example, "x is int i".
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIsPatternExpression : IOperation
    {
        /// <summary>
        /// Expression.
        /// </summary>
        IOperation Expression { get; }

        /// <summary>
        /// Pattern.
        /// </summary>
        IPattern Pattern { get; }
    }
}

