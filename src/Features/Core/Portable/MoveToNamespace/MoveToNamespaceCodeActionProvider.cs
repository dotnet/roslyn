﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveToNamespace), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.SyncNamespace)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.MoveTypeToFile)]
    internal class MoveToNamespaceCodeActionProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public MoveToNamespaceCodeActionProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var moveToNamespaceService = document.GetLanguageService<IMoveToNamespaceService>();
            var actions = await moveToNamespaceService.GetCodeActionsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
