// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.IntroduceVariable;

[ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct)]
[ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass)]
[ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertConditional)]
[ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertLogical)]
[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.IntroduceVariable), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class IntroduceVariableCodeRefactoringProvider() : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
            return;

        var service = document.GetRequiredLanguageService<IIntroduceVariableService>();
        var cleanupOptions = await document.GetCodeCleanupOptionsAsync(context.Options, context.CancellationToken).ConfigureAwait(false);
        var action = await service.IntroduceVariableAsync(document, textSpan, cleanupOptions, cancellationToken).ConfigureAwait(false);
        if (action != null)
            context.RegisterRefactoring(action, textSpan);
    }
}
