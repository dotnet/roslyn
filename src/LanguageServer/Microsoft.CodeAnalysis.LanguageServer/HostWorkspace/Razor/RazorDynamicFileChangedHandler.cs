// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[ExportCSharpVisualBasicStatelessLspService(typeof(RazorDynamicFileChangedHandler))]
[Method("razor/dynamicFileInfoChanged")]
internal class RazorDynamicFileChangedHandler : ILspServiceNotificationHandler<RazorDynamicFileChangedParams>
{
    private readonly RazorDynamicFileInfoProvider _razorDynamicFileInfoProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorDynamicFileChangedHandler(RazorDynamicFileInfoProvider razorDynamicFileInfoProvider)
    {
        _razorDynamicFileInfoProvider = razorDynamicFileInfoProvider;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    public Task HandleNotificationAsync(RazorDynamicFileChangedParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var filePath = ProtocolConversions.GetDocumentFilePathFromUri(request.RazorDocument.Uri);
        _razorDynamicFileInfoProvider.Update(filePath);
        return Task.CompletedTask;
    }
}
