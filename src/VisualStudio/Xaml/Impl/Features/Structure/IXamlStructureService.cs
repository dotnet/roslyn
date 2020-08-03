// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Structure
{
    internal interface IXamlStructureService : ILanguageService
    {
        Task<ImmutableArray<IXamlStructureTag>> GetStructureTagsAsync(TextDocument document, CancellationToken cancellationToken);
    }
}
