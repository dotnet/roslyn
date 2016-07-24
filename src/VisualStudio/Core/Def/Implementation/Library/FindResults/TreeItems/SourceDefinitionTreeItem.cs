// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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

#if false // source base constructor
                        // We store the document ID, line and offset for navigation so that we
            // still provide reasonable navigation if the user makes changes elsewhere
            // in the document other than inserting or removing lines.

            _workspace = document.Project.Solution.Workspace;
            _documentId = document.Id;
            _projectName = document.Project.Name;
            _filePath = GetFilePath(document, commonPathElements);
            _sourceSpan = sourceSpan;

            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var textLine = text.Lines.GetLineFromPosition(_sourceSpan.Start);
            _textLineString = textLine.ToString();

            _lineNumber = textLine.LineNumber;
            _offset = sourceSpan.Start - textLine.Start;

            var spanInSecondaryBuffer = text.GetVsTextSpanForLineOffset(_lineNumber, _offset);

            VsTextSpan spanInPrimaryBuffer;
            var succeeded = spanInSecondaryBuffer.TryMapSpanFromSecondaryBufferToPrimaryBuffer(_workspace, _documentId, out spanInPrimaryBuffer);

            _mappedLineNumber = succeeded ? spanInPrimaryBuffer.iStartLine : _lineNumber;
            _mappedOffset = succeeded ? spanInPrimaryBuffer.iStartIndex : _offset;

#endif

            this.DisplayText = CreateDisplayText();

#if false // source contructor
            _canGoToDefinition = symbol.Kind != SymbolKind.Namespace;

#endif

#if false // metadata constructor
                        _workspace = workspace;
            _referencingProjectId = referencingProjectId;
            _symbolKey = definition.GetSymbolKey();
            _symbolDefinition = definition.ToDisplayString(FindReferencesUtilities.DefinitionDisplayFormat);
            _canGoToDefinition = definition.Kind != SymbolKind.Namespace;
#endif
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