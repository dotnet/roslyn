// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.CodeAnalysis.QuickInfo;
using StreamJsonRpc;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    [ExportStatelessLspService(typeof(HoverHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
    [Method(Methods.TextDocumentHoverName)]
    internal sealed class HoverHandler : IRequestHandler<TextDocumentPositionParams, Hover?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HoverHandler(IGlobalOptionService globalOptions)
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier? GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.AdditionalDocument;
            if (document == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));
            var textInLine = text.Lines.GetLineFromPosition(offset).ToString();

            var equalPosition = textInLine.IndexOf('=');
            var caretPosition = request.Position.Character;

            // The caret may be in a position that is outside the text
            if (caretPosition >= textInLine.Length)
            {
                return null;
            }

            // We don't want to show hovering description if we are over these characters
            var character = textInLine.ElementAt(caretPosition);
            if (character == ' ' || character == '=' || character == ',' || character == ':')
            {
                return null;
            }

            // We are on the left of the setting definition and need to display the settings name description
            if (equalPosition == -1 || caretPosition < equalPosition)
            {
                var settingName = equalPosition == -1 ? textInLine : textInLine[..equalPosition];
                return CreateHover(document, settingName.Trim());
            }

            // We are on the right part of the setting definition

            // We look for a semicolon
            var colonPosition = textInLine.IndexOf(':');
            if (caretPosition < colonPosition)
            {
                return HandleMultiValuedSettings(document, textInLine, caretPosition, equalPosition, ':');
            }

            // We look for commas
            var commaPosition = textInLine.IndexOf(',');
            if (caretPosition < commaPosition)
            {
                return HandleMultiValuedSettings(document, textInLine, caretPosition, equalPosition, ',');
            }

            // We didn't find a comma or colon, so we just display the value description
            var name = textInLine[..equalPosition].Trim();
            var value = textInLine[(equalPosition + 1)..].Trim();
            return CreateHover(document, name, value, true);
        }

        private static string? FindDescriptionForSetting(TextDocument document, string settingName, string settingValue, bool displayValueInfo)
        {
            var workspace = document.Project.Solution.Workspace;
            var filePath = document.FilePath;
            var optionSet = workspace.Options;
            Contract.ThrowIfNull(filePath);

            var settingsSnapshots = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
            var foundSetting = settingsSnapshots.Where(sett => sett.GetSettingName() == settingName);

            if (foundSetting.Any())
            {
                var setting = foundSetting.First();
                if (displayValueInfo)
                {
                    var value = setting.GetSettingValues(optionSet)?.Where(val => val == settingValue).FirstOrDefault();
                    return value != null ? setting.GetValueDocumentation(settingValue) : null;
                }

                return setting.GetDocumentation();
            }

            return null;
        }

        private static Hover? CreateHover(TextDocument document, string settingName, string settingValue = "", bool displayValueInfo = false)
        {
            var description = FindDescriptionForSetting(document, settingName, settingValue, displayValueInfo);

            if (description != null)
            {
                return new Hover
                {
                    Contents = new MarkupContent
                    {
                        Kind = MarkupKind.Markdown,
                        Value = description,
                    },
                };
            }

            return null;
        }

        private static Hover? HandleMultiValuedSettings(TextDocument document, string textInLine, int caretPosition, int equalPosition, char splitCharacter)
        {
            var values = textInLine[(equalPosition + 1)..].Split(splitCharacter);
            var cont = equalPosition + 1;
            foreach (var element in values)
            {
                cont += element.Length + 1;
                if (caretPosition < cont)
                {
                    var settingName = textInLine[..equalPosition].Trim();
                    var settingValue = element.Trim();
                    return CreateHover(document, settingName, settingValue, true);
                }
            }

            return null;
        }
    }
}
