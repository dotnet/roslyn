// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using LSP = Roslyn.LanguageServer.Protocol;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Xaml;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;
#endif

/// <summary>
/// Represents a service to be exported via MEF for language server initialization.
/// </summary>
internal interface IOnInitializedService
{
    /// <summary>
    /// Called when the language server is being initialized.
    /// </summary>
    Task OnInitializedAsync(IClientRequestManager clientRequestManager, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken);
}
