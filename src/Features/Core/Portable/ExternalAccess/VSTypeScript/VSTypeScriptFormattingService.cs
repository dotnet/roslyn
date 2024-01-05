// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(IFormattingService), InternalLanguageNames.TypeScript), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class VSTypeScriptFormattingService([Import(AllowDefault = true)] IVSTypeScriptFormattingServiceImplementation? impl) : IFormattingService
    {
        // 'impl' is a required import, but MEF 2 does not support silent part rejection when a required import is
        // missing so we combine AllowDefault with a null check in the constructor to defer the exception until the part
        // is instantiated.
        private readonly IVSTypeScriptFormattingServiceImplementation _impl = impl ?? throw new ArgumentNullException(nameof(impl));

        public Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, LineFormattingOptions lineFormattingOptions, SyntaxFormattingOptions? syntaxFormattingOptions, CancellationToken cancellationToken)
        {
            var tsOptions = new VSTypeScriptIndentationOptions(
                UseSpaces: !lineFormattingOptions.UseTabs,
                TabSize: lineFormattingOptions.TabSize,
                IndentSize: lineFormattingOptions.IndentationSize);

            return _impl.FormatAsync(document, spans, tsOptions, cancellationToken);
        }
    }
}
