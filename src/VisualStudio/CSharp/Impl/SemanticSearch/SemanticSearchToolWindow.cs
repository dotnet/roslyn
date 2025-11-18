// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.Extensibility.VSSdkCompatibility;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using Dock = Microsoft.VisualStudio.Extensibility.ToolWindows.Dock;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

[VisualStudioContribution]
internal sealed class SemanticSearchToolWindow(MefInjection<SemanticSearchToolWindowImpl> impl) : ToolWindow
{
    /// <summary>
    /// HACK: Id of the tool window needed for finding tool window frame. This is created by Gladstone by hashing the full type names of
    /// <see cref="SemanticSearchToolWindow"/> and <see cref="CSharpExtension"/> types.
    /// </summary>
    internal static readonly Guid Id = new("91ef2fc9-e39d-1962-9b55-7047b01b40f7");

    // Initialized by InitializeAsync
    private SemanticSearchToolWindowImpl _impl = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _impl?.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);

        Title = string.Format(ServicesVSResources.Semantic_search_0, LanguageNames.CSharp);

        _impl = await impl.GetServiceAsync().ConfigureAwait(false);
    }

    public override ToolWindowConfiguration ToolWindowConfiguration => new()
    {
        Placement = ToolWindowPlacement.Floating,
        DockDirection = Dock.Bottom,
        AllowAutoCreation = true,
    };

    public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        => _impl.InitializeAsync(cancellationToken);
}
