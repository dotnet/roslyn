// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface ILspReferencesResultCreationService : IWorkspaceService
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

    [ExportWorkspaceService(typeof(ILspReferencesResultCreationService)), Shared]
    internal sealed class DefaultLspReferencesResultCreationService : ILspReferencesResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLspReferencesResultCreationService()
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
