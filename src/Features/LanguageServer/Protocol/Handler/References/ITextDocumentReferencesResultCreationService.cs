// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface ITextDocumentReferencesResultCreationService : IWorkspaceService
    {
        SumType<VSInternalReferenceItem, LSP.Location>? CreateReference(
            int definitionId,
            int id,
            ClassifiedTextElement text,
            DocumentSpan? documentSpan,
            ImmutableDictionary<string, string> properties,
            ClassifiedTextElement? definitionText,
            Glyph definitionGlyph,
            SymbolUsageInfo? symbolUsageInfo,
            LSP.Location? location);
    }

    [ExportWorkspaceService(typeof(ITextDocumentReferencesResultCreationService)), Shared]
    internal sealed class DefaultTextDocumentReferencesResultCreationService : ITextDocumentReferencesResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultTextDocumentReferencesResultCreationService()
        {
        }

        public SumType<VSInternalReferenceItem, LSP.Location>? CreateReference(
            int definitionId,
            int id,
            ClassifiedTextElement text,
            DocumentSpan? documentSpan,
            ImmutableDictionary<string, string> properties,
            ClassifiedTextElement? definitionText,
            Glyph definitionGlyph,
            SymbolUsageInfo? symbolUsageInfo,
            LSP.Location? location)
        {
            if (location is null)
                return null;

            return location;
        }
    }
}
