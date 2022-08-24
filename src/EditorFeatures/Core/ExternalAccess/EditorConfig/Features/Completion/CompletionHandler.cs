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
using Roslyn.Utilities;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

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

            if (request.Context == null)
            {
                return null;
            }

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
            // Show completion: |setting_name = (caret)
            // Show completion: |setting_name = setting_va(caret)
            // Show completion: |setting_name = setting_value_1, (caret)
            // Dont't show completion: | setting_name = setting_value (caret)
            var seenWhitespace = false;
            foreach (var element in textToCheck)
            {
                if (char.IsWhiteSpace(element))
                {
                    seenWhitespace = true;
                }
                else if (element == ',')
                {
                    return CreateCompletionList(document, textInLine, allowsMultipleValues: true);
                }
                else if (element == '=')
                {
                    return CreateCompletionList(document, textInLine);
                }
                else
                {
                    if (seenWhitespace)
                    {
                        return null;
                    }
                }
            }

            // Check if we need to suggest completion for the setting name
            // Show completion: |indent(caret)
            // Don't show completion: |indent_size (caret)
            seenWhitespace = false;
            foreach (var element in textToCheck)
            {
                if (seenWhitespace && !char.IsWhiteSpace(element))
                {
                    return null;
                }
                seenWhitespace = char.IsWhiteSpace(element);
            }

            return CreateCompletionList(document, textInLine, showValueList: false);
        }

        private static CompletionItem? GenerateSettingNameCompletionItem(IEditorConfigSettingInfo setting)
        {
            var name = setting.GetSettingName();
            if (name == null)
            {
                return null;
            }
            var documentation = setting.GetDocumentation();

            return CreateCompletionItem(name, name, CompletionItemKind.Property, documentation, _settingNameCommitCharacters);
        }

        private static CompletionItem[]? GenerateSettingValuesCompletionItem(IEditorConfigSettingInfo setting, bool additional)
        {
            var values = new List<CompletionItem>();
            var allowsMultipleValues = setting.AllowsMultipleValues();

            // User may type a ',' but not in a setting that allows multiple values
            if (additional && (!allowsMultipleValues))
            {
                return null;
            }

            // Create normal values list
            var settingValues = setting.GetSettingValues();
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

        private static CompletionItem[]? GetSettingValues(string settingName, ImmutableArray<IEditorConfigSettingInfo> settingsSnapshot, bool multipleValues = false)
        {
            var foundSetting = settingsSnapshot.Where(sett => sett.GetSettingName() == settingName);
            if (foundSetting.Any())
            {
                return GenerateSettingValuesCompletionItem(foundSetting.First(), multipleValues);
            }

            return null;
        }

        private static CompletionList? CreateCompletionList(TextDocument document, string textInLine, bool showValueList = true, bool allowsMultipleValues = false)
        {
            var workspace = document.Project.Solution.Workspace;
            var filePath = document.FilePath;
            Contract.ThrowIfNull(filePath);

            var settingsSnapshots = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
            var settingName = textInLine.Split('=').First().Trim();

            if (showValueList)
            {
                var values = GetSettingValues(settingName, settingsSnapshots, multipleValues: allowsMultipleValues);
                return values == null ? null : new CompletionList { Items = values };
            }

            var names = settingsSnapshots.Select(GenerateSettingNameCompletionItem).WhereNotNull().GroupBy(x => x.Label).Select(grp => grp.First());
            return names == null ? null : new CompletionList { Items = names.ToArray() };
        }
    }
}
