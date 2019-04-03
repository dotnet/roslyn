// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents either a namespace or a type declaration.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface INamespaceOrTypeDeclaration
    {
        /// <summary>
        /// Get the declaration name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get all the children namespace and type declarations of this declaration.
        /// </summary>
        ImmutableArray<INamespaceOrTypeDeclaration> Children { get; }

        /// <summary>
        /// Returns true if this declaration is a namespace. If it is not a namespace, it must be a type.
        /// </summary>
        bool IsNamespace { get; }

        /// <summary>
        /// Returns true if this declaration is a type. If it is not a type, it must be a namespace.
        /// </summary>
        bool IsType { get; }
    }
}
