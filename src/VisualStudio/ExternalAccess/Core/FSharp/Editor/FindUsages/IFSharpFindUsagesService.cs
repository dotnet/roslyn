// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor.FindUsages;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages;
#endif

internal interface IFSharpFindUsagesService
{
    /// <summary>
    /// Finds the references for the symbol at the specific position in the document,
    /// pushing the results into the context instance.
    /// </summary>
    Task FindReferencesAsync(Document document, int position, IFSharpFindUsagesContext context);

    /// <summary>
    /// Finds the implementations for the symbol at the specific position in the document,
    /// pushing the results into the context instance.
    /// </summary>
    Task FindImplementationsAsync(Document document, int position, IFSharpFindUsagesContext context);
}
