// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindReferences
{
    /// <summary>
    /// A collection of <see cref="DefinitionItem"/>s and <see cref="SourceReferenceItem"/>s
    /// that can be presented in an editor and used to navigate to the defintions and
    /// references found for a symbol.
    /// </summary>
    internal struct DefinitionsAndReferences
    {
        public static readonly DefinitionsAndReferences Empty =
            new DefinitionsAndReferences(ImmutableArray<DefinitionItem>.Empty, ImmutableArray<SourceReferenceItem>.Empty);

        /// <summary>
        /// All the definitions to show.  Note: not all definitions may have references.
        /// </summary>
        public ImmutableArray<DefinitionItem> Definitions { get; }

        /// <summary>
        /// All the references to show.  Note: every <see cref="SourceReferenceItem.Definition"/> 
        /// should be in <see cref="Definitions"/> 
        /// </summary>
        public ImmutableArray<SourceReferenceItem> References { get; }

        public DefinitionsAndReferences(
            ImmutableArray<DefinitionItem> definitions,
            ImmutableArray<SourceReferenceItem> references)
        {
            Definitions = definitions;
            References = references;
        }
    }
}