// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ShellControls = Microsoft.VisualStudio.Shell.Controls;
using ShellStyles = Microsoft.VisualStudio.Shell.Styles;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests.InheritanceMargin;

[Trait(Traits.Feature, Traits.Features.InheritanceMargin)]
[UseExportProvider]
public sealed class InheritanceMarginGlyphTests
{
    [WpfTheory, InlineData(false), InlineData(true)]
    public async Task ClickingTargetNavigatesToDeclaration(bool nested)
    {
        using var workspace = CreateWorkspace(nested);
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var glyph = host.Glyph;
        var menu = (InheritanceMarginContextMenu)glyph.FindName("InheritanceMenu");
        var button = (ShellControls.DropDownButton)glyph.FindName("HoverButton");
        var popup = (ShellControls.Popup)glyph.FindName("HoverPopup");

        try
        {
            glyph.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseEnterEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            button.ShowDropDown();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            var menuSurface = (ShellControls.FlyoutSurface)VisualTreeHelper.GetChild(menu, 0);
            Assert.Equal(ShellStyles.StrokeWidths.Thin, menuSurface.BorderThickness);

            ItemsControl targetMenu = menu;
            if (nested)
            {
                var member = menu.Items.OfType<MemberMenuItemViewModel>().Single(item => item.DisplayContent.Contains("M()"));
                var memberContainer = (MenuItem)menu.ItemContainerGenerator.ContainerFromItem(member);
                memberContainer.IsSubmenuOpen = true;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                targetMenu = memberContainer;
            }

            var target = targetMenu.Items.OfType<TargetMenuItemViewModel>().Single();
            var targetContainer = (MenuItem)targetMenu.ItemContainerGenerator.ContainerFromItem(target);
            var targetClicks = 0;
            var menuClicks = 0;
            targetContainer.Click += (_, _) => targetClicks++;
            menu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler((_, _) => menuClicks++), handledEventsToo: true);
            var peer = new MenuItemAutomationPeer(targetContainer);
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();

            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await workspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.InheritanceMargin).ExpeditedWaitAsync();

