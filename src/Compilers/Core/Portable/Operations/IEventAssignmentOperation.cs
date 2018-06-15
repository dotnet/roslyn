// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a binding of an event.
    /// <para>
    /// Current usage:
    ///  (1) C# event assignment expression.
    ///  (2) VB Add/Remove handler statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IEventAssignmentOperation : IOperation
    {
        /// <summary>
        /// Reference to the event being bound.
        /// </summary>
        IOperation EventReference { get; }
        
        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        IOperation HandlerValue { get; }

        /// <summary>
        /// True for adding a binding, false for removing one.
        /// </summary>
        bool Adds { get; }
    }
}

