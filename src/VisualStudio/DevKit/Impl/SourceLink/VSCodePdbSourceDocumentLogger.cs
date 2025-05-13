// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Services.SourceLink;

[Export(typeof(IPdbSourceDocumentLogger)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSCodePdbSourceDocumentLogger(ILoggerFactory loggerFactory) : IPdbSourceDocumentLogger
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("SourceLink");

    public void Clear()
    {
        // Do nothing, we just leave all the logs up.
        return;
    }

    public void Log(string message)
    {
        _logger.LogTrace(message);
    }
}
