// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor;

[Export(typeof(ISmartIndentProvider))]
[ContentType(ContentTypeNames.FSharpContentType)]
internal sealed class FSharpSmartIndentProvider : ISmartIndentProvider
{
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly IFSharpSynchronousIndentationService? _legacyService;
#pragma warning restore
    private readonly IFSharpIndentationService? _service;

    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpSmartIndentProvider(
        [Import(AllowDefault = true)] IFSharpSynchronousIndentationService? legacyService,
        [Import(AllowDefault = true)] IFSharpIndentationService? service,
        IGlobalOptionService globalOptions)
    {
        Contract.ThrowIfTrue(service == null && legacyService == null);

        _legacyService = legacyService;
        _service = service;
        _globalOptions = globalOptions;
    }

    public ISmartIndent? CreateSmartIndent(ITextView textView)
        => _globalOptions.GetOption(SmartIndenterOptionsStorage.SmartIndenter) ? new SmartIndent(textView, this) : null;

    private sealed class SmartIndent : ISmartIndent
    {
        private readonly ITextView _textView;
        private readonly FSharpSmartIndentProvider _provider;

        public SmartIndent(ITextView textView, FSharpSmartIndentProvider provider)
        {
            _textView = textView;
            _provider = provider;
        }

        public void Dispose()
        {
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
            => GetDesiredIndentation(line, CancellationToken.None);

        private int? GetDesiredIndentation(ITextSnapshotLine line, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SmartIndentation_Start, cancellationToken))
            {
                var document = line.Snapshot.GetOpenDocumentInCurrentContextWithChanges();

                // all F# documents should have a file path
                if (document?.FilePath == null)
                    return null;

                FSharpIndentationResult? result;
                if (_provider._service != null)
                {
                    var text = document.GetTextSynchronously(cancellationToken);

                    var indentStyle = _provider._globalOptions.GetOption(IndentationOptionsStorage.SmartIndent, document.Project.Language);

                    var fsharpOptions = new FSharpIndentationOptions(
                        TabSize: _textView.Options.GetOptionValue(DefaultOptions.TabSizeOptionId),
                        IndentStyle: (FormattingOptions.IndentStyle)indentStyle);

#pragma warning disable 0618 // Compat with existing EA api
                    result = _provider._service.GetDesiredIndentation(document.Project.LanguageServices, text, document.Id, document.FilePath, line.LineNumber, fsharpOptions);
#pragma warning restore
                }
                else
                {
                    Contract.ThrowIfNull(_provider._legacyService);
                    result = _provider._legacyService.GetDesiredIndentation(document, line.LineNumber, cancellationToken);
                }

                return result.HasValue ? new IndentationResult(result.Value.BasePosition, result.Value.Offset).GetIndentation(_textView, line) : null;
            }
        }
    }
}
