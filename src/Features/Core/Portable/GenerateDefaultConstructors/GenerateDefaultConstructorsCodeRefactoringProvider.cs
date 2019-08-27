// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors
{
    /// <summary>
    /// This <see cref="CodeRefactoringProvider"/> gives users a way to generate constructors for
    /// a derived type that delegate to a base type.  For all accessibly constructors in the base
    /// type, the user will be offered to create a constructor in the derived type with the same
    /// signature if they don't already have one.  This way, a user can override a type and easily
    /// create all the forwarding constructors.
    /// 
    /// Importantly, this type is not responsible for generating constructors when the user types
    /// something like "new MyType(x, y, z)", nor is it responsible for generating constructors
    /// for a type based on the fields/properties of that type. Both of those are handled by other 
    /// services.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.GenerateDefaultConstructors), Shared]
    internal class GenerateDefaultConstructorsCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        public GenerateDefaultConstructorsCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;

            // TODO: https://github.com/dotnet/roslyn/issues/5778
            // Not supported in REPL for now.
            if (document.Project.IsSubmission)
            {
                return;
            }

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var service = document.GetLanguageService<IGenerateDefaultConstructorsService>();
            var actions = await service.GenerateDefaultConstructorsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
