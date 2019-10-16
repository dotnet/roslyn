// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an event.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IEventSymbol : ISymbol
    {
        /// <summary>
        /// The type of the event. 
        /// </summary>
        ITypeSymbol Type { get; }

        /// <summary>
        /// The top-level nullability of the event.
        /// </summary>
        NullableAnnotation NullableAnnotation { get; }

        /// <summary>
        /// Returns true if the event is a WinRT type event.
        /// </summary>
        bool IsWindowsRuntimeEvent { get; }

        /// <summary>
        /// The 'add' accessor of the event.  Null only in error scenarios.
        /// </summary>
        IMethodSymbol? AddMethod { get; }

        /// <summary>
        /// The 'remove' accessor of the event.  Null only in error scenarios.
        /// </summary>
        IMethodSymbol? RemoveMethod { get; }

        /// <summary>
        /// The 'raise' accessor of the event.  Null if there is no raise method.
        /// </summary>
        IMethodSymbol? RaiseMethod { get; }

        /// <summary>
        /// The original definition of the event. If the event is constructed from another
        /// symbol by type substitution, OriginalDefinition gets the original symbol, as it was 
        /// defined in source or metadata.
        /// </summary>
        new IEventSymbol OriginalDefinition { get; }

        /// <summary>
        /// Returns the overridden event, or null.
        /// </summary>
        IEventSymbol? OverriddenEvent { get; }

        /// <summary>
        /// Returns interface properties explicitly implemented by this event.
        /// </summary>
        /// <remarks>
        /// Properties imported from metadata can explicitly implement more than one event.
        /// </remarks>
        ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; }
    }
}
