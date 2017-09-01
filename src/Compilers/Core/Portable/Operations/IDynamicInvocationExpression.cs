// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a dynamically bound invocation expression in C# and late bound invocation in VB.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicInvocationExpression : IHasDynamicArgumentsExpression
    {
        /// <summary>
        /// Dynamically or late bound expression.
        /// </summary>
        IOperation Expression { get; }
    }
}
