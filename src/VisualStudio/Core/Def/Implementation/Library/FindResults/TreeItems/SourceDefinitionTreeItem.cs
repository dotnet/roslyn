// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class DefinitionTreeItem : AbstractTreeItem
    {
        private readonly DefinitionItem _definitionItem;
        private readonly DefinitionLocation _definitionLocation;

        public DefinitionTreeItem(DefinitionItem definitionItem, DefinitionLocation definitionLocation)
            : base(definitionItem.Tags.GetGlyph().GetGlyphIndex())
        {
            _definitionItem = definitionItem;
            _definitionLocation = definitionLocation;

            this.DisplayText = CreateDisplayText();
        }

        private string CreateDisplayText()
        {
            var displayString = _definitionItem.DisplayParts.JoinText();

            return _definitionLocation.OriginationParts.Length == 0
                ? displayString
                : $"[{_definitionLocation.OriginationParts.JoinText()}] {displayString}";
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

        internal override void SetReferenceCount(int referenceCount)
        {
            // source case.
            var referenceCountDisplay = referenceCount == 1
                ? ServicesVSResources._1_reference
                : string.Format(ServicesVSResources._0_references, referenceCount);

            this.DisplayText = CreateDisplayText() + $" ({referenceCount})";
        }
    }
}