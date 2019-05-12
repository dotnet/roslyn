// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(ISynchronousIndentationService), LanguageNames.FSharp)]
    internal class FSharpSynchronousIndentationService : ISynchronousIndentationService
    {
        private readonly IFSharpSynchronousIndentationService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpSynchronousIndentationService(IFSharpSynchronousIndentationService service)
        {
            _service = service;
        }

        public CodeAnalysis.Editor.IndentationResult? GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var result = _service.GetDesiredIndentation(document, lineNumber, cancellationToken);
            if (result.HasValue)
            {
                return new CodeAnalysis.Editor.IndentationResult(result.Value.BasePosition, result.Value.Offset);
            }
            else
            {
                return null;
            }
        }
    }
}
