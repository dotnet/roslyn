// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EncapsulateField;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
    Name = PredefinedCodeRefactoringProviderNames.EncapsulateField), Shared]
internal sealed class EncapsulateFieldRefactoringProvider : CodeRefactoringProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public EncapsulateFieldRefactoringProvider()
    {
    }

    public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        var service = document.GetRequiredLanguageService<AbstractEncapsulateFieldService>();

        var actions = await service.GetEncapsulateFieldCodeActionsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        context.RegisterRefactorings(actions);
    }
}
