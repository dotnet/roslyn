// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnifiedSettings;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageId)]
[ProvideMenuResource("CodeStyleSettingsCommands.ctmenu", 1)]
[ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\csharpCodeStyleSettings.registration.json")]
internal sealed class CSharpCodeStyleSettingsPackage : AsyncPackage
{
    public const string PackageId = "47b0d77e-35c4-4b41-9fc4-9309ef73f93b";
    public const string PreviewFactoryId = "b147d67e-5ddf-4106-bd6e-c6cc49b88141";
    private readonly List<CodeStyleSettingsProvider> _providers = [];
    private IGlobalOptionService? _globalOptions;
    private IDisposable? _commands;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _globalOptions = this.GetMefService<IGlobalOptionService>();
        this.GetMefService<INativeCodeStyleSettingsService>().Start(LanguageNames.CSharp);
        var settings = CreateSettings(_globalOptions).ToArray();
        var categories = new[] { "", "indentation", "newLines", "spacing", "wrapping", "general" };
        for (var index = 0; index < categories.Length; index++)
        {
            var prefix = categories[index] + ".";
            var pageSettings = settings.Where(setting => index == 0 ? !setting.Moniker.Contains('.') : setting.Moniker.StartsWith(prefix, StringComparison.Ordinal))
                .Select(setting => new CodeStyleSetting(index == 0 ? setting.Moniker : setting.Moniker.Substring(prefix.Length), setting.Key.Option, LanguageNames.CSharp, setting.Description, setting.Flag) { Choices = setting.Choices });
            var provider = new CodeStyleSettingsProvider(_globalOptions, LanguageNames.CSharp, pageSettings, CreatePendingStore, this.GetMefService<IThreadingContext>());
            _providers.Add(provider);
        }
        var registrar = await GetServiceAsync(typeof(SVsUIFactory)).ConfigureAwait(true) as IVsRegisterUIFactories ?? throw new InvalidOperationException();
        var factoryId = new Guid(PreviewFactoryId);
        var factory = new CodeStyleSettingsPreviewFactory(this, id => _providers[(int)id], CreatePendingStore, CreatePreviewModel, maximumElementId: 4, DisposalToken);
        ErrorHandler.ThrowOnFailure(registrar.RegisterUIFactory(ref factoryId, factory));
        var commandService = await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true) as IMenuCommandService ?? throw new InvalidOperationException();
        _commands = CodeStyleSettingsCommands.Register(commandService, new Guid(PackageId),
            () => CodeStyleSettingsCommands.GenerateEditorConfig(this, factory.CreatePendingStore(), LanguageNames.CSharp),
            CodeStyleSettingsCommands.ShowHelp, DisposalToken);
    }

    private OptionStore CreatePendingStore()
    {
        var store = new OptionStore(_globalOptions!);
        foreach (var provider in _providers)
        {
            foreach (var change in provider.CreatePreviewStore().GetChangedOptions())
                store.SetOption(change.key.Option, change.key.Language, change.newValue);
        }
        return store;
    }

    internal IEnumerable<CodeStyleSetting> CreateSettings(IGlobalOptionService globalOptions)
    {
        var store = CodeStyleSettingsProvider.CreateMetadataStore(this, globalOptions, LanguageNames.CSharp);
        using var style = new StyleViewModel(store, this);
        var result = style.CodeStyleItems.Select(item => CodeStyleSettingsProvider.CreateStyleSetting(item, LanguageNames.CSharp)).ToList();
        AddFormatting(result, "indentation", new IndentationViewModel(store, this));
        AddFormatting(result, "newLines", new NewLinesViewModel(store, this));
        AddFormatting(result, "spacing", new SpacingViewModel(store, this));
        AddFormatting(result, "wrapping", new WrappingViewModel(store, this));
        result.Add(new("indentation.labelPositioning", CSharpFormattingOptions2.LabelPositioning, LanguageNames.CSharp, CSharpVSResources.Label_Indentation)
        {
            Choices = [new("LeftMost", CSharpVSResources.Place_goto_labels_in_leftmost_column), new("NoIndent", CSharpVSResources.Indent_labels_normally), new("OneLess", CSharpVSResources.Place_goto_labels_one_indent_less_than_current)],
        });
        result.Add(new("spacing.binaryOperatorSpacing", CSharpFormattingOptions2.SpacingAroundBinaryOperator, LanguageNames.CSharp, CSharpVSResources.Set_spacing_for_operators)
        {
            Choices = [new("Ignore", CSharpVSResources.Ignore_spaces_around_binary_operators), new("Remove", CSharpVSResources.Remove_spaces_before_and_after_binary_operators), new("Single", CSharpVSResources.Insert_space_before_and_after_binary_operators)],
        });
        result.Add(new("onTypeFormatting.formatOnTyping", AutoFormattingOptionsStorage.FormatOnTyping, LanguageNames.CSharp, CSharpVSResources.Automatically_format_when_typing));
        result.Add(new("onTypeFormatting.formatOnSemicolon", AutoFormattingOptionsStorage.FormatOnSemicolon, LanguageNames.CSharp, CSharpVSResources.Automatically_format_statement_on_semicolon));
        result.Add(new("onTypeFormatting.formatOnCloseBrace", AutoFormattingOptionsStorage.FormatOnCloseBrace, LanguageNames.CSharp, CSharpVSResources.Automatically_format_block_on_close_brace));
        result.Add(new("onTypeFormatting.formatOnReturn", AutoFormattingOptionsStorage.FormatOnReturn, LanguageNames.CSharp, CSharpVSResources.Automatically_format_on_return));
        result.Add(new("onTypeFormatting.formatOnPaste", FormattingOptionsStorage.FormatOnPaste, LanguageNames.CSharp, CSharpVSResources.Automatically_format_on_paste));
        return result;
    }

    private static void AddFormatting(List<CodeStyleSetting> result, string category, AbstractOptionPreviewViewModel model)
    {
        using (model)
        {
            var braceFlags = new[]
            {
                NewLineBeforeOpenBracePlacement.Types, NewLineBeforeOpenBracePlacement.Methods,
                NewLineBeforeOpenBracePlacement.Properties, NewLineBeforeOpenBracePlacement.Accessors,
                NewLineBeforeOpenBracePlacement.AnonymousMethods, NewLineBeforeOpenBracePlacement.ControlBlocks,
                NewLineBeforeOpenBracePlacement.AnonymousTypes, NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers,
                NewLineBeforeOpenBracePlacement.LambdaExpressionBody,
            };
            var parenthesisFlags = new[] { SpacePlacementWithinParentheses.Expressions, SpacePlacementWithinParentheses.TypeCasts, SpacePlacementWithinParentheses.ControlFlowStatements };
            var braceIndex = 0;
            var parenthesisIndex = 0;
            foreach (var item in model.Items.OfType<AbstractCheckBoxViewModel>())
            {
                int? flag = null;
                var suffix = string.Empty;
                if (item.Option == CSharpFormattingOptions2.NewLineBeforeOpenBrace)
                {
                    var value = braceFlags[braceIndex++];
                    flag = (int)value;
                    suffix = "_" + value;
                }
                else if (item.Option == CSharpFormattingOptions2.SpaceBetweenParentheses)
                {
                    var value = parenthesisFlags[parenthesisIndex++];
                    flag = (int)value;
                    suffix = "_" + value;
                }
                var moniker = flag is null ? CodeStyleSetting.ToUnifiedScalarMoniker(item.Option.Definition.ConfigName) : item.Option.Definition.ConfigName + suffix;
                result.Add(new(category + "." + moniker, item.Option, LanguageNames.CSharp, item.Description, flag));
            }
        }
    }

    private AbstractOptionPreviewViewModel CreatePreviewModel(uint elementId, OptionStore store)
        => elementId switch
        {
            0 => new StyleViewModel(store, this),
            1 => new IndentationViewModel(store, this),
            2 => new NewLinesViewModel(store, this),
            3 => new SpacingViewModel(store, this),
            4 => new WrappingViewModel(store, this),
            _ => throw new ArgumentOutOfRangeException(nameof(elementId)),
        };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _commands?.Dispose();
            foreach (var provider in _providers)
                provider.Dispose();
        }
        base.Dispose(disposing);
    }
}
