// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.FindUsages;

[Shared]
[ExportLanguageService(typeof(IFindUsagesService), LanguageNames.FSharp)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FSharpFindUsagesService(IFSharpFindUsagesService service) : IFindUsagesService
{
    public Task FindImplementationsAsync(IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
        => service.FindImplementationsAsync(document, position, new FSharpFindUsagesContext(context, cancellationToken));

    public Task FindReferencesAsync(IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
        => service.FindReferencesAsync(document, position, new FSharpFindUsagesContext(context, cancellationToken));
}
