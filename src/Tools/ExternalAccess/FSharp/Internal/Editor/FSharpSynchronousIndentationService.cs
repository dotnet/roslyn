// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(IIndentationService), LanguageNames.FSharp)]
    internal class FSharpSynchronousIndentationService : IIndentationService
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly IFSharpSynchronousIndentationService? _legacyService;
#pragma warning restore
        private readonly IFSharpIndentationService? _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpSynchronousIndentationService(
            [Import(AllowDefault = true)] IFSharpSynchronousIndentationService? legacyService,
            [Import(AllowDefault = true)] IFSharpIndentationService? service)
        {
            Contract.ThrowIfTrue(service == null && legacyService == null);

            _legacyService = legacyService;
            _service = service;
        }

        public IndentationResult GetIndentation(SyntacticDocument document, int lineNumber, FormattingOptions.IndentStyle indentStyle, IndentationOptions options, CancellationToken cancellationToken)
        {
            // all F# documents should have a file path
            if (document.Document.FilePath == null)
            {
                return default;
            }

            FSharpIndentationResult? result;
            if (_service != null)
            {

                var fsharpOptions = new FSharpIndentationOptions(
                    TabSize: options.FormattingOptions.Options.GetOption(FormattingOptions2.TabSize),
                    IndentStyle: indentStyle);

                result = _service.GetDesiredIndentation(document.Project.LanguageServices, document.Text, document.Document.Id, document.Document.FilePath, lineNumber, fsharpOptions);
            }
            else
            {
                Contract.ThrowIfNull(_legacyService);
                result = _legacyService.GetDesiredIndentation(document.Document, lineNumber, cancellationToken);
            }

            return result.HasValue ? new IndentationResult(result.Value.BasePosition, result.Value.Offset) : default;
        }
    }
}
