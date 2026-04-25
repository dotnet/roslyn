// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(IFilePathService))]
[method: ImportingConstructor]
internal sealed class VisualStudioFilePathService() : AbstractFilePathService
{
}
