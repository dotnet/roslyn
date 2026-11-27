// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

[Shared]
[Export(typeof(ILoggerFactory))]
[method: ImportingConstructor]
internal sealed class LoggerFactory(ILoggerProvider provider)
    : AbstractLoggerFactory([provider])
{
}
