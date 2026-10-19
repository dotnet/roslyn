// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class CodeStyleSettingsProviderTests
{
    private static readonly CancellationToken s_token = CancellationToken.None;

    [Fact]
    public void EditorConfigFileFilterIncludesTheWildcardPattern()
        => Assert.Equal("All files (*.*)|*.*", Microsoft.VisualStudio.LanguageServices.ServicesVSResources.All_files_filter);

    [Fact]
    public void CategoryCommandsDispatchTheExistingActionsAndStopAfterShutdown()
    {
        using var shutdown = new CancellationTokenSource();
        var commandSet = Guid.NewGuid();
        var generated = 0;
        var helped = 0;
        var commands = CodeStyleSettingsCommands.CreateCommands(commandSet, () => generated++, () => helped++, shutdown.Token);
        Assert.Equal(new[] { CodeStyleSettingsCommands.GenerateEditorConfigCommandId, CodeStyleSettingsCommands.HelpCommandId }, commands.Select(command => command.CommandID.ID));
        Assert.All(commands, command => Assert.Equal(commandSet, command.CommandID.Guid));
        commands[0].Invoke();
        commands[1].Invoke();
        Assert.Equal(1, generated);
        Assert.Equal(1, helped);
        shutdown.Cancel();
        commands[0].Invoke();
        commands[1].Invoke();
        Assert.Equal(1, generated);
        Assert.Equal(1, helped);
    }

    [Fact]
    public void PackageCommandRegistrationRemovesBothHandlersOnDisposal()
    {
        var added = new List<MenuCommand>();
        var removed = new List<MenuCommand>();
        var service = new Mock<IMenuCommandService>(MockBehavior.Strict);
        service.Setup(service => service.AddCommand(It.IsAny<MenuCommand>())).Callback<MenuCommand>(added.Add);
        service.Setup(service => service.RemoveCommand(It.IsAny<MenuCommand>())).Callback<MenuCommand>(removed.Add);
        var registration = CodeStyleSettingsCommands.Register(service.Object, Guid.NewGuid(), () => { }, () => { }, s_token);
        Assert.Equal(2, added.Count);
        Assert.Empty(removed);
        registration.Dispose();
        Assert.Equal(added, removed);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task GenerateCategoryCommandReadsPendingChangesAtInvocationWithoutCommitting(string language)
    {
        var globalOptions = new TestGlobalOptions();
        var setting = StyleSetting(language);
        globalOptions.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(globalOptions, language, [setting]);
        string? generated = null;
        var commands = CodeStyleSettingsCommands.CreateCommands(Guid.NewGuid(),
            () => generated = CodeStyleSettingsCommands.GenerateText([("Code style", ImmutableArray.Create(setting.Key.Option))], provider.CreatePreviewStore(), language),
            () => { }, s_token);
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        await provider.SetValueAsync(setting.Moniker + "_severity", "error", s_token);
        commands[0].Invoke();
        Assert.NotNull(generated);
        Assert.Contains(setting.Moniker + " = true:error", generated);
        Assert.False(globalOptions.GetOption<CodeStyleOption2<bool>>(setting.Key).Value);
        Assert.Equal(0, globalOptions.PersistCount);
    }

    [WpfFact]
    public void NativePreviewContainsOnlyTheEditorHostAndNoActionButtons()
    {
        var preview = new CodeStyleSettingsPreview();
        Assert.Equal(150, preview.Height);
        Assert.Equal(VerticalAlignment.Top, preview.VerticalAlignment);
        var host = Assert.IsType<ScrollViewer>(preview.Content);
        Assert.Equal(ScrollBarVisibility.Auto, host.VerticalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Disabled, host.HorizontalScrollBarVisibility);
        Assert.Equal(VerticalAlignment.Top, host.VerticalContentAlignment);
        Assert.Null(host.Content);
    }

    [WpfFact]
    [UseExportProvider]
    public void NativePreviewUsesAnOuterScrollbarWithoutChangingGlobalEditorOptions()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("class C { }");
        var view = workspace.Documents.Single().GetTextView();
        var globalOptions = view.Options.GlobalOptions;
        var horizontal = globalOptions.GetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId);
        var vertical = globalOptions.GetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId);

        CodeStyleSettingsPreview.ConfigureEditor(view);

        Assert.False(view.Options.GetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId));
        Assert.False(view.Options.GetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId));
        Assert.Equal(VerticalAlignment.Top, view.VisualElement.VerticalAlignment);
        Assert.Equal(horizontal, globalOptions.GetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId));
        Assert.Equal(vertical, globalOptions.GetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId));
    }

    [WpfTheory]
    [InlineData(1, Visibility.Collapsed)]
    [InlineData(30, Visibility.Visible)]
    [UseExportProvider]
    public void NativePreviewShowsVerticalScrollbarOnlyWhenCodeExceedsTheViewport(int lineCount, Visibility expectedVisibility)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(string.Join(Environment.NewLine, Enumerable.Repeat("class C { }", lineCount)));
        var view = workspace.Documents.Single().GetTextView();
        var editor = workspace.ExportProvider.GetExportedValue<ITextEditorFactoryService>().CreateTextViewHost(view, setFocus: false);
        try
        {
            CodeStyleSettingsPreview.ConfigureEditor(view);
            var preview = new CodeStyleSettingsPreview();
            var scrollViewer = Assert.IsType<ScrollViewer>(preview.Content);
            scrollViewer.Content = editor.HostControl;
            Layout(150);

            Assert.Equal(expectedVisibility, scrollViewer.ComputedVerticalScrollBarVisibility);
            Assert.Equal(Visibility.Collapsed, scrollViewer.ComputedHorizontalScrollBarVisibility);
            if (expectedVisibility == Visibility.Visible)
            {
                Assert.True(scrollViewer.ScrollableHeight > 0);
                scrollViewer.ScrollToBottom();
                preview.UpdateLayout();
                Assert.True(scrollViewer.VerticalOffset > 0);
            }

            Layout(800);
            Assert.Equal(expectedVisibility, scrollViewer.ComputedVerticalScrollBarVisibility);

            Layout(double.PositiveInfinity);
            Assert.Equal(expectedVisibility, scrollViewer.ComputedVerticalScrollBarVisibility);

            view.TextBuffer.Replace(new Span(0, view.TextSnapshot.Length), "class C { }");
            CodeStyleSettingsPreview.ConfigureEditor(view);
            Layout(double.PositiveInfinity);
            Assert.Equal(Visibility.Collapsed, scrollViewer.ComputedVerticalScrollBarVisibility);
            Assert.Equal(0, scrollViewer.ScrollableHeight);
            Assert.Equal(0, scrollViewer.VerticalOffset);

            void Layout(double height)
            {
                preview.Measure(new Size(400, height));
                Assert.Equal(150, preview.DesiredSize.Height);
                preview.Arrange(new Rect(new Size(400, preview.DesiredSize.Height)));
                preview.UpdateLayout();
                Assert.Equal(150, preview.ActualHeight);
                Assert.Equal(400, preview.ActualWidth);
            }
        }
        finally
        {
            editor.Close();
        }
    }

    [WpfTheory]
    [InlineData(149, Visibility.Collapsed)]
    [InlineData(150, Visibility.Collapsed)]
    [InlineData(151, Visibility.Visible)]
    public void NativePreviewVerticalScrollbarReflectsTheFixedHeightBoundary(double contentHeight, Visibility expectedVisibility)
    {
        var preview = new CodeStyleSettingsPreview();
        var scrollViewer = Assert.IsType<ScrollViewer>(preview.Content);
        scrollViewer.Content = new Border { Height = contentHeight };
        preview.Measure(new Size(400, double.PositiveInfinity));
        preview.Arrange(new Rect(new Size(400, preview.DesiredSize.Height)));
        preview.UpdateLayout();

        Assert.Equal(150, preview.ActualHeight);
        Assert.Equal(150, scrollViewer.ViewportHeight);
        Assert.Equal(contentHeight, scrollViewer.ExtentHeight);
        Assert.Equal(expectedVisibility, scrollViewer.ComputedVerticalScrollBarVisibility);
        Assert.Equal(Math.Max(0, contentHeight - 150), scrollViewer.ScrollableHeight);
    }

    [WpfTheory]
    [InlineData("Display();")]
    [InlineData("Display();\n\nDisplay();")]
    [InlineData("Display();\n")]
    [UseExportProvider]
    public void PreviewProjectionPreservesSampleLinesWithoutAddingATerminalLine(string code)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("class C { }");
        var componentModel = new MockComponentModel(workspace.ExportProvider);
        componentModel.ProvideService(workspace.ExportProvider);
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(provider => provider.GetService(typeof(SComponentModel))).Returns(componentModel);
        using var model = new Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting.StyleViewModel(new OptionStore(new TestGlobalOptions()), serviceProvider.Object);
        model.UpdatePreview($$"""
            class C
            {
                void M()
                {
            //[
            {{code}}
            //]
                }
            }
            """);

        var editor = model.TextViewHost;
        var view = editor.TextView;
        var snapshot = Assert.IsAssignableFrom<IProjectionSnapshot>(view.TextSnapshot);
        Assert.Equal(code, snapshot.GetText().ReplaceLineEndings("\n"));
        var source = snapshot.GetSourceSpans()[0].Snapshot;
        var marker = source.GetLineFromPosition(source.GetText().IndexOf("//]", StringComparison.Ordinal));
        Assert.True(source.GetLineFromLineNumber(marker.LineNumber - 1).LineBreakLength > 0);

        CodeStyleSettingsPreview.ConfigureEditor(view);
        var preview = new CodeStyleSettingsPreview();
        var scrollViewer = Assert.IsType<ScrollViewer>(preview.Content);
        scrollViewer.Content = editor.HostControl;
        Layout();
        Assert.Equal(150, preview.ActualHeight);
        Assert.Equal(0, view.VisualElement.TranslatePoint(new Point(), scrollViewer).Y);

        preview.Height = view.VisualElement.Height;
        Layout();
        Assert.Equal(Visibility.Collapsed, scrollViewer.ComputedVerticalScrollBarVisibility);
        Assert.Equal(0, scrollViewer.ScrollableHeight);

        void Layout()
        {
            preview.Measure(new Size(400, double.PositiveInfinity));
            preview.Arrange(new Rect(new Size(400, preview.DesiredSize.Height)));
            preview.UpdateLayout();
        }
    }

    [WpfFact]
    public void NativePreviewMouseWheelAtItsBoundaryScrollsTheContainingPage()
    {
        var preview = new CodeStyleSettingsPreview { Height = 100 };
        var previewScrollViewer = Assert.IsType<ScrollViewer>(preview.Content);
        var previewCode = new Border { Height = 300 };
        previewScrollViewer.Content = previewCode;
        var panel = new StackPanel();
        panel.Children.Add(new Border { Height = 100 });
        panel.Children.Add(preview);
        panel.Children.Add(new Border { Height = 300 });
        var pageScrollViewer = new ScrollViewer { Content = panel };
        var size = new Size(400, 200);
        pageScrollViewer.Measure(size);
        pageScrollViewer.Arrange(new Rect(size));
        pageScrollViewer.UpdateLayout();
        previewScrollViewer.ScrollToBottom();
        previewScrollViewer.UpdateLayout();
        pageScrollViewer.ScrollToTop();
        Assert.Equal(previewScrollViewer.ScrollableHeight, previewScrollViewer.VerticalOffset);
        Assert.True(pageScrollViewer.ScrollableHeight > 0);

        var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120)
        {
            RoutedEvent = Mouse.PreviewMouseWheelEvent,
        };
        previewCode.RaiseEvent(wheel);
        wheel.RoutedEvent = Mouse.MouseWheelEvent;
        previewCode.RaiseEvent(wheel);
        pageScrollViewer.UpdateLayout();

        Assert.Equal(previewScrollViewer.ScrollableHeight, previewScrollViewer.VerticalOffset);
        Assert.True(pageScrollViewer.VerticalOffset > 0);
    }

    [WpfTheory]
    [InlineData(3)]
    [InlineData(30)]
    [UseExportProvider]
    public void NativePreviewMouseWheelCannotScrollTheEditorPastTheCode(int lineCount)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(string.Join(Environment.NewLine, Enumerable.Repeat("class C { }", lineCount)));
        var view = workspace.Documents.Single().GetTextView();
        var editor = workspace.ExportProvider.GetExportedValue<ITextEditorFactoryService>().CreateTextViewHost(view, setFocus: false);
        try
        {
            CodeStyleSettingsPreview.ConfigureEditor(view);
            var preview = new CodeStyleSettingsPreview();
            var scrollViewer = Assert.IsType<ScrollViewer>(preview.Content);
            scrollViewer.Content = editor.HostControl;
            var size = new Size(400, 200);
            preview.Measure(size);
            preview.Arrange(new Rect(size));
            preview.UpdateLayout();
            view.DisplayTextLineContainingBufferPosition(view.TextSnapshot.GetLineFromLineNumber(0).Start, 0, ViewRelativePosition.Top, 400, view.VisualElement.Height);

            ScrollWithWheel(-120);
            Assert.Equal(scrollViewer.ScrollableHeight, scrollViewer.VerticalOffset);
            Assert.Equal(0, view.ViewportTop);

            ScrollWithWheel(120);
            Assert.Equal(0, scrollViewer.VerticalOffset);
            Assert.Equal(0, view.ViewportTop);

            void ScrollWithWheel(int delta)
            {
                for (var i = 0; i < 50; i++)
                {
                    var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, delta)
                    {
                        RoutedEvent = Mouse.PreviewMouseWheelEvent,
                    };
                    view.VisualElement.RaiseEvent(wheel);
                    wheel.RoutedEvent = Mouse.MouseWheelEvent;
                    view.VisualElement.RaiseEvent(wheel);
                    preview.UpdateLayout();
                }
            }
        }
        finally
        {
            editor.Close();
        }
    }

    [Fact]
    public void MetadataChoicesFollowTheLegacySubsetAndOrderRatherThanNumericEnumOrder()
    {
        Assert.Equal<object>(
            [AddImportPlacement.InsideNamespace, AddImportPlacement.OutsideNamespace],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, LanguageNames.CSharp, []));
        Assert.Equal<object>(
            [ParenthesesPreference.AlwaysForClarity, ParenthesesPreference.NeverIfUnnecessary],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CodeStyleOptions2.ArithmeticBinaryParentheses, LanguageNames.CSharp, []));
        Assert.Equal<object>(
            [UnusedValuePreference.UnusedLocalVariable, UnusedValuePreference.DiscardVariable],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CSharpCodeStyleOptions.UnusedValueAssignment, LanguageNames.CSharp, []));
        Assert.Equal<object>(
            [UnusedValuePreference.UnusedLocalVariable],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CSharpCodeStyleOptions.UnusedValueAssignment, LanguageNames.VisualBasic, []));
        Assert.Equal<object>(
            [UnusedParametersPreference.NonPublicMethods, UnusedParametersPreference.AllMethods],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CodeStyleOptions2.UnusedParameters, LanguageNames.CSharp, []));
        Assert.Equal<object>(
            [NamespaceDeclarationPreference.BlockScoped, NamespaceDeclarationPreference.FileScoped],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CSharpCodeStyleOptions.NamespaceDeclarations, LanguageNames.CSharp, []));
        Assert.Equal<object>(
            [ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, LanguageNames.CSharp, []));
        Assert.Equal(
            ["Always", "None", "WhenMultiline"],
            CodeStyleSettingsProvider.GetStylePreferenceValues(CSharpCodeStyleOptions.PreferBraces, LanguageNames.CSharp, []).Select(value => value.ToString()));
    }

    [Fact]
    public void SeverityEditsPreserveUsingPlacementValuesNotOfferedByTheLegacyDropdown()
    {
        var setting = new CodeStyleSetting("csharp_using_directive_placement", CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, LanguageNames.CSharp, "Using placement");
        var current = new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.OutsideNamespaceIgnoringAliases, NotificationOption2.Silent);
        var edited = Assert.IsType<CodeStyleOption2<AddImportPlacement>>(setting.Update(current, "warning", severity: true));
        Assert.Equal(AddImportPlacement.OutsideNamespaceIgnoringAliases, edited.Value);
        Assert.Equal(NotificationOption2.Warning, edited.Notification);
    }

    [Theory]
    [InlineData("silent")]
    [InlineData("suggestion")]
    [InlineData("warning")]
    [InlineData("error")]
    public void PreferenceEditsPreserveNotificationAndRoundTripLegacyXml(string severity)
    {
        var setting = StyleSetting(LanguageNames.CSharp);
        var notification = CodeStyleSetting.ParseSeverity(severity).WithIsExplicitlySpecified(true);
        var current = new CodeStyleOption2<bool>(false, notification);
        var edited = Assert.IsType<CodeStyleOption2<bool>>(setting.Update(current, "true"));
        Assert.True(edited.Value);
        Assert.Equal(notification, edited.Notification);
        var legacy = CodeStyleOption2<bool>.FromXElement(edited.ToXElement());
        Assert.True(legacy.Value);
        Assert.Equal(notification.Severity, legacy.Notification.Severity);
        var fromLegacy = Assert.IsType<CodeStyleOption2<bool>>(setting.Update(legacy, "false"));
        Assert.False(fromLegacy.Value);
        Assert.Equal(notification.Severity, fromLegacy.Notification.Severity);
    }

    [Theory]
    [InlineData("silent")]
    [InlineData("suggestion")]
    [InlineData("warning")]
    [InlineData("error")]
    public void SeverityEditsPreserveEveryEnumPreferenceAndRoundTripLegacyXml(string severity)
    {
        var setting = new CodeStyleSetting("expressionBody", CSharpCodeStyleOptions.PreferExpressionBodiedMethods, LanguageNames.CSharp, "Expression body");
        foreach (var preference in Enum.GetValues(typeof(ExpressionBodyPreference)).Cast<ExpressionBodyPreference>())
        {
            var current = new CodeStyleOption2<ExpressionBodyPreference>(preference, NotificationOption2.Silent);
            var edited = Assert.IsType<CodeStyleOption2<ExpressionBodyPreference>>(setting.Update(current, severity, severity: true));
            Assert.Equal(preference, edited.Value);
            Assert.Equal(CodeStyleSetting.ParseSeverity(severity), edited.Notification);
            Assert.Equal(edited, CodeStyleOption2<ExpressionBodyPreference>.FromXElement(edited.ToXElement()));
        }
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task MissingValuesUseTheActualCompoundDefaultWithoutPersisting(string language)
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(language);
        using var provider = new CodeStyleSettingsProvider(options, language, [setting]);
        var expected = (ICodeStyleOption2)setting.Key.Option.DefaultValue!;
        Assert.Equal(setting.SerializePreference(expected), Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.Equal(CodeStyleSetting.SerializeSeverity(expected.Notification), Value(await provider.GetValueAsync<string>(setting.Moniker + "_severity", s_token)));
        Assert.Empty(options.Values);
        Assert.Equal(0, options.PersistCount);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task LegacyProfileImportAndGlobalResetRefreshBothCompoundComponentsWithoutPersisting(string language)
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(language);
        using var provider = new CodeStyleSettingsProvider(options, language, [setting]);
        var notifications = 0;
        provider.SettingValuesChanged += (_, _) => notifications++;
        var imported = new CodeStyleOption2<bool>(true, NotificationOption2.Error);

        options.RefreshOption(setting.Key, imported);
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.Equal("error", Value(await provider.GetValueAsync<string>(setting.Moniker + "_severity", s_token)));
        Assert.Equal(imported, provider.CreatePreviewStore().GetOption<CodeStyleOption2<bool>>(setting.Key.Option, language));

        var expected = (ICodeStyleOption2)setting.Key.Option.DefaultValue!;
        options.RefreshOption(setting.Key, expected);
        Assert.Equal(setting.SerializePreference(expected), Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.Equal(CodeStyleSetting.SerializeSeverity(expected.Notification), Value(await provider.GetValueAsync<string>(setting.Moniker + "_severity", s_token)));
        Assert.Equal(expected, provider.CreatePreviewStore().GetOption<CodeStyleOption2<bool>>(setting.Key.Option, language));
        Assert.Equal(2, notifications);
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(0, options.PersistCount);
    }

    [Fact]
    public async Task LegacyGlobalResetRefreshesBothFormattingMasksWithoutPersisting()
    {
        var options = new TestGlobalOptions();
        var braces = new CodeStyleSetting("types", CSharpFormattingOptions2.NewLineBeforeOpenBrace, LanguageNames.CSharp, "Types", (int)NewLineBeforeOpenBracePlacement.Types);
        var parentheses = new CodeStyleSetting("expressions", CSharpFormattingOptions2.SpaceBetweenParentheses, LanguageNames.CSharp, "Expressions", (int)SpacePlacementWithinParentheses.Expressions);
        var defaultBraces = (NewLineBeforeOpenBracePlacement)braces.Key.Option.DefaultValue!;
        var defaultParentheses = (SpacePlacementWithinParentheses)parentheses.Key.Option.DefaultValue!;
        options.Incoming(braces.Key, (defaultBraces ^ NewLineBeforeOpenBracePlacement.Types) | (NewLineBeforeOpenBracePlacement)(1 << 24));
        options.Incoming(parentheses.Key, (defaultParentheses ^ SpacePlacementWithinParentheses.Expressions) | (SpacePlacementWithinParentheses)(1 << 24));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [braces, parentheses]);
        var notifications = 0;
        provider.SettingValuesChanged += (_, _) => notifications++;

        options.RefreshOption(braces.Key, defaultBraces);
        options.RefreshOption(parentheses.Key, defaultParentheses);
        Assert.Equal((defaultBraces & NewLineBeforeOpenBracePlacement.Types) != 0, Value(await provider.GetValueAsync<bool>(braces.Moniker, s_token)));
        Assert.Equal((defaultParentheses & SpacePlacementWithinParentheses.Expressions) != 0, Value(await provider.GetValueAsync<bool>(parentheses.Moniker, s_token)));
        var preview = provider.CreatePreviewStore();
        Assert.Equal(defaultBraces, preview.GetOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace));
        Assert.Equal(defaultParentheses, preview.GetOption(CSharpFormattingOptions2.SpaceBetweenParentheses));
        Assert.Equal(2, notifications);
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(0, options.PersistCount);
    }

    [Fact]
    public async Task PairedPendingEditsCommitOneCoherentLegacyOption()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.CSharp);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        await provider.SetValueAsync(setting.Moniker + "_severity", "error", s_token);
        Assert.False(options.GetOption<CodeStyleOption2<bool>>(setting.Key).Value);
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.Equal("error", Value(await provider.GetValueAsync<string>(setting.Moniker + "_severity", s_token)));
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Error), options.GetOption<CodeStyleOption2<bool>>(setting.Key));
        Assert.Equal(1, options.PersistCount);
    }

    [Fact]
    public async Task IncomingLegacySeverityIsRebasedWhenOnlyPreferenceWasEdited()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.CSharp);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        var notifications = 0;
        provider.SettingValuesChanged += (_, _) => notifications++;
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Warning));
        Assert.True(notifications > 0);
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Warning), options.GetOption<CodeStyleOption2<bool>>(setting.Key));
    }

    [Fact]
    public async Task IncomingLegacyPreferenceIsRebasedWhenOnlySeverityWasEdited()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.VisualBasic);
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.VisualBasic, [setting]);
        await provider.SetValueAsync(setting.Moniker + "_severity", "warning", s_token);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(true, NotificationOption2.Error));
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Warning), options.GetOption<CodeStyleOption2<bool>>(setting.Key));
    }

    [Fact]
    public async Task IndependentFlagEditsPreserveOtherFlagsAndUnknownBits()
    {
        var options = new TestGlobalOptions();
        var types = new CodeStyleSetting("types", CSharpFormattingOptions2.NewLineBeforeOpenBrace, LanguageNames.CSharp, "Types", (int)NewLineBeforeOpenBracePlacement.Types);
        var methods = new CodeStyleSetting("methods", CSharpFormattingOptions2.NewLineBeforeOpenBrace, LanguageNames.CSharp, "Methods", (int)NewLineBeforeOpenBracePlacement.Methods);
        var unknown = (NewLineBeforeOpenBracePlacement)(1 << 24);
        options.Incoming(types.Key, unknown | NewLineBeforeOpenBracePlacement.Types);
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [types, methods]);
        await provider.SetValueAsync("types", false, s_token);
        await provider.SetValueAsync("methods", true, s_token);
        options.Incoming(types.Key, unknown | NewLineBeforeOpenBracePlacement.Types | NewLineBeforeOpenBracePlacement.Properties);
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(unknown | NewLineBeforeOpenBracePlacement.Methods | NewLineBeforeOpenBracePlacement.Properties, options.GetOption<NewLineBeforeOpenBracePlacement>(types.Key));
        Assert.Equal(1, options.PersistCount);
    }

    [Fact]
    public async Task ParenthesisFlagEditsPreserveTheOtherBits()
    {
        var setting = new CodeStyleSetting("expressions", CSharpFormattingOptions2.SpaceBetweenParentheses, LanguageNames.CSharp, "Expressions", (int)SpacePlacementWithinParentheses.Expressions);
        var unknown = (SpacePlacementWithinParentheses)(1 << 24);
        var current = unknown | SpacePlacementWithinParentheses.Expressions | SpacePlacementWithinParentheses.ControlFlowStatements;
        Assert.Equal(unknown | SpacePlacementWithinParentheses.ControlFlowStatements, setting.Update(current, false));
        var options = new TestGlobalOptions();
        options.Incoming(setting.Key, unknown | SpacePlacementWithinParentheses.TypeCasts);
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        await provider.SetValueAsync("expressions", true, s_token);
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(unknown | SpacePlacementWithinParentheses.TypeCasts | SpacePlacementWithinParentheses.Expressions, options.GetOption<SpacePlacementWithinParentheses>(setting.Key));
    }

    [Theory]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.Types)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.Methods)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.Properties)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.Accessors)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.AnonymousMethods)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.ControlBlocks)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.AnonymousTypes)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers)]
    [InlineData(true, (int)NewLineBeforeOpenBracePlacement.LambdaExpressionBody)]
    [InlineData(false, (int)SpacePlacementWithinParentheses.Expressions)]
    [InlineData(false, (int)SpacePlacementWithinParentheses.TypeCasts)]
    [InlineData(false, (int)SpacePlacementWithinParentheses.ControlFlowStatements)]
    public async Task EveryFormattingPreviewFlagPreservesPositiveUnknownBitsAndIncomingLegacyChanges(bool braces, int flag)
    {
        IOption2 option = braces ? CSharpFormattingOptions2.NewLineBeforeOpenBrace : CSharpFormattingOptions2.SpaceBetweenParentheses;
        var allFlags = braces ? (int)NewLineBeforeOpenBracePlacement.All : (int)SpacePlacementWithinParentheses.All;
        var setting = new CodeStyleSetting("flag", option, LanguageNames.CSharp, "Flag", flag);
        var options = new TestGlobalOptions();
        var original = (1 << 20) | allFlags;
        options.Incoming(setting.Key, Enum.ToObject(option.Type, original));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        Assert.True(Value(await provider.GetValueAsync<bool>(setting.Moniker, s_token)));
        await provider.SetValueAsync(setting.Moniker, false, s_token);
        var incoming = original | (1 << 24);
        options.Incoming(setting.Key, Enum.ToObject(option.Type, incoming));
        Assert.False(Value(await provider.GetValueAsync<bool>(setting.Moniker, s_token)));
        Assert.Equal(incoming & ~flag, Convert.ToInt32(CodeStyleSettingsProvider.Read(provider.CreatePreviewStore(), setting.Key)));
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(incoming & ~flag, Convert.ToInt32(options.GetOption<object>(setting.Key)));
        Assert.Equal(1, options.PersistCount);
    }

    [Fact]
    public async Task RefreshDiscardsOnlyThatPagesPendingEditsAndResumePreservesThem()
    {
        var options = new TestGlobalOptions();
        var csharp = StyleSetting(LanguageNames.CSharp);
        var basic = StyleSetting(LanguageNames.VisualBasic);
        options.Incoming(csharp.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        options.Incoming(basic.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var csharpProvider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [csharp]);
        using var basicProvider = new CodeStyleSettingsProvider(options, LanguageNames.VisualBasic, [basic]);
        await csharpProvider.SetValueAsync(csharp.Moniker, "true", s_token);
        await basicProvider.SetValueAsync(basic.Moniker, "true", s_token);
        await csharpProvider.SuspendAsync(s_token);
        await csharpProvider.ResumeAsync(s_token);
        Assert.Equal("true", Value(await csharpProvider.GetValueAsync<string>(csharp.Moniker, s_token)));
        await basicProvider.RefreshCacheAsync(s_token);
        Assert.Equal("false", Value(await basicProvider.GetValueAsync<string>(basic.Moniker, s_token)));
        Assert.Equal("true", Value(await csharpProvider.GetValueAsync<string>(csharp.Moniker, s_token)));
        Assert.Equal(0, options.PersistCount);
    }

    [Fact]
    public async Task SeverityChoicesMatchLegacyVisibleValuesAndParseSuppressedSeverity()
    {
        var setting = StyleSetting(LanguageNames.CSharp);
        using var provider = new CodeStyleSettingsProvider(new TestGlobalOptions(), LanguageNames.CSharp, [setting]);
        var choices = Value(await provider.GetEnumChoicesAsync(setting.Moniker + "_severity", s_token));
        Assert.Equal(new[] { "silent", "suggestion", "warning", "error" }, choices.Select(choice => choice.Moniker));
        Assert.All(choices, choice => Assert.False(string.IsNullOrWhiteSpace(choice.Title)));
        Assert.Equal(NotificationOption2.None, CodeStyleSetting.ParseSeverity("none"));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "[*.cs]")]
    [InlineData(LanguageNames.VisualBasic, "[*.vb]")]
    public async Task EditorConfigGenerationUsesPendingCompoundEditsWithoutCommitting(string language, string section)
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(language);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, language, [setting]);
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        await provider.SetValueAsync(setting.Moniker + "_severity", "warning", s_token);
        var text = CodeStyleSettingsCommands.GenerateText([("Code style", ImmutableArray.Create(setting.Key.Option))], provider.CreatePreviewStore(), language);
        Assert.Contains(section, text);
        Assert.Contains(setting.Moniker + " = true:warning", text);
        Assert.False(options.GetOption<CodeStyleOption2<bool>>(setting.Key).Value);
        Assert.Equal(0, options.PersistCount);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task PivotCellOverlaysUpdatePreviewWithoutLeakingIntoTheBackingStore(string language)
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(language);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, language, [setting]);
        var preview = provider.CreatePreviewStore();
        var row = NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, setting.Moniker);
        Assert.Same(setting, provider.FindSetting(row + ".preference"));
        Assert.Same(setting, provider.FindSetting(row + ".severity"));
        provider.ApplyPreviewValue(preview, row + ".preference", "true");
        provider.ApplyPreviewValue(preview, row + ".severity", "error");
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Error), preview.GetOption<CodeStyleOption2<bool>>(setting.Key.Option, language));
        Assert.Equal("false", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(0, options.PersistCount);
        Assert.False(options.GetOption<CodeStyleOption2<bool>>(setting.Key).Value);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task PivotCellsShareCompoundPendingValuesAndEditorConfigWithoutCommitting(string language)
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(language);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, language, [setting]);
        var row = NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, setting.Moniker);
        await provider.SetValueAsync(row + ".preference", "true", s_token);
        await provider.SetValueAsync(row + ".severity", "error", s_token);
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.Equal("error", Value(await provider.GetValueAsync<string>(setting.Moniker + "_severity", s_token)));
        var text = CodeStyleSettingsCommands.GenerateText([("Code style", ImmutableArray.Create(setting.Key.Option))], provider.CreatePreviewStore(), language);
        Assert.Contains(setting.Moniker + " = true:error", text);
        Assert.Equal(0, options.PersistCount);
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Error), options.GetOption<CodeStyleOption2<bool>>(setting.Key));
        Assert.Equal(1, options.PersistCount);
    }

    [Fact]
    public async Task FullShellMonikersResolveAndShareOnePendingValueWithRelativeMonikers()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.CSharp);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        var prefix = "languages.csharp.editorConfig.codeStyle.options.";
        await provider.SetValueAsync(setting.Moniker, "false", s_token);
        await provider.SetValueAsync(prefix + setting.Moniker, "true", s_token);
        await provider.SetValueAsync(prefix + setting.Moniker + "_severity", "warning", s_token);
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(prefix + setting.Moniker, s_token)));
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.Contains(Value(await provider.GetEnumChoicesAsync(prefix + setting.Moniker, s_token)), choice => choice.Moniker == "true");
        Assert.Equal(4, Value(await provider.GetEnumChoicesAsync(prefix + setting.Moniker + "_severity", s_token)).Count);
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Warning), options.GetOption<CodeStyleOption2<bool>>(setting.Key));
        Assert.Equal(1, options.PersistCount);
    }

    [Fact]
    public void NormalJsonFormattingOverlaysDrivePreviewAndEditorConfigWithoutWritingLegacyValues()
    {
        var options = new TestGlobalOptions();
        var spacing = new CodeStyleSetting("csharpSpaceAfterComma", CSharpFormattingOptions2.SpaceAfterComma, LanguageNames.CSharp, "Comma");
        var binary = new CodeStyleSetting("binaryOperatorSpacing", CSharpFormattingOptions2.SpacingAroundBinaryOperator, LanguageNames.CSharp, "Operators");
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [spacing, binary]);
        var preview = provider.CreatePreviewStore();
        var prefix = "languages.csharp.editorConfig.spacing.";
        provider.ApplyPreviewValue(preview, prefix + spacing.Moniker, false);
        provider.ApplyPreviewValue(preview, prefix + binary.Moniker, "Remove");
        Assert.False(preview.GetOption(CSharpFormattingOptions2.SpaceAfterComma));
        Assert.Equal(BinaryOperatorSpacingOptionsInternal.Remove, preview.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator));
        var text = CodeStyleSettingsCommands.GenerateText([("Spacing", ImmutableArray.Create(spacing.Key.Option, binary.Key.Option))], preview, LanguageNames.CSharp);
        Assert.Contains("csharp_space_after_comma = false", text);
        Assert.Contains("csharp_space_around_binary_operators = none", text);
        Assert.Empty(options.Values);
        Assert.Equal(0, options.PersistCount);
        Assert.True(options.GetOption(CSharpFormattingOptions2.SpaceAfterComma));
    }

    [Fact]
    public async Task InvalidValuesDoNotReplacePendingCompoundEdits()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.CSharp);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        foreach (var moniker in new[] { setting.Moniker, setting.Moniker + "_severity" })
        {
            var failure = Assert.IsType<ExternalSettingOperationResult.Failure>(await provider.SetValueAsync(moniker, "invalid", s_token));
            Assert.Equal(ExternalSettingsErrorScope.SingleSettingOnly, failure.Scope);
        }
        await provider.CommitPendingChangesAsync(s_token);
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Silent), options.GetOption<CodeStyleOption2<bool>>(setting.Key));
    }

    [Fact]
    public async Task SuspensionAndDisposalReleaseSubscriptionsWithoutPersistingOrDiscardingPendingValues()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.CSharp);
        var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        var notifications = 0;
        provider.SettingValuesChanged += (_, _) => notifications++;
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        await provider.SuspendAsync(s_token);
        Assert.Equal(0, options.HandlerCount);
        var previous = notifications;
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Warning));
        Assert.Equal(previous, notifications);
        await provider.ResumeAsync(s_token);
        await provider.ResumeAsync(s_token);
        Assert.Equal(1, options.HandlerCount);
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.Equal("warning", Value(await provider.GetValueAsync<string>(setting.Moniker + "_severity", s_token)));
        provider.Dispose();
        provider.Dispose();
        Assert.Equal(0, options.HandlerCount);
        Assert.Equal(0, options.PersistCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.SetValueAsync(setting.Moniker, "false", s_token));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.GetValueAsync<string>(setting.Moniker, s_token));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.ResumeAsync(s_token));
    }

    [Fact]
    public async Task CanceledOperationsDoNotCommitClearPendingValuesOrDetachSubscriptions()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.CSharp);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        var canceled = new CancellationToken(canceled: true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.CommitPendingChangesAsync(canceled));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.RefreshCacheAsync(canceled));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SuspendAsync(canceled));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SetValueAsync(setting.Moniker, "false", canceled));
        Assert.Equal(1, options.HandlerCount);
        Assert.Equal(0, options.PersistCount);
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
    }

    [Fact]
    public async Task FailedPublicationRetainsThePendingValueForRetry()
    {
        var options = new TestGlobalOptions();
        var setting = StyleSetting(LanguageNames.CSharp);
        options.Incoming(setting.Key, new CodeStyleOption2<bool>(false, NotificationOption2.Silent));
        using var provider = new CodeStyleSettingsProvider(options, LanguageNames.CSharp, [setting]);
        await provider.SetValueAsync(setting.Moniker, "true", s_token);
        options.FailPublication = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CommitPendingChangesAsync(s_token));
        Assert.Equal("true", Value(await provider.GetValueAsync<string>(setting.Moniker, s_token)));
        Assert.False(options.GetOption<CodeStyleOption2<bool>>(setting.Key).Value);
        options.FailPublication = false;
        await provider.CommitPendingChangesAsync(s_token);
        Assert.True(options.GetOption<CodeStyleOption2<bool>>(setting.Key).Value);
    }

    [Fact]
    public void PreviewFactoryRejectsShutdownAndUnknownIdsWithoutCreatingUiOrAcquiringServices()
    {
        var id = Guid.Empty;
        var factory = new CodeStyleSettingsPreviewFactory(null!, _ => throw new InvalidOperationException(), () => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException(), maximumElementId: 4, new CancellationToken(canceled: true));
        Assert.Equal(Microsoft.VisualStudio.VSConstants.E_ABORT, factory.CreateUIElement(ref id, 0, out var element));
        Assert.Null(element);
        factory = new CodeStyleSettingsPreviewFactory(null!, _ => throw new InvalidOperationException(), () => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException(), maximumElementId: 4, s_token);
        Assert.Equal(Microsoft.VisualStudio.VSConstants.E_INVALIDARG, factory.CreateUIElement(ref id, 5, out element));
        Assert.Null(element);
    }

    private static CodeStyleSetting StyleSetting(string language)
        => new(CodeStyleOptions2.QualifyFieldAccess.Definition.ConfigName, CodeStyleOptions2.QualifyFieldAccess, language, "Qualify field access");

    private static T Value<T>(ExternalSettingOperationResult<T> result)
        => Assert.IsType<ExternalSettingOperationResult<T>.Success>(result).Value;

    private sealed class TestGlobalOptions : IGlobalOptionService
    {
        private readonly List<(object target, WeakEventHandler<OptionChangedEventArgs> handler)> _handlers = [];
        internal int HandlerCount => _handlers.Count;
        internal Dictionary<OptionKey2, object?> Values { get; } = [];
        internal int PersistCount { get; private set; }
        internal bool FailPublication { get; set; }

        public T GetOption<T>(OptionKey2 key) => (T)(Values.TryGetValue(key, out var value) ? value : key.Option.DefaultValue)!;
        public T GetOption<T>(Option2<T> option) => GetOption<T>(new OptionKey2(option));
        public T GetOption<T>(PerLanguageOption2<T> option, string language) => GetOption<T>(new OptionKey2(option, language));
        public ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey2> keys) => keys.Select(GetOption<object?>).ToImmutableArray();
        public bool TryGetOption<T>(OptionKey2 key, out T value)
        {
            value = GetOption<T>(key);
            return true;
        }
        public void SetGlobalOption<T>(Option2<T> option, T value) => SetGlobalOption(new OptionKey2(option), value);
        public void SetGlobalOption<T>(PerLanguageOption2<T> option, string language, T value) => SetGlobalOption(new OptionKey2(option, language), value);
        public void SetGlobalOption(OptionKey2 key, object? value) => SetGlobalOptions([new(key, value)]);
        public bool SetGlobalOptions(ImmutableArray<KeyValuePair<OptionKey2, object?>> options)
        {
            if (FailPublication)
                throw new InvalidOperationException();
            PersistCount += options.Length;
            foreach (var pair in options)
                Incoming(pair.Key, pair.Value);
            return !options.IsEmpty;
        }
        public bool RefreshOption(OptionKey2 key, object? value)
        {
            Incoming(key, value);
            return true;
        }
        internal void Incoming(OptionKey2 key, object? value)
        {
            Values[key] = value;
            foreach (var (target, handler) in _handlers.ToArray())
                handler(this, target, new OptionChangedEventArgs([(key, value)]));
        }
        public void AddOptionChangedHandler(object target, WeakEventHandler<OptionChangedEventArgs> handler) => _handlers.Add((target, handler));
        public void RemoveOptionChangedHandler(object target, WeakEventHandler<OptionChangedEventArgs> handler) => _handlers.Remove((target, handler));
    }
}
