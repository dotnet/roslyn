// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Cci = Microsoft.Cci;

// ^ using Microsoft.Contracts;

namespace Microsoft.Cci
{
    /// <summary>
    /// An object corresponding to a metadata entity such as a type or a field.
    /// </summary>
    internal interface IDefinition : IReference
    {
    }

    /// <summary>
    /// An object corresponding to reference to a metadata entity such as a type or a field.
    /// </summary>
    internal interface IReference
    {
        /// <summary>
        /// A collection of metadata custom attributes that are associated with this definition.
        /// </summary>
        IEnumerable<ICustomAttribute> GetAttributes(Microsoft.CodeAnalysis.Emit.Context context); // TODO: consider moving this to IDefinition, we shouldn't need to examine attributes on references.

        /// <summary>
        /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
        /// of the object implementing IDefinition. The dispatch method does not invoke Dispatch on any child objects. If child traversal
        /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
        /// </summary>
        void Dispatch(MetadataVisitor visitor);

        /// <summary>
        /// Gets the definition object corresponding to this reference within the given context, 
        /// or null if the referenced entity isn't defined in the context.
        /// </summary>
        IDefinition AsDefinition(Microsoft.CodeAnalysis.Emit.Context context);
    }

    /// <summary>
    /// An object that represents a document. This can be either source or binary or designer surface etc
    /// </summary>
    internal interface Document
    {
        /// <summary>
        /// The location where this document was found, or where it should be stored.
        /// This will also uniquely identify the source document within an instance of compilation host.
        /// </summary>
        string Location { get; }

        /// <summary>
        /// The name of the document. For example the name of the file if the document corresponds to a file.
        /// </summary>
        string Name { get; }
    }
}