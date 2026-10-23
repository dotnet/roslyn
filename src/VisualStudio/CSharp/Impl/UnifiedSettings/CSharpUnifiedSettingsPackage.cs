// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnifiedSettings;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageId)]
[ProvideMenuResource("UnifiedSettingsCommands.ctmenu", 1)]
[ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\csharpUnifiedSettings.registration.json")]
[ProvideService(typeof(ProviderService), IsAsyncQueryable = true)]
[ProvideService(typeof(MigratorService), IsAsyncQueryable = true)]
internal sealed class CSharpUnifiedSettingsPackage : UnifiedSettingsPackage
{
    public const string PackageId = "47b0d77e-35c4-4b41-9fc4-9309ef73f93b";
    public const string PreviewFactoryId = "b147d67e-5ddf-4106-bd6e-c6cc49b88141";
    public const string ProviderServiceId = "c573a3f9-63f0-4297-9eb4-eaa2f16b87d7";
    public const string MigratorServiceId = "cf41c6e3-6a8f-43b0-bdb9-2325ec46e6cc";
    private List<CodeStyleSettingsProvider>? _providers;

    protected override string Language => LanguageNames.CSharp;
    protected override Guid CommandSet => new(PackageId);
    protected override Guid PreviewFactory => new(PreviewFactoryId);
    protected override Type ProviderServiceType => typeof(ProviderService);
    protected override Type MigratorServiceType => typeof(MigratorService);

    protected override CodeStyleSettingsPreviewFactory CreatePreviewFactory()
        => new(this, id => GetProviders()[(int)id], CreatePendingStore, CreatePreviewModel, maximumElementId: 4, DisposalToken);

    private List<CodeStyleSettingsProvider> GetProviders()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _providers ??= CreateProviders();
    }

    private List<CodeStyleSettingsProvider> CreateProviders()
    {
        var providers = new List<CodeStyleSettingsProvider>();
        var settings = CreateSettings(GlobalOptions).ToArray();
        var categories = new[] { "", "indentation", "newLines", "spacing", "wrapping", "general" };
        for (var index = 0; index < categories.Length; index++)
        {
            var prefix = categories[index] + ".";
            var pageSettings = settings.Where(setting => index == 0 ? !setting.Moniker.Contains('.') : setting.Moniker.StartsWith(prefix, StringComparison.Ordinal))
                .Select(setting => new CodeStyleSetting(index == 0 ? setting.Moniker : setting.Moniker.Substring(prefix.Length), setting.Key.Option, LanguageNames.CSharp, setting.Description, setting.Flag) { Choices = setting.Choices });
            var provider = new CodeStyleSettingsProvider(GlobalOptions, LanguageNames.CSharp, pageSettings, CreatePendingStore, ThreadingContext);
            providers.Add(provider);
        }
        return providers;
    }

    private OptionStore CreatePendingStore()
    {
        var store = new OptionStore(GlobalOptions);
        foreach (var provider in GetProviders())
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
        if (disposing && _providers is not null)
        {
            foreach (var provider in _providers)
                provider.Dispose();
        }
        base.Dispose(disposing);
    }

    [Guid(ProviderServiceId)]
    private sealed class ProviderService;

    [Guid(MigratorServiceId)]
    private sealed class MigratorService;
}
