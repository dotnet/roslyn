// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class SourceDefinitionTreeItem : AbstractSourceTreeItem
    {
        private readonly string _symbolDisplay;

        public SourceDefinitionTreeItem(Document document, TextSpan sourceSpan, ISymbol symbol, ushort glyphIndex)
            : base(document, sourceSpan, glyphIndex)
        {
            _symbolDisplay = symbol.ToDisplayString(definitionDisplayFormat);

            this.DisplayText = $"[{document.Project.Name}] {_symbolDisplay}";
        }

        internal override void SetReferenceCount(int referenceCount)
        {
            var referenceCountDisplay = referenceCount == 1
                ? ServicesVSResources.ReferenceCountSingular
                : string.Format(ServicesVSResources.ReferenceCountPlural, referenceCount);

            this.DisplayText = $"[{_projectName}] {_symbolDisplay} ({referenceCountDisplay})";
        }
    }
}
