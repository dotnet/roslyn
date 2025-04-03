// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

[ExportCSharpVisualBasicStatelessLspService(typeof(ExtensionRegisterHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ExtensionRegisterHandler()
    : ILspServiceRequestHandler<ExtensionRegisterParams, ExtensionRegisterResponse>
{
    private const string MethodName = "roslyn/extensionRegister";

    /// <summary>
    /// Report that we mutate solution state so that we only attempt to register or unregister one extension at a time.
    /// This ensures we don't have to handle any threading concerns while this is happening.  As this should be a rare
    /// operation, this simplifies things while ideally being low cost.
    /// </summary>
    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => true;

    public async Task<ExtensionRegisterResponse> HandleRequestAsync(ExtensionRegisterParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
        var response = await service.RegisterExtensionAsync(request.AssemblyFilePath, cancellationToken).ConfigureAwait(false);

        return new(response.WorkspaceMessageHandlers, response.DocumentMessageHandlers);
    }
}
