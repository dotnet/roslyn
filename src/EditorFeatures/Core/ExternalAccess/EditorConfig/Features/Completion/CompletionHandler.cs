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
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.AccessControl;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

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

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));
            var textInLine = text.Lines.GetLineFromPosition(offset).ToString();

            var settingsAggregator = workspace.Services.GetRequiredService<ISettingsAggregator>();

            var codeStyleProvider = settingsAggregator.GetSettingsProvider<CodeStyleSetting>(filePath);
            var codeStyleSettings = codeStyleProvider?.GetCurrentDataSnapshot();

            var whitespaceProvider = settingsAggregator.GetSettingsProvider<WhitespaceSetting>(filePath);
            var whitespaceSettings = whitespaceProvider?.GetCurrentDataSnapshot().Select(GetSettingsNameCompletion);

            var analyzerProvider = settingsAggregator.GetSettingsProvider<AnalyzerSetting>(filePath);
            var analyzerSettings = analyzerProvider?.GetCurrentDataSnapshot().Select(GetSettingsNameCompletion);

            var namingStyleProvider = settingsAggregator.GetSettingsProvider<NamingStyleSetting>(filePath);
            var namingStyleSettings = namingStyleProvider?.GetCurrentDataSnapshot();

            var settings = whitespaceSettings.Concat(analyzerSettings);
            if (settings == null)
            {
                return null;
            }

            var commitCharactersCache = new Dictionary<EditorConfigCompletionKind, string[]>();

            if (textInLine.Contains("="))
            {
                var settingName = textInLine.Split('=').FirstOrDefault().Split(' ').FirstOrDefault();
                var values = GetSettingsValues(settingName, settings);
                return new CompletionList
                {
                    Items = values.Select(value => CreateCompletionItem(value, commitCharactersCache)).ToArray(),
                };
            }

            return new CompletionList
            {
                Items = settings.Select(setting => CreateCompletionItem(setting, commitCharactersCache)).ToArray(),
            };
        }

        private static EditorConfigCompletionItem GetSettingsNameCompletion<T>(T setting)
        {
            var item = new EditorConfigCompletionItem();

            if (setting is WhitespaceSetting whitespaceSetting)
            {
                var settingText = ((IEditorConfigStorageLocation2)whitespaceSetting.Key.Option.StorageLocations.First()).KeyName;
                var valueType = whitespaceSetting.Type;
                var values = valueType != null ? GetValues(valueType) : Array.Empty<string>();
                item = new EditorConfigCompletionItem
                {
                    Label = settingText,
                    InsertText = settingText,
                    Documentation = whitespaceSetting.Description,
                    CommitCharacters = new string[] { " ", "=" },
                    Values = values,
                };
            }

            if (setting is CodeStyleSetting codeStyleSetting)
            {
                return item;
            }

            if (setting is AnalyzerSetting analyzerSetting)
            {
                var settingText = $"dotnet_diagnostic.{analyzerSetting.Id}.severity";
                item = new EditorConfigCompletionItem
                {
                    Label = settingText,
                    InsertText = settingText,
                    Documentation = analyzerSetting.Description,
                    CommitCharacters = new string[] { " ", "=" },
                    Values = new string[] { "silent", "suggestion", "warning", "error" },
                };
            }

            if (setting is NamingStyleSetting namingStyleSetting)
            {
                return item;
            }

            return item;
        }

        private static IEnumerable<EditorConfigCompletionItem>? GetSettingsValues(string settingName, IEnumerable<EditorConfigCompletionItem> settings)
        {
            var values = settings.Where(setting => setting.Label == settingName);

            if (values.IsEmpty())
                return null;

            return values.First().Values.Select(value =>
                new EditorConfigCompletionItem
                {
                    Label = value,
                    InsertText = value,
                    Kind = EditorConfigCompletionKind.Value,
                    CommitCharacters = Array.Empty<string>(),
                }
            );
        }

        private static CompletionItem CreateCompletionItem(EditorConfigCompletionItem editorConfigCompletion, Dictionary<EditorConfigCompletionKind, string[]> commitCharactersCache)
        {
            var item = new CompletionItem
            {
                Label = editorConfigCompletion.Label,
                InsertText = editorConfigCompletion.InsertText,
                Kind = GetItemKind(editorConfigCompletion.Kind),
                Documentation = editorConfigCompletion.Documentation,
                CommitCharacters = GetCommitCharacters(editorConfigCompletion, commitCharactersCache),
            };
            return item;
        }

        private static CompletionItemKind GetItemKind(EditorConfigCompletionKind kind)
        {
            switch (kind)
            {
                case EditorConfigCompletionKind.Property:
                    return CompletionItemKind.Property;
                case EditorConfigCompletionKind.Value:
                    return CompletionItemKind.Value;
                default:
                    Debug.Fail($"Unhandled {nameof(EditorConfigCompletionKind)}: {Enum.GetName(typeof(EditorConfigCompletionKind), kind)}");
                    return CompletionItemKind.Text;
            }
        }

        private static string[] GetCommitCharacters(EditorConfigCompletionItem completionItem, Dictionary<EditorConfigCompletionKind, string[]> commitCharactersCache)
        {
            if (commitCharactersCache.TryGetValue(completionItem.Kind, out var cachedCharacters))
            {
                // If we have already cached the commit characters, return the cached ones
                return cachedCharacters.ToArray();
            }

            var commitCharacters = completionItem.CommitCharacters;
            commitCharactersCache.Add(completionItem.Kind, commitCharacters);
            return commitCharacters.ToArray();
        }

        private static string[] GetValues(Type type)
        {
            if (type.Name == "Boolean")
            {
                return new string[] { "true", "false" };
            }
            if (type.Name == "Int32")
            {
                return new string[] { "2", "4", "6", "8" };
            }
            if (type.Name == "String")
            {
                return new string[] { "Not yet implemented!" };
            }
            if (type.BaseType?.Name == "Enum")
            {
                var enumName = type.GetEnumValues();
                return type.GetEnumNames();
            }

            return Array.Empty<string>();
        }
    }
}
