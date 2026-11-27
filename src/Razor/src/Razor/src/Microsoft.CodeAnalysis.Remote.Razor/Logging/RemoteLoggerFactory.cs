// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Remote.Razor.Logging;

[Shared]
[Export(typeof(ILoggerFactory))]
[Export(typeof(RemoteLoggerFactory))]
internal sealed class RemoteLoggerFactory : ILoggerFactory
{
    private ILoggerFactory _targetLoggerFactory = EmptyLoggerFactory.Instance;

    /// <summary>
    /// Sets the logger factory to use in OOP
    /// </summary>
    /// <returns>True if the factory was set</returns>
    internal bool SetTargetLoggerFactory(ILoggerFactory loggerFactory)
    {
        // We only set the target logger factory if the current factory is empty.
        if (_targetLoggerFactory is EmptyLoggerFactory)
        {
            _targetLoggerFactory = loggerFactory;
            return true;
        }

        return false;
    }

    public void AddLoggerProvider(ILoggerProvider provider)
    {
        _targetLoggerFactory.AddLoggerProvider(provider);
    }

    public ILogger GetOrCreateLogger(string categoryName)
    {
        return _targetLoggerFactory.GetOrCreateLogger(categoryName);
    }

    public void Dispose()
    {
    }
}
