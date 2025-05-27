// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

/// <summary>
/// A helper class that implements the creation of GraphNodeIds that matches the .dgml creation
/// by the metadata progression provider.
/// </summary>
internal static class GraphNodeIdCreation
{
    public static GraphNodeId GetIdForDocument(Document document)
    {
        return
            GraphNodeId.GetNested(
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, new Uri(document.Project.FilePath, UriKind.RelativeOrAbsolute)),
                GraphNodeId.GetPartial(CodeGraphNodeIdName.File, new Uri(document.FilePath, UriKind.RelativeOrAbsolute)));
    }
}
