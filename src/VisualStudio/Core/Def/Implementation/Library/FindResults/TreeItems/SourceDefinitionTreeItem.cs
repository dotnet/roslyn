// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class SourceDefinitionTreeItem : AbstractSourceTreeItem
    {
        private readonly bool _canGoToDefinition;
        private readonly string _symbolDisplay;

        public SourceDefinitionTreeItem(Document document, TextSpan sourceSpan, ISymbol symbol, ushort glyphIndex)
            : base(document, sourceSpan, glyphIndex)
        {
            _symbolDisplay = symbol.ToDisplayString(definitionDisplayFormat);
            this.DisplayText = $"{GetProjectNameString()}{_symbolDisplay}";

            _canGoToDefinition = symbol.Kind != SymbolKind.Namespace;
        }

        public override bool CanGoToDefinition()
        {
            return _canGoToDefinition;
        }

        internal override void SetReferenceCount(int referenceCount)
        {
            var referenceCountDisplay = referenceCount == 1
                ? ServicesVSResources.ReferenceCountSingular
                : string.Format(ServicesVSResources.ReferenceCountPlural, referenceCount);

            this.DisplayText = $"{GetProjectNameString()}{_symbolDisplay} ({referenceCountDisplay})";
        }

        private string GetProjectNameString()
        {
            return (_projectName != null && _canGoToDefinition) ? $"[{_projectName}] " : string.Empty;
        }
    }
}