            var navigation = (MockDocumentNavigationService)workspace.Services.GetRequiredService<IDocumentNavigationService>();
            Assert.True(navigation._triedNavigationToSpan || navigation._triedNavigationToPosition,
                $"Target clicks: {targetClicks}; menu clicks: {menuClicks}; target data: {targetContainer.DataContext}; menu open: {menu.IsOpen}.");
            Assert.Equal(workspace.Documents.Single().Id, navigation._documentId);
            if (navigation._triedNavigationToSpan)
                Assert.Equal(workspace.Documents.Single().SelectedSpans.Single(), navigation._span);
            else
                Assert.Equal(workspace.Documents.Single().SelectedSpans.Single().Start, navigation._position);
            Assert.False(menu.IsOpen);
        }
        finally
        {
            menu.IsOpen = false;
            popup.IsOpen = false;
        }
    }

    [WpfFact]
    public async Task HoverSurfaceHasStandardControlSize()
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var popup = (ShellControls.Popup)host.Glyph.FindName("HoverPopup");
        var surface = (ShellControls.FlyoutSurface)popup.Child;
        surface.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        surface.Arrange(new Rect(surface.DesiredSize));

        Assert.Equal(ShellStyles.Sizes.ControlMinWidth, surface.ActualWidth - surface.Padding.Left - surface.Padding.Right);
        Assert.Equal(ShellStyles.Sizes.ControlMinHeight, surface.ActualHeight - surface.Padding.Top - surface.Padding.Bottom);

        var button = (ShellControls.DropDownButton)host.Glyph.FindName("HoverButton");
        var buttonBounds = button.TransformToAncestor(surface).TransformBounds(new Rect(button.RenderSize));
        Assert.Equal(new Rect(surface.Padding.Left, surface.Padding.Top, ShellStyles.Sizes.ControlMinWidth, ShellStyles.Sizes.ControlMinHeight), buttonBounds);
    }

    [WpfFact]
    public async Task MenuContainersAreDeferredUntilOpened()
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var menu = (InheritanceMarginContextMenu)host.Glyph.FindName("InheritanceMenu");

        Assert.Null(menu.ItemContainerGenerator.ContainerFromIndex(0));
    }

    [WpfFact]
    public async Task OutsideClickDismissesMenu()
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var glyph = host.Glyph;
        var menu = (InheritanceMarginContextMenu)glyph.FindName("InheritanceMenu");
        var button = (ShellControls.DropDownButton)glyph.FindName("HoverButton");
        var popup = (ShellControls.Popup)glyph.FindName("HoverPopup");

        try
        {
            glyph.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseEnterEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            button.ShowDropDown();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(menu.IsOpen);

            menu.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, timestamp: 0, MouseButton.Left)
            {
                RoutedEvent = Mouse.PreviewMouseDownOutsideCapturedElementEvent,
                Source = button,
            });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            Assert.False(menu.IsOpen);
        }
        finally
        {
            menu.IsOpen = false;
            popup.IsOpen = false;
        }
    }

    [WpfFact]
    public async Task LeavingFlyoutDismissesHover()
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var glyph = host.Glyph;
        var popup = (ShellControls.Popup)glyph.FindName("HoverPopup");

        try
        {
            glyph.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseEnterEvent });
            Assert.True(popup.IsOpen);

            popup.Child.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseLeaveEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            Assert.False(popup.IsOpen);
        }
        finally
        {
            popup.IsOpen = false;
        }
    }

    [WpfFact]
    public async Task LeavingFlyoutKeepsOpenMenu()
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var glyph = host.Glyph;
        var popup = (ShellControls.Popup)glyph.FindName("HoverPopup");
        var menu = (InheritanceMarginContextMenu)glyph.FindName("InheritanceMenu");
        var button = (ShellControls.DropDownButton)glyph.FindName("HoverButton");

        try
        {
            glyph.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseEnterEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            button.ShowDropDown();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            popup.Child.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseLeaveEvent });
            await workspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.InheritanceMargin).ExpeditedWaitAsync();

            Assert.True(menu.IsOpen);
            Assert.True(popup.IsOpen);
        }
        finally
        {
            menu.IsOpen = false;
            popup.IsOpen = false;
        }
    }

    private static EditorTestWorkspace CreateWorkspace(bool nested = false)
    {
        var markup = nested
            ? "interface I { void [|M|](); void N(); }\r\nclass C : I { public void M() { } public void N() { } }"
            : "interface [|I|] { }\r\nclass C : I { }";
        return EditorTestWorkspace.CreateCSharp(markup, composition: GoToTestHelpers.Composition);
    }

    private static async Task<InheritanceMarginGlyph> CreateGlyphAsync(EditorTestWorkspace workspace)
    {
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.Single().Id);
        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var service = document.GetRequiredLanguageService<IInheritanceMarginService>();
        var items = await service.GetInheritanceMemberItemsAsync(
            document, root.FullSpan, includeGlobalImports: true, frozenPartialSemantics: true, CancellationToken.None);
        var tag = new InheritanceMarginTag(1, items.Where(item => item.LineNumber == 1).ToImmutableArray());
        var exportProvider = workspace.ExportProvider;
        var classificationFormatMap = exportProvider.GetExportedValue<IClassificationFormatMapService>().GetClassificationFormatMap("tooltip");

        return new InheritanceMarginGlyph(
            workspace,
            exportProvider.GetExportedValue<IThreadingContext>(),
            new MockStreamingFindUsagesPresenter(() => { }),
            exportProvider.GetExportedValue<ClassificationTypeMap>(),
            classificationFormatMap,
            exportProvider.GetExportedValue<IUIThreadOperationExecutor>(),
            tag,
            workspace.Documents.Single().GetTextView(),
            exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.InheritanceMargin));
    }

    private sealed class GlyphHost : IDisposable
    {
        public InheritanceMarginGlyph Glyph { get; }
        private readonly Window _window;

        public GlyphHost(InheritanceMarginGlyph glyph)
        {
            Glyph = glyph;
            _window = new Window
            {
                Content = glyph,
                Width = 16,
                Height = 16,
                Left = -10000,
                Top = -10000,
                WindowStyle = WindowStyle.None,
                ShowActivated = false,
                ShowInTaskbar = false,
            };
            _window.Show();
        }

        public void Dispose()
            => _window.Close();
    }
}
