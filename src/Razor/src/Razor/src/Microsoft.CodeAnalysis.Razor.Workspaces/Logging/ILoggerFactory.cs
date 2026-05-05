// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Logging;

// Our version of ILoggerFactory, so that we're not MEF importing general use types
internal interface ILoggerFactory
{
    void AddLoggerProvider(ILoggerProvider provider);
    ILogger GetOrCreateLogger(string categoryName);
}
