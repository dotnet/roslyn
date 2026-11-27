// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

[Shared]
[Export(typeof(ILoggerProvider))]
[method: ImportingConstructor]
internal class LoggerProvider(RazorClientServerManagerProvider razorClientServerManagerProvider) : ILoggerProvider
{
    private readonly RazorClientServerManagerProvider _razorClientServerManagerProvider = razorClientServerManagerProvider;

    public ILogger CreateLogger(string categoryName)
    {
        return new LspLogger(categoryName, _razorClientServerManagerProvider);
    }
}
