// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

internal class ProjectContextList : VSProjectContextList
{
    //
    // Summary:
    //     Gets or sets the value which maps project ids to intermediate output paths
    [DataMember(Name = "_roslyn_projectIdMap")]
    public required Dictionary<string, string?> ProjectIdToIntermediatePathMap { get; set; }
}
