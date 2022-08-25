// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Enables legacy APIs to access indentation inference editor APIs from workspace.
/// https://github.com/dotnet/roslyn/issues/61109
/// </summary>
internal interface ILegacyIndentationManagerWorkspaceService : IWorkspaceService
{
    bool UseSpacesForWhitespace(SourceText text);
    int GetTabSize(SourceText text);
    int GetIndentSize(SourceText text);
}
