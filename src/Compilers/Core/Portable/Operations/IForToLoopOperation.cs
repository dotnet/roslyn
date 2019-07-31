// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a for to loop with loop control variable and initial, limit and step values for the control variable.
    /// <para>
    /// Current usage:
    ///  (1) VB 'For ... To ... Step' loop statement
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IForToLoopOperation : ILoopOperation
    {
        /// <summary>
        /// Refers to the operation for declaring a new local variable or reference an existing variable or an expression.
        /// </summary>
        IOperation LoopControlVariable { get; }
        /// <summary>
        /// Operation for setting the initial value of the loop control variable. This comes from the expression between the 'For' and 'To' keywords.
        /// </summary>
        IOperation InitialValue { get; }
        /// <summary>
        /// Operation for the limit value of the loop control variable. This comes from the expression after the 'To' keyword.
        /// </summary>
        IOperation LimitValue { get; }
        /// <summary>
        /// Operation for the step value of the loop control variable. This comes from the expression after the 'Step' keyword,
        /// or inferred by the compiler if 'Step' clause is omitted.
        /// </summary>
        IOperation StepValue { get; }
        /// <summary>
        /// <code>true</code> if arithmetic operations behind this loop are 'checked'.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// Optional list of comma separated next variables at loop bottom.
        /// </summary>
        ImmutableArray<IOperation> NextVariables { get; }
    }
}
