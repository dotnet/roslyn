// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

/// <summary>
/// Implements View/Other Windows/Semantic Search command.
/// </summary>
[VisualStudioContribution]
internal sealed class OpenSemanticSearchWindowCommand : Command
{
    public override CommandConfiguration CommandConfiguration => new("%CSharpLanguageServiceExtension.OpenSemanticSearchWindow.DisplayName%")
    {
        Icon = new(ImageMoniker.KnownValues.FindSymbol, IconSettings.IconAndText),
        Placements = [CommandPlacement.KnownPlacements.ViewOtherWindowsMenu.WithPriority(0x8010)],
        VisibleWhen = ActivationConstraint.FeatureFlag("Roslyn.SemanticSearchEnabled")
    };

    public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        => Extensibility.Shell().ShowToolWindowAsync<SemanticSearchToolWindow>(activate: true, cancellationToken);
}
