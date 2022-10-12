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
    internal sealed class VSTypeScriptFormattingService : IFormattingService
    {
        private readonly IVSTypeScriptFormattingServiceImplementation _impl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptFormattingService([Import(AllowDefault = true)] IVSTypeScriptFormattingServiceImplementation impl)
            => _impl = impl ?? throw new ArgumentNullException(nameof(impl));

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
