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

[ExportCSharpVisualBasicStatelessLspService(typeof(DeactivateExtensionHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DeactivateExtensionHandler()
    : AbstractExtensionHandler, ILspServiceNotificationHandler<DeactivateExtensionParams>
{
    private const string MethodName = "server/_vs_deactivateExtension";

    public async Task HandleNotificationAsync(DeactivateExtensionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var service = context.Solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
        await service.UnregisterExtensionAsync(request.AssemblyFilePath, cancellationToken).ConfigureAwait(false);
    }
}
