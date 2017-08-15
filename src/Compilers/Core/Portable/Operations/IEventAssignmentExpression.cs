// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IEventAssignmentExpression : IOperation
    {
        /// <summary>
        /// Event being bound.
        /// </summary>
        IEventSymbol Event { get; }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        IOperation EventInstance { get; }

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

