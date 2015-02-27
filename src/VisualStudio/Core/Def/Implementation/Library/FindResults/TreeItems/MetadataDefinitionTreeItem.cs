// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class MetadataDefinitionTreeItem : AbstractTreeItem, ITreeItemWithReferenceCount
    {
        private readonly string _assemblyName;
        private readonly string _symbolDefinition;

        public override bool CanGoToSource
        {
            get
            {
                return false;
            }
        }

        public override bool UseGrayText
        {
            get
            {
                return true;
            }
        }

        public MetadataDefinitionTreeItem(ISymbol definition, ushort glyphIndex)
            : base(glyphIndex)
        {
            _assemblyName = definition.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            _symbolDefinition = definition.ToDisplayString(definitionDisplayFormat);
            this.DisplayText = $"[{_assemblyName}] {_symbolDefinition}";
        }       

        public override int GoToSource()
        {
            return VSConstants.E_NOTIMPL;
        }

        public void SetReferenceCount(int referenceCount)
        {
            if (referenceCount > 0)
            {
                var referenceCountDisplay = referenceCount == 1
                    ? string.Format(ServicesVSResources.ReferenceCountSingular, referenceCount)
                    : string.Format(ServicesVSResources.ReferenceCountPlural, referenceCount);

                this.DisplayText = $"[{_assemblyName}] {_symbolDefinition} ({referenceCountDisplay})";
            }
        }
    }
}
