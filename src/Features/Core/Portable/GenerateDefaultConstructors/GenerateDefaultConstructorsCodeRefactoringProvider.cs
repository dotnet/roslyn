// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors
{
    /// <summary>
    /// This <see cref="CodeRefactoringProvider"/> gives users a way to generate constructors for
    /// a derived type that delegate to a base type.  For all accessible constructors in the base
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public GenerateDefaultConstructorsCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;

            // TODO: https://github.com/dotnet/roslyn/issues/5778
            // Not supported in REPL for now.
            if (document.Project.IsSubmission)
                return;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                return;

            var service = document.GetRequiredLanguageService<IGenerateDefaultConstructorsService>();
            var actions = await service.GenerateDefaultConstructorsAsync(
                document, textSpan, context.Options, forRefactoring: true, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
