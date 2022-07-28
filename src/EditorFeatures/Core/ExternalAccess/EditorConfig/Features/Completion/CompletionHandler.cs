// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Roslyn.Utilities;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using System.Net.WebSockets;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    [ExportStatelessLspService(typeof(CompletionHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
    [Method(Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : IRequestHandler<CompletionParams, CompletionList?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(CompletionParams request) => request.TextDocument;

        public async Task<CompletionList?> HandleRequestAsync(CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.AdditionalDocument;
            Contract.ThrowIfNull(document);

            var workspace = context.Solution?.Workspace;
            Contract.ThrowIfNull(workspace);

            Contract.ThrowIfNull(request.Context);

            var filePath = document.FilePath;
            Contract.ThrowIfNull(filePath);

            var optionSet = workspace.Options;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));
            var textInLine = text.Lines.GetLineFromPosition(offset).ToString();

            var settingsAggregator = workspace.Services.GetRequiredService<ISettingsAggregator>();

            var codeStyleProvider = settingsAggregator.GetSettingsProvider<CodeStyleSetting>(filePath);
            var whitespaceProvider = settingsAggregator.GetSettingsProvider<WhitespaceSetting>(filePath);
            var analyzerProvider = settingsAggregator.GetSettingsProvider<AnalyzerSetting>(filePath);

            var codeStyleSnapshot = codeStyleProvider?.GetCurrentDataSnapshot();
            var whitespaceSnapshot = whitespaceProvider?.GetCurrentDataSnapshot();
            var analyzerSnapshot = analyzerProvider?.GetCurrentDataSnapshot();

            // Check if the user has written a name and not a value
            if (textInLine.Contains(' ') && !textInLine.Contains('='))
            {
                return null;
            }

            // Check if we need to show the name of the setting or the values
            if (textInLine.Contains('='))
            {
                if (textInLine.Count(c => c == '=') > 1 || textInLine.Count(c => c == ':') > 1)
                {
                    return null;
                }

                var settingName = textInLine.Split('=').FirstOrDefault().Trim();

                // Check if we need to show severities (only for code style settings)
                if (textInLine.Contains(':'))
                {
                    var severities = GetSettingValues(settingName, codeStyleSnapshot, whitespaceSnapshot, analyzerSnapshot, true, optionSet);
                    if (severities == null)
                    {
                        return null;
                    }

                    return new CompletionList
                    {
                        Items = severities,
                    };
                }

                // Generate completion for setting values
                var values = GetSettingValues(settingName, codeStyleSnapshot, whitespaceSnapshot, analyzerSnapshot, false, optionSet);
                if (values == null)
                {
                    return null;
                }

                return new CompletionList
                {
                    Items = values,
                };
            }

            // Generate completion for settings names
            var codeStyleSettingsItems = codeStyleSnapshot?.Select(GenerateSettingNameCompletionItem);
            var whitespaceSettingsItems = whitespaceSnapshot?.Select(GenerateSettingNameCompletionItem);
            var analyzerSettingsItems = analyzerSnapshot?.Select(GenerateSettingNameCompletionItem);
            var settingsItems = codeStyleSettingsItems.Concat(whitespaceSettingsItems).Concat(analyzerSettingsItems).Where(item => item != null) as IEnumerable<CompletionItem>;
            if (settingsItems == null)
            {
                return null;
            }

            return new CompletionList
            {
                Items = settingsItems.ToArray(),
            };
        }

        private static CompletionItem? GenerateSettingNameCompletionItem<T>(T setting) where T : IEditorConfigSettingInfo
        {
            var name = setting.GetSettingName();
            if (name == null)
            {
                return null;
            }
            var documentation = setting.GetDocumentation();
            var commitCharacters = new string[] { " ", "=" };

            return CreateCompletionItem(name, name, CompletionItemKind.Property, documentation, commitCharacters);
        }

        private static CompletionItem[]? GenerateSettingValuesCompletionItem<T>(T setting, bool additional, bool isCodeStyle, OptionSet optionSet) where T : IEditorConfigSettingInfo
        {
            var values = new List<CompletionItem>();

            // Create severities list only if there exists ':' and we are in a code style setting
            if (additional && isCodeStyle)
            {
                values.Add(CreateCompletionItem("silent", "silent", CompletionItemKind.Value, null, null));
                values.Add(CreateCompletionItem("suggestion", "suggestion", CompletionItemKind.Value, null, null));
                values.Add(CreateCompletionItem("warning", "warning", CompletionItemKind.Value, null, null));
                values.Add(CreateCompletionItem("error", "error", CompletionItemKind.Value, null, null));

                return values.ToArray();
            }

            // User may type a ':' but not in a code style setting
            if (additional && !isCodeStyle)
            {
                return null;
            }

            // Create normal values list
            var settingValues = setting.GetSettingValues(optionSet);
            if (settingValues == null)
            {
                return null;
            }

            foreach (var value in settingValues)
            {
                var commitCharacters = isCodeStyle ? new string[] { ":" } : Array.Empty<string>();
                values.Add(CreateCompletionItem(value, value, CompletionItemKind.Value, null, commitCharacters));
            }

            return values.ToArray();
        }

        private static CompletionItem CreateCompletionItem(string label, string insertText, CompletionItemKind kind, string? documentation, string[]? commitCharacters)
        {
            var item = new CompletionItem
            {
                Label = label,
                InsertText = insertText,
                Kind = kind,
                Documentation = documentation,
                CommitCharacters = commitCharacters,
            };
            return item;
        }

        private static CompletionItem[]? GetSettingValues(string settingName, ImmutableArray<CodeStyleSetting>? cs, ImmutableArray<WhitespaceSetting>? ws, ImmutableArray<AnalyzerSetting>? a, bool additional, OptionSet optionSet)
        {
            var codestyleSettings = cs?.Where(setting => setting.GetSettingName() == settingName);
            if (codestyleSettings.Any())
            {
                return GenerateSettingValuesCompletionItem(codestyleSettings.First(), additional, true, optionSet);
            }

            var whitespaceSettings = ws?.Where(setting => setting.GetSettingName() == settingName);
            if (whitespaceSettings.Any())
            {
                return GenerateSettingValuesCompletionItem(whitespaceSettings.First(), additional, false, optionSet);
            }

            var analyzerSettings = a?.Where(setting => setting.GetSettingName() == settingName);
            if (analyzerSettings.Any())
            {
                return GenerateSettingValuesCompletionItem(analyzerSettings.First(), additional, false, optionSet);
            }

            return null;
        }
    }
}
