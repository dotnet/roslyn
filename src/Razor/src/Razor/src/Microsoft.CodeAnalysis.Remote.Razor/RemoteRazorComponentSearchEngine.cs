// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Export(typeof(IRazorComponentSearchEngine)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteRazorComponentSearchEngine(ILoggerFactory loggerFactory) : RazorComponentSearchEngine(loggerFactory)
{
}
