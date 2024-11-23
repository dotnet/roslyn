// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.LanguageService.TypeInferenceService;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageService(typeof(ITypeInferenceService), LanguageNames.CSharp), Shared]
internal partial class CSharpTypeInferenceService : AbstractTypeInferenceService
{
    public static readonly CSharpTypeInferenceService Instance = new();

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")]
    public CSharpTypeInferenceService()
    {
    }

    protected override AbstractTypeInferrer CreateTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken)
        => new TypeInferrer(semanticModel, cancellationToken);
}
