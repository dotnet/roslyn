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

            // We don't want to show hovering description if we are over these symbols
            var character = textInLine.ElementAt(caretPosition);
            if (character == ' ' || character == '=' || character == ',')
            {
                return null;
            }

            // We are on the left of the setting and need to display the settings name description
            if (equalPosition != -1 && caretPosition < equalPosition)
            {
                var settingName = textInLine[..equalPosition].Replace(" ", "");
                var description = FindDescriptionForSetting(document, settingName);

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
            }

            // We are on the right of the setting and need to display the settings values description
            else if (equalPosition != -1 && caretPosition > equalPosition)
            {
                var settingName = textInLine[..equalPosition].Replace(" ", "");
                var description = FindDescriptionForSetting(document, settingName, nameDescription: false);

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
            }

            return null;
        }

        private static string? FindDescriptionForSetting(TextDocument document, string settingName, bool nameDescription = true)
        {
            var workspace = document.Project.Solution.Workspace;
            var filePath = document.FilePath;
            Contract.ThrowIfNull(filePath);

            var settingsSnapshots = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
            var codeStyleSetting = settingsSnapshots.codeStyleSnapshot?.Where(sett => sett.GetSettingName() == settingName);
            if (codeStyleSetting.Any())
            {
                var setting = codeStyleSetting.First();
                return nameDescription ? setting.GetDocumentation() : setting.Type.ToString();
            }

            var whitespaceSetting = settingsSnapshots.whitespaceSnapshot?.Where(sett => sett.GetSettingName() == settingName);
            if (whitespaceSetting.Any())
            {
                var setting = whitespaceSetting.First();
                return nameDescription ? setting.GetDocumentation() : setting.Type.ToString();
            }

            var analyzerSetting = settingsSnapshots.analyzerSnapshot?.Where(sett => sett.GetSettingName() == settingName);
            if (analyzerSetting.Any())
            {
                var setting = analyzerSetting.First();
                return nameDescription ? setting.GetDocumentation() : "Analyzer setting value";
            }

            return null;
        }
    }
}
