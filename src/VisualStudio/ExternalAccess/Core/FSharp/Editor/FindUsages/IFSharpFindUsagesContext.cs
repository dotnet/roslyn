// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.VisualStudio.ExternalAccess.FSharp.FindUsages;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor.FindUsages;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages;
#endif

internal interface IFSharpFindUsagesContext
{
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Report a message to be displayed to the user.
    /// </summary>
    Task ReportMessageAsync(string message);

    /// <summary>
    /// Set the title of the window that results are displayed in.
    /// </summary>
    Task SetSearchTitleAsync(string title);

    Task OnDefinitionFoundAsync(FSharpDefinitionItem definition);
    Task OnReferenceFoundAsync(FSharpSourceReferenceItem reference);

    Task ReportProgressAsync(int current, int maximum);
}
