// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExtractClass
{
    internal interface IExtractClassOptionsService : IWorkspaceService
    {
        Task<ExtractClassOptions?> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol originalType, ISymbol? selectedMember);
    }
}
