// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

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
        IEnumerable<ICustomAttribute> GetAttributes(EmitContext context); // TODO: consider moving this to IDefinition, we shouldn't need to examine attributes on references.

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
        IDefinition? AsDefinition(EmitContext context);
    }
}
