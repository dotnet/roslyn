// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers;
using Microsoft.CodeAnalysis.FindUsages;
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
    [WpfTheory]
    [InlineData(InheritanceRelationship.BaseType, "Show base types for Example")]
    [InlineData(InheritanceRelationship.DerivedType, "Show derived types for Example")]
    [InlineData(InheritanceRelationship.ImplementedInterface, "Show implemented interfaces for Example")]
    [InlineData(InheritanceRelationship.InheritedInterface, "Show base interfaces for Example")]
    [InlineData(InheritanceRelationship.ImplementingType, "Show implementations for Example")]
    [InlineData(InheritanceRelationship.ImplementingMember, "Show implementations for Example")]
    [InlineData(InheritanceRelationship.ImplementedMember, "Show implemented members for Example")]
    [InlineData(InheritanceRelationship.OverriddenMember, "Show base members for Example")]
    [InlineData(InheritanceRelationship.OverridingMember, "Show overrides for Example")]
    [InlineData(InheritanceRelationship.BaseType | InheritanceRelationship.ImplementedInterface, "Show base types and implemented interfaces for Example")]
    [InlineData(InheritanceRelationship.BaseType | InheritanceRelationship.DerivedType, "Show base and derived types for Example")]
    [InlineData(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverriddenMember, "Show implemented and overridden members for Example")]
    [InlineData(InheritanceRelationship.OverriddenMember | InheritanceRelationship.OverridingMember, "Show base members and overrides for Example")]
    [InlineData(InheritanceRelationship.BaseType | InheritanceRelationship.ImplementedInterface | InheritanceRelationship.DerivedType, "Show inheritance for Example")]
    [InlineData(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverriddenMember | InheritanceRelationship.OverridingMember, "Show inheritance for Example")]
    [InlineData(InheritanceRelationship.InheritedInterface | InheritanceRelationship.ImplementingType, "Show inheritance for Example")]
    [InlineData(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverridingMember, "Show inheritance for Example")]
    public void ToolTipDescribesDistinctTargetKinds(object value, string expected)
    {
        var relationships = Assert.IsType<InheritanceRelationship>(value);
        using var workspace = CreateWorkspace();
        var definition = DefinitionItem.CreateNonNavigableItem(tags: [], displayParts: []).Detach();
        var targets = Enum.GetValues(typeof(InheritanceRelationship)).Cast<InheritanceRelationship>()
            .Where(relationship => relationship != InheritanceRelationship.None && relationships.HasFlag(relationship))
            .Select(relationship => new InheritanceTargetItem(
                relationship, definition, Glyph.ClassPublic, Glyph.CSharpFile, "Target", projectName: null))
            .ToImmutableArray();
        var member = new InheritanceMarginItem(
            lineNumber: 0,
            topLevelDisplayText: null,
            displayTexts: [new TaggedText(TextTags.Class, "Example")],
            glyph: Glyph.ClassPublic,
            targetItems: [.. targets, .. targets]);
        var provider = workspace.ExportProvider;
        var model = InheritanceMarginGlyphViewModel.Create(
            provider.GetExportedValue<ClassificationTypeMap>(),
            provider.GetExportedValue<IClassificationFormatMapService>().GetClassificationFormatMap("tooltip"),
            new InheritanceMarginTag(0, [member]),
            zoomLevel: 100);
        var text = new TextRange(model.ToolTipTextBlock.ContentStart, model.ToolTipTextBlock.ContentEnd).Text
            .Replace("\u200e", "").TrimEnd('\r', '\n');

        Assert.Equal(expected, text);
        Assert.Equal(expected, model.AutomationName);
    }

    [WpfTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToolTipDescribesInheritanceNavigation(bool multiple)
    {
        using var workspace = CreateWorkspace(nested: multiple);
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var glyph = host.Glyph;
        var model = (InheritanceMarginGlyphViewModel)glyph.DataContext;
        var text = new TextRange(model.ToolTipTextBlock.ContentStart, model.ToolTipTextBlock.ContentEnd).Text
            .Replace("\u200e", "").TrimEnd('\r', '\n');

        Assert.Equal(multiple
            ? "Show inheritance for members on this line"
            : "Show implemented interfaces for class C", text);
        Assert.Equal(multiple ? "Show inheritance for members on line 1" : text, model.AutomationName);
        Assert.Equal(model.AutomationName, AutomationProperties.GetName(glyph));
        Assert.Equal(model.AutomationName, AutomationProperties.GetName((DependencyObject)glyph.FindName("HoverButton")));
    }

    [WpfTheory]
    [InlineData(1.0, false)]
    [InlineData(1.0, true)]
    [InlineData(1.25, false)]
    [InlineData(1.25, true)]
    [InlineData(1.75, false)]
    [InlineData(1.75, true)]
    [InlineData(3.8, false)]
    [InlineData(3.8, true)]
    public async Task ToolTipMatchesEditorZoom(double zoom, bool hover)
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace, zoomLevel: zoom * 100), zoom);
        var glyph = host.Glyph;
        var popup = (ShellControls.Popup)glyph.FindName("HoverPopup");
        var button = (ShellControls.DropDownButton)glyph.FindName("HoverButton");
        if (hover)
        {
            glyph.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseEnterEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        }

        var target = hover ? (FrameworkElement)button : glyph;
        var toolTip = Assert.IsType<ShellControls.ToolTip>(target.ToolTip);
        toolTip.PlacementTarget = target;
        toolTip.Placement = PlacementMode.Bottom;
        try
        {
            toolTip.IsOpen = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var content = Assert.IsType<TextBlock>(toolTip.Content);
            Assert.True(content.ActualWidth > 0);
            Assert.True(content.ActualHeight > 0);
            var dpi = VisualTreeHelper.GetDpi(content);
            var origin = content.PointToScreen(default);
            var extent = content.PointToScreen(new Point(100, 100));
            Assert.InRange(extent.X - origin.X, 100 * zoom * dpi.DpiScaleX - 1, 100 * zoom * dpi.DpiScaleX + 1);
            Assert.InRange(extent.Y - origin.Y, 100 * zoom * dpi.DpiScaleY - 1, 100 * zoom * dpi.DpiScaleY + 1);
        }
        finally
        {
            toolTip.IsOpen = false;
            popup.IsOpen = false;
        }
    }

    [WpfTheory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task ImportTargetsHaveNoAdditionalIndentation(bool isImport, bool disambiguated)
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var relationship = isImport ? InheritanceRelationship.InheritedImport : InheritanceRelationship.DerivedType;
        var definition = DefinitionItem.CreateNonNavigableItem(tags: [], displayParts: []).Detach();
        var target = new InheritanceTargetItem(
            relationship, definition, isImport ? Glyph.None : Glyph.ClassPublic,
            Glyph.CSharpFile, isImport ? "System" : "Derived", projectName: null);
        var otherTarget = new InheritanceTargetItem(
            relationship, definition, target.Glyph, Glyph.BasicFile, target.DisplayName, projectName: null);
        var item = new InheritanceMarginItem(
            lineNumber: 0,
            topLevelDisplayText: isImport ? "Global imports" : null,
            displayTexts: [new TaggedText(TextTags.Text, "Source")],
            glyph: isImport ? Glyph.Namespace : Glyph.ClassPublic,
            targetItems: disambiguated ? [target, otherTarget] : [target]);
        var provider = workspace.ExportProvider;
        var model = InheritanceMarginGlyphViewModel.Create(
            provider.GetExportedValue<ClassificationTypeMap>(),
            provider.GetExportedValue<IClassificationFormatMapService>().GetClassificationFormatMap("tooltip"),
            new InheritanceMarginTag(0, [item]),
            zoomLevel: 100);
        if (isImport)
        {
            var tooltipText = new TextRange(model.ToolTipTextBlock.ContentStart, model.ToolTipTextBlock.ContentEnd).Text
                .Replace("\u200e", "").TrimEnd('\r', '\n');
            Assert.Equal("Global imports", tooltipText);
            Assert.Equal("Global imports", model.AutomationName);
        }
        var menu = new InheritanceMarginContextMenu { DataContext = model, PlacementTarget = host.Glyph };
        try
        {
            menu.IsOpen = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var targets = menu.Items.OfType<TargetMenuItemViewModel>().ToArray();
            Assert.Equal(disambiguated ? 2 : 1, targets.Length);
            foreach (var targetModel in targets)
            {
                Assert.Equal(disambiguated, targetModel is DisambiguousTargetMenuItemViewModel);
                var container = (MenuItem)menu.ItemContainerGenerator.ContainerFromItem(targetModel);
                Assert.Equal(isImport ? new Thickness(0) : ShellStyles.Spacings.LeftL, container.Margin);
            }
        }
        finally
        {
            menu.IsOpen = false;
        }
    }

    [WpfTheory]
    [InlineData(0.75, 0.0, 0.0, "Light")]
    [InlineData(1.0, 0.0, 0.0, "Light")]
    [InlineData(1.25, 0.0, 0.0, "Light")]
    [InlineData(1.33, 0.0, 0.0, "Light")]
    [InlineData(1.75, 0.0, 0.0, "Light")]
    [InlineData(2.13, 0.0, 0.0, "Light")]
    [InlineData(3.8, 0.0, 0.0, "Light")]
    [InlineData(4.0, 0.0, 0.0, "Light")]
    [InlineData(3.8, 0.3, 0.4, "Light")]
    [InlineData(3.8, 0.7, 0.1, "Light")]
    [InlineData(1.0, 0.0, 0.0, "Dark")]
    [InlineData(3.8, 0.0, 0.0, "Dark")]
    [InlineData(1.0, 0.0, 0.0, "HighContrast")]
    [InlineData(3.8, 0.0, 0.0, "HighContrast")]
    public async Task HoverMatchesMarginLightbulb(double zoom, double left, double top, string theme)
    {
        using var workspace = CreateWorkspace();
        var glyph = await CreateGlyphAsync(workspace, zoomLevel: zoom * 100);
        SetThemeResources(glyph, theme);
        using var host = new GlyphHost(glyph, zoom, new Point(left, top), onScreen: true);
        var popup = (ShellControls.Popup)glyph.FindName("HoverPopup");
        var button = (ShellControls.DropDownButton)glyph.FindName("HoverButton");
        var image = (FrameworkElement)glyph.FindName("GlyphImage");
        SetThemeResources(button, theme);
        SetThemeResources((FrameworkElement)popup.Child, theme);
        var source = new DrawingImage(new GeometryDrawing(Brushes.Black, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        ((Image)image).Source = source;
        image.Visibility = Visibility.Visible;
        ((Image)button.Content).Source = source;
        ((FrameworkElement)button.Content).Visibility = Visibility.Visible;
        host.UpdateLayout();

        try
        {
            glyph.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseEnterEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var hoverImage = (FrameworkElement)button.Content;
            var lightbulb = new ShellControls.Button
            {
                Padding = new Thickness(0),
                CornerRadius = ShellStyles.CornerRadii.L,
                Content = new Border { Width = ShellStyles.Sizes.IconS, Height = ShellStyles.Sizes.IconS, Background = Brushes.Black },
            };
            var lightbulbHost = new Border
            {
                Width = ShellStyles.Sizes.ControlMinWidth,
                Height = ShellStyles.Sizes.ControlMinHeight,
                Child = lightbulb,
            };
            var lightbulbPopup = new ShellControls.Popup
            {
                FocusOnOpen = false,
                PlacementTarget = glyph,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Center,
                VerticalOffset = 5,
                Content = lightbulbHost,
            };
            lightbulbPopup.Child.SetValue(Control.BackgroundProperty, Brushes.Transparent);
            lightbulbPopup.Child.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            SetThemeResources(lightbulb, theme);
            SetThemeResources((FrameworkElement)lightbulbPopup.Child, theme);
            ((Panel)glyph.Content).Children.Add(lightbulbPopup);
            lightbulbPopup.IsOpen = true;
            try
            {
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.Equal(lightbulb.CornerRadius, button.CornerRadius);
                Assert.IsType<SolidColorBrush>(button.Background);
                Assert.IsType<SolidColorBrush>(button.BorderBrush);
                Assert.IsType<SolidColorBrush>(lightbulb.Background);
                Assert.IsType<SolidColorBrush>(lightbulb.BorderBrush);
                Assert.Equal(ScreenBounds(lightbulb).Size, ScreenBounds(button).Size);
                Assert.Equal(ScreenBounds(image), ScreenBounds(hoverImage));
                Assert.All(RenderPixels(glyph), value => Assert.Equal(0, value));
                Assert.NotNull(glyph.InputHitTest(new Point(glyph.ActualWidth / 2, glyph.ActualHeight / 2)));
                var actualPixels = RenderPixels((FrameworkElement)popup.Child);
                var expectedPixels = RenderPixels((FrameworkElement)lightbulbPopup.Child);
                Assert.True(expectedPixels.SequenceEqual(actualPixels), "The hover frame must render identically to the margin lightbulb, including translucent edges and corners.");
            }
            finally
            {
                lightbulbPopup.IsOpen = false;
                ((Panel)glyph.Content).Children.Remove(lightbulbPopup);
            }
        }
        finally
        {
            popup.IsOpen = false;
        }

        Assert.Contains(RenderPixels(glyph), value => value != 0);
    }

    private static Rect ScreenBounds(UIElement element)
    {
        var topLeft = element.PointToScreen(default);
        var bottomRight = element.PointToScreen(new Point(element.RenderSize.Width, element.RenderSize.Height));
        return new Rect(topLeft, bottomRight);
    }

    private static void SetThemeResources(FrameworkElement element, string theme)
    {
        element.Resources[ShellStyles.ShellColors.SurfaceBackgroundFillDefaultBrushKey] = Brushes.White;
        element.Resources[ShellStyles.ShellColors.SolidBackgroundFillTertiaryBrushKey] = Brushes.White;
        element.Resources[ShellStyles.ShellColors.SurfaceStrokeFlyoutBrushKey] = Brushes.Gray;
        element.Resources[ShellStyles.ShellColors.ShadowFlyoutColorKey] = Color.FromArgb(128, 0, 0, 0);
        element.Resources[ShellStyles.ShellColors.ControlFillDefaultBrushKey] = theme switch
        {
            "Light" => new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
            "Dark" => new SolidColorBrush(Color.FromArgb(160, 32, 32, 32)),
            "HighContrast" => Brushes.Black,
            _ => throw new ArgumentOutOfRangeException(nameof(theme)),
        };
        element.Resources[ShellStyles.ShellColors.ControlFillSecondaryBrushKey] = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
        element.Resources[ShellStyles.ShellColors.ControlFillTertiaryBrushKey] = new SolidColorBrush(Color.FromArgb(96, 255, 255, 255));
        element.Resources[ShellStyles.ShellColors.ControlStrokeDefaultBrushKey] = theme == "HighContrast"
            ? Brushes.White
            : new SolidColorBrush(Color.FromArgb(32, 0, 0, 0));
        element.Resources[ShellStyles.ShellColors.ControlStrokeSecondaryBrushKey] = new SolidColorBrush(Color.FromArgb(48, 0, 0, 0));
        element.Resources[ShellStyles.ShellColors.TextFillPrimaryBrushKey] = Brushes.Black;
    }

    private static byte[] RenderPixels(FrameworkElement element)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, offset: 0);
        return pixels;
    }

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
    public async Task LeavingButtonDismissesHoverImmediately()
    {
        using var workspace = CreateWorkspace();
        using var host = new GlyphHost(await CreateGlyphAsync(workspace));
        var glyph = host.Glyph;
        var popup = (ShellControls.Popup)glyph.FindName("HoverPopup");
        var button = (ShellControls.DropDownButton)glyph.FindName("HoverButton");
        try
        {
            glyph.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseEnterEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(popup.IsOpen);
            button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, timestamp: 0) { RoutedEvent = Mouse.MouseLeaveEvent });
            Assert.False(popup.IsOpen);
        }
        finally
        {
            popup.IsOpen = false;
            await workspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.InheritanceMargin).ExpeditedWaitAsync();
        }
    }

    [WpfFact]
    public async Task LeavingFlyoutDismissesHoverImmediately()
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

            Assert.False(popup.IsOpen);
        }
        finally
        {
            popup.IsOpen = false;
            await workspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.InheritanceMargin).ExpeditedWaitAsync();
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

    private static async Task<InheritanceMarginGlyph> CreateGlyphAsync(EditorTestWorkspace workspace, double zoomLevel = 100)
    {
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.Single().Id);
        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var service = document.GetRequiredLanguageService<IInheritanceMarginService>();
        var items = await service.GetInheritanceMemberItemsAsync(
            document, root.FullSpan, includeGlobalImports: true, frozenPartialSemantics: true, CancellationToken.None);
        var tag = new InheritanceMarginTag(1, items.Where(item => item.LineNumber == 1).ToImmutableArray());
        var exportProvider = workspace.ExportProvider;
        var classificationFormatMap = exportProvider.GetExportedValue<IClassificationFormatMapService>().GetClassificationFormatMap("tooltip");
        workspace.Documents.Single().GetTextView().ZoomLevel = zoomLevel;

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

        public GlyphHost(InheritanceMarginGlyph glyph, double zoom = 1.0, Point offset = default, bool onScreen = false)
        {
            Glyph = glyph;
            glyph.Width = 16;
            glyph.Height = 16;
            var content = new Canvas
            {
                Width = 16 + offset.X,
                Height = 16 + offset.Y,
                LayoutTransform = new ScaleTransform(zoom, zoom),
            };
            content.Children.Add(glyph);
            Canvas.SetLeft(glyph, offset.X);
            Canvas.SetTop(glyph, offset.Y);
            _window = new Window
            {
                Content = content,
                Width = (16 + offset.X) * zoom,
                Height = (16 + offset.Y) * zoom,
                Left = onScreen ? 400 : -10000,
                Top = onScreen ? 300 : -10000,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowActivated = false,
                ShowInTaskbar = false,
            };
            _window.Show();
        }

        public void Dispose()
            => _window.Close();

        public void UpdateLayout()
            => _window.UpdateLayout();

    }
}
