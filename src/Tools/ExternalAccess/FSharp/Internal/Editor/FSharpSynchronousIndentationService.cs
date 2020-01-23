// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(IIndentationService), LanguageNames.FSharp)]
    internal class FSharpSynchronousIndentationService : IIndentationService
    {
        private readonly IFSharpSynchronousIndentationService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpSynchronousIndentationService(IFSharpSynchronousIndentationService service)
        {
            _service = service;
        }

        public IndentationResult GetIndentation(Document document, int lineNumber, FormattingOptions.IndentStyle indentStyle, CancellationToken cancellationToken)
        {
            var result = _service.GetDesiredIndentation(document, lineNumber, cancellationToken);
            if (result.HasValue)
            {
                return new IndentationResult(result.Value.BasePosition, result.Value.Offset);
            }
            else
            {
                return default;
            }
        }
    }
}
