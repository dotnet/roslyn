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
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Collections.Internal;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    [ExportStatelessLspService(typeof(CompletionHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
    [Method(Methods.TextDocumentCompletionName)]
    internal sealed class CompletionHandler : IRequestHandler<CompletionParams, CompletionList?>
    {
        private static readonly ImmutableArray<string> _settingNameCommitCharacters = ImmutableArray.Create(new string[] { " ", "=" });
        private static readonly ImmutableArray<string> _multipleValuesCommitCharacters = ImmutableArray.Create(new string[] { "," });

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

            var workspace = document.Project.Solution.Workspace;

            if (request.Context == null)
            {
                return null;
            }

            var filePath = document.FilePath;
            Contract.ThrowIfNull(filePath);

            var optionSet = workspace.Options;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));
            var textInLine = text.Lines.GetLineFromPosition(offset).ToString();
            var textToCheck = textInLine[..request.Position.Character].Reverse();

            // Correct syntax is setting_name = setting_value_1, setting_value2, setting_value3... or setting_name = setting_value 
            // When there exists more then one '=' it is incorrect and we should not suggest any completion 
            if (textInLine.Count(c => c == '=') > 1)
            {
                return null;
            }

            // Check if we need to display values of the settings
            // |setting_name = (caret is here)
            // |setting_name = setting_value_1, (caret is here)
            bool showValueComma = false, showValueEqual = false, showName = false, seenWhitespace = false;
            foreach (var element in textToCheck)
            {
                if (element == ' ')
                {
                    seenWhitespace = true;
                }
                else if (element == ',')
                {
                    showValueComma = true;
                    break;
                }
                else if (element == '=')
                {
                    showValueEqual = true;
                    break;
                }
                else
                {
                    if (seenWhitespace)
                    {
                        showValueEqual = false;
                        break;
                    }
                }
            }

            // Check if we need to suggest completion for the setting name
            // Show completion |indent(caret is here)
            // Don't show completion |indent_size (caret is here)
            if (!showValueComma && !showValueEqual)
            {
                seenWhitespace = false;
                foreach (var element in textToCheck)
                {
                    if (seenWhitespace && !(element == ' '))
                    {
                        showName = false;
                        break;
                    }
                    seenWhitespace = element == ' ';
                    showName = true;
                }
            }

            // Show completion additional values (after a comma)
            if (showValueComma)
            {
                var settingName = textInLine.Split('=').First().Trim();
                var settingsSnapshots1 = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
                var options = GetSettingValues(settingName, settingsSnapshots1, optionSet, multipleValues: true);
                if (options == null)
                {
                    return null;
                }

                return new CompletionList
                {
                    Items = options,
                };
            }

            // Show completion for setting values (after equal)
            if (showValueEqual)
            {
                var settingName = textInLine.Split('=').First().Trim();
                var settingsSnapshots2 = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
                var values = GetSettingValues(settingName, settingsSnapshots2, optionSet);
                if (values == null)
                {
                    return null;
                }

                return new CompletionList
                {
                    Items = values,
                };
            }

            // Show completion for the setting name
            if (showName)
            {
                var settingsSnapshots3 = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
                var settingsItems = settingsSnapshots3.Select(GenerateSettingNameCompletionItem).WhereNotNull().GroupBy(x => x.Label).Select(grp => grp.First());
                if (settingsItems == null)
                {
                    return null;
                }

                return new CompletionList
                {
                    Items = settingsItems.ToArray(),
                };
            }

            return null;
        }

        private static CompletionItem? GenerateSettingNameCompletionItem(IEditorConfigSettingInfo setting)
        {
            var name = setting.GetSettingName();
            if (name == null)
            {
                return null;
            }
            var documentation = setting.GetDocumentation();
            var commitCharacters = _settingNameCommitCharacters;

            return CreateCompletionItem(name, name, CompletionItemKind.Property, documentation, commitCharacters);
        }

        private static CompletionItem[]? GenerateSettingValuesCompletionItem(IEditorConfigSettingInfo setting, bool additional, OptionSet optionSet, bool allowsMultipleValues = false)
        {
            var values = new List<CompletionItem>();

            // User may type a ',' but not in a setting that allows multiple values
            if (additional && (!allowsMultipleValues))
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
                var commitCharacters = allowsMultipleValues ? _multipleValuesCommitCharacters : ImmutableArray.Create(Array.Empty<string>());
                values.Add(CreateCompletionItem(value, value, CompletionItemKind.Value, null, commitCharacters));
            }

            return values.ToArray();
        }

        private static CompletionItem CreateCompletionItem(string label, string insertText, CompletionItemKind kind, string? documentation, ImmutableArray<string> commitCharacters)
        {
            var item = new CompletionItem
            {
                Label = label,
                InsertText = insertText,
                Kind = kind,
                Documentation = documentation,
                CommitCharacters = commitCharacters.ToArray(),
            };
            return item;
        }

        private static CompletionItem[]? GetSettingValues(string settingName, ImmutableArray<IEditorConfigSettingInfo> settingsSnapshot, OptionSet optionSet, bool multipleValues = false)
        {
            var foundSetting = settingsSnapshot.Where(sett => sett.GetSettingName() == settingName);
            if (foundSetting.Any())
            {
                var allowsMultipleValues = settingName == "csharp_new_line_before_open_brace" || settingName == "csharp_space_between_parentheses";
                return GenerateSettingValuesCompletionItem(foundSetting.First(), multipleValues, optionSet, allowsMultipleValues: allowsMultipleValues);
            }

            return null;
        }
    }
}
