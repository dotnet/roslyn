// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    /// <summary>
    /// Represent the location of a symbol definition that can be presented in 
    /// an editor and used to navigate to the symbol's origin.
    /// 
    /// Standard implmentations can be obtained through <see cref="CreateDocumentLocation"/>,
    /// <see cref="CreateNonNavigatingLocation"/> and <see cref="CreateSymbolLocation"/>.
    /// 
    /// Subclassing is also supported for scenarios that fall outside the bounds of
    /// these common cases.
    /// </summary>
    internal abstract partial class DefinitionLocation
    {
        /// <summary>
        /// Where the location originally came from (for example, the containing assembly or
        /// project name).  May be used in the presentation of a definition.
        /// </summary>
        public abstract ImmutableArray<TaggedText> OriginationParts { get; }

        protected DefinitionLocation()
        {
        }

        public abstract bool CanNavigateTo();
        public abstract bool TryNavigateTo();

        public static DefinitionLocation CreateDocumentLocation(DocumentLocation location)
        {
            return new DocumentDefinitionLocation(location);
        }

        public static DefinitionLocation CreateSymbolLocation(ISymbol symbol, Project referencingProject)
        {
            return new SymbolDefinitionLocation(symbol, referencingProject);
        }

        public static DefinitionLocation CreateNonNavigatingLocation(
            ImmutableArray<TaggedText> originationParts)
        {
            return new NonNavigatingDefinitionLocation(originationParts);
        }

        internal static ImmutableArray<TaggedText> GetOriginationParts(ISymbol symbol)
        {
            var assemblyName = symbol.ContainingAssembly?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return string.IsNullOrWhiteSpace(assemblyName)
                ? ImmutableArray<TaggedText>.Empty
                : ImmutableArray.Create(new TaggedText(TextTags.Assembly, assemblyName));
        }
    }
}