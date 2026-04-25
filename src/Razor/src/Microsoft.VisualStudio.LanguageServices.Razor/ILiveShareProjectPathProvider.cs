// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor;

internal interface ILiveShareProjectPathProvider
{
    bool TryGetProjectPath(ITextBuffer textBuffer, [NotNullWhen(true)] out string? filePath);
}
