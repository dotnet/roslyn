// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class DefinitionTreeItem : AbstractTreeItem
    {
        private readonly DefinitionItem _definitionItem;
        private readonly DefinitionLocation _definitionLocation;

        public DefinitionTreeItem(
            DefinitionItem definitionItem,
            DefinitionLocation definitionLocation,
            ImmutableArray<SourceReferenceTreeItem> referenceItems)
            : base(definitionItem.Tags.GetGlyph().GetGlyphIndex())
        {
            _definitionItem = definitionItem;
            _definitionLocation = definitionLocation;

            this.Children.AddRange(referenceItems);
            this.DisplayText = CreateDisplayText();
        }

        private string CreateDisplayText()
        {
            var displayString = _definitionItem.DisplayParts.JoinText();
            var referenceCount = this.Children.Count;

            var referenceCountDisplay = referenceCount == 1
                ? ServicesVSResources._1_reference
                : string.Format(ServicesVSResources._0_references, referenceCount);

            // If we don't have an origination or reference count, then just display the 
            // parts and nothing else.  These items happen when we're getting third party
            // results that tell us about their definition location, but not any additional
            // reference.  We don't want to say '0' references in that case as that can
            // be misleading.
            var hasOrigination = _definitionLocation.OriginationParts.Length > 0;
            return hasOrigination
                ? $"[{_definitionLocation.OriginationParts.JoinText()}] {displayString} ({referenceCountDisplay})"
                : referenceCount > 0
                    ? $"{displayString} ({referenceCountDisplay})"
                    : displayString;
        }

        public override int GoToSource()
        {
            return _definitionLocation.TryNavigateTo()
                ? VSConstants.S_OK
                : VSConstants.E_FAIL;
        }

        public override bool CanGoToDefinition()
        {
            return _definitionLocation.CanNavigateTo();
        }
    }
}