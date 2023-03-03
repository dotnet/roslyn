// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

[Export(typeof(ILoggerFactory))]
[Export(typeof(ServerLoggerFactory))]
[Shared]
internal class ServerLoggerFactory : ILoggerFactory
{
    private ILoggerFactory? _loggerFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServerLoggerFactory()
    {
    }

    public void SetFactory(ILoggerFactory loggerFactory)
    {
        Contract.ThrowIfTrue(_loggerFactory is not null);
        _loggerFactory = loggerFactory;
    }

    void ILoggerFactory.AddProvider(ILoggerProvider provider)
    {
        Contract.ThrowIfNull(_loggerFactory);
        _loggerFactory.AddProvider(provider);
    }

    ILogger ILoggerFactory.CreateLogger(string categoryName)
    {
        Contract.ThrowIfNull(_loggerFactory);
        return _loggerFactory.CreateLogger(categoryName);
    }

    void IDisposable.Dispose()
    {
        _loggerFactory?.Dispose();
    }
}
