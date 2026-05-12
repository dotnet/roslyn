// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.GoToDefinition;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor.GoToDefinition;

[Export(typeof(IDefinitionService)), Shared]
[method: ImportingConstructor]
internal sealed class DefinitionService(
    IRazorComponentSearchEngine componentSearchEngine,
    IDocumentMappingService documentMappingService,
    ITagHelperSearchEngine tagHelperSearchEngine,
    ILoggerFactory loggerFactory)
    : AbstractDefinitionService(componentSearchEngine, tagHelperSearchEngine, documentMappingService, loggerFactory.GetOrCreateLogger<DefinitionService>())
{
}
