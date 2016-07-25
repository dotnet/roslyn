// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FindReferences
{
    /// <summary>
    /// Information about a symbol's reference that can be used for diplay and 
    /// navigation in an editor.
    /// </summary>
    internal sealed class SourceReferenceItem
    {
        /// <summary>
        /// The definition this reference corresponds to.
        /// </summary>
        public DefinitionItem Definition { get; }

        /// <summary>
        /// The location of the source item.
        /// </summary>
        public DocumentLocation Location { get; }

        public SourceReferenceItem(DefinitionItem definition, DocumentLocation location)
        {
            Definition = definition;
            Location = location;
        }
    }
}