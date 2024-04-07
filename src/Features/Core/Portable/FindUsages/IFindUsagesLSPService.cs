// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.FindUsages;

internal interface IFindUsagesLSPService : ILanguageService
{
    /// <summary>
    /// Finds the references for the symbol at the specific position in the document,
    /// pushing the results into the context instance.
    /// </summary>
    Task FindReferencesAsync(IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the implementations for the symbol at the specific position in the document,
    /// pushing the results into the context instance.
    /// </summary>
    Task FindImplementationsAsync(IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken);
}
