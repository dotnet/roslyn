// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.VSSdkCompatibility;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;

/// <summary>
/// Implements View/Other Windows/C# Interactive command.
/// </summary>
[VisualStudioContribution]
internal class OpenInteractiveWindowCommand(
    MefInjection<IThreadingContext> mefThreadingContext,
    MefInjection<CSharpVsInteractiveWindowProvider> mefInteractiveWindowProvider) : Command
{
    public override CommandConfiguration CommandConfiguration => new("%CSharpLanguageServiceExtension.OpenInteractiveWindow.DisplayName%")
    {
        Placements = new[] { CommandPlacement.KnownPlacements.ViewOtherWindowsMenu.WithPriority(0x8000) },
        // TODO: Shortcuts https://github.com/dotnet/roslyn/issues/3941
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var threadingContext = await mefThreadingContext.GetServiceAsync().ConfigureAwait(false);
        var interactiveWindowProvider = await mefInteractiveWindowProvider.GetServiceAsync().ConfigureAwait(false);

        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _ = interactiveWindowProvider.Open(instanceId: 0, focus: true);
    }
}
