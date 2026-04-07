// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Provide document difference service specific to remote workspace's behavior.
/// 
/// Default <see cref="AbstractDocumentDifferenceService"/> is optimized for typing case in editor where we have events
/// for each typing. But in remote workspace, we aggregate changes and update solution in bulk and we don't have concept
/// of active file making default implementation unsuitable. Functionally, default one is still correct, but it often
/// time makes us to do more than we need. Basically, it always says this project has semantic change which can cause a
/// lot of re-analysis.
/// </summary>
internal class RemoteDocumentDifferenceService : IDocumentDifferenceService
{
    [ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.CSharp, layer: ServiceLayer.Host), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CSharpDocumentDifferenceService() : RemoteDocumentDifferenceService;

    [ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.VisualBasic, layer: ServiceLayer.Host), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class VisualBasicDocumentDifferenceService() : RemoteDocumentDifferenceService;

    public async Task<SyntaxNode?> GetChangedMemberAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
    {
        return null;
    }
}
