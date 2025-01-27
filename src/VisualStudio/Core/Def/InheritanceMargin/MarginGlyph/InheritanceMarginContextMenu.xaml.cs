// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

/// <summary>
/// Interaction logic for InheritanceMarginContextMenu.xaml
/// </summary>
internal partial class InheritanceMarginContextMenu : ContextMenu
{
    private readonly IThreadingContext _threadingContext;
    private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
    private readonly IUIThreadOperationExecutor _operationExecutor;
    private readonly Workspace _workspace;
    private readonly IAsynchronousOperationListener _listener;

    public InheritanceMarginContextMenu(
        IThreadingContext threadingContext,
        IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
        IUIThreadOperationExecutor operationExecutor,
        Workspace workspace,
        IAsynchronousOperationListener listener,
        double scaleFactor)
    {
        _threadingContext = threadingContext;
        _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
        _workspace = workspace;
        _operationExecutor = operationExecutor;
        _listener = listener;
        InitializeComponent();
        LayoutTransform = new ScaleTransform(scaleFactor, scaleFactor);
        LayoutTransform.Freeze();
    }

    private void TargetMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MenuItem { DataContext: TargetMenuItemViewModel viewModel })
        {
            InheritanceMarginLogger.LogNavigateToTarget();

            var token = _listener.BeginAsyncOperation(nameof(TargetMenuItem_OnClick));
            TargetMenuItem_OnClickAsync(viewModel).CompletesAsyncOperation(token);
        }
    }

    private async Task TargetMenuItem_OnClickAsync(TargetMenuItemViewModel viewModel)
    {
        using var context = _operationExecutor.BeginExecute(
            title: EditorFeaturesResources.Navigating,
            defaultDescription: string.Format(ServicesVSResources.Navigate_to_0, viewModel.DisplayContent),
            allowCancellation: true,
            showProgress: false);

        var cancellationToken = context.UserCancellationToken;
        var rehydrated = await viewModel.DefinitionItem.TryRehydrateAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
        if (rehydrated == null)
            return;

        await _streamingFindUsagesPresenter.TryPresentLocationOrNavigateIfOneAsync(
            _threadingContext,
            _workspace,
            string.Format(CultureInfo.InvariantCulture, EditorFeaturesResources._0_declarations, viewModel.DisplayContent),
            [rehydrated],
            cancellationToken).ConfigureAwait(false);
    }

    private void TargetsSubmenu_OnOpen(object sender, RoutedEventArgs e)
    {
        InheritanceMarginLogger.LogInheritanceTargetsMenuOpen();
    }
}
