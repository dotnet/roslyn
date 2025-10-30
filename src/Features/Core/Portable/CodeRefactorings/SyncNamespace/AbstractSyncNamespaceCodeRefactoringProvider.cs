// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;

internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax>
    : CodeRefactoringProvider
    where TNamespaceDeclarationSyntax : SyntaxNode
    where TCompilationUnitSyntax : SyntaxNode
    where TMemberDeclarationSyntax : SyntaxNode
{
    /// <summary>
    /// Try to get the node that can be used to trigger the refactoring based on current cursor position. 
    /// </summary>
    /// <returns>
    /// (1) a node of type <typeparamref name="TNamespaceDeclarationSyntax"/> node, if cursor in the name and it's the 
    /// only namespace declaration in the document.
    /// (2) a node of type <typeparamref name="TCompilationUnitSyntax"/> node, if the cursor is in the name of first 
    /// declaration in global namespace and there's no namespace declaration in this document.
    /// (3) otherwise, null.
    /// </returns>
    protected abstract Task<SyntaxNode?> TryGetApplicableInvocationNodeAsync(Document document, TextSpan span, CancellationToken cancellationToken);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles ||
            document.IsGeneratedCode(cancellationToken))
        {
            return;
        }

        var state = await State.CreateAsync(this, document, textSpan, cancellationToken).ConfigureAwait(false);
        if (state == null)
            return;

        // No move file action if rootnamespace isn't a prefix of current declared namespace
        if (state.RelativeDeclaredNamespace != null)
        {
            // These code actions try to move file to a new location based on declared namespace
            // and the default namespace of the project. The new location is a list of folders
            // determined by the relative part of the declared namespace compare to the default namespace.
            // 
            // For example, if he default namespace is `A.B.C`, file path is 
            // "[project root dir]\Class1.cs" and declared namespace in the file is
            // `A.B.C.D.E`, then this action will move the file to [project root dir]\D\E\Class1.cs". .
            // 
            // We also try to use existing folders as target if possible, using the same example above,
            // if folder "[project root dir]\D.E\" already exist, we will also offer to move file to 
            // "[project root dir]\D.E\Class1.cs".
            context.RegisterRefactorings(MoveFileCodeAction.Create(state));
        }

        // No change namespace action if we can't construct a valid namespace from rootnamespace and folder names.
        if (state.TargetNamespace != null)
        {
            // This code action tries to change the name of the namespace declaration to 
            // match the folder hierarchy of the document. The new namespace is constructed 
            // by concatenating the default namespace of the project and all the folders in 
            // the file path up to the project root.
            // 
            // For example, if he default namespace is `A.B.C`, file path is 
            // "[project root dir]\D\E\F\Class1.cs" and declared namespace in the file is
            // `Foo.Bar.Baz`, then this action will change the namespace declaration
            // to `A.B.C.D.E.F`. 
            // 
            // Note that it also handles the case where the target namespace or declared namespace 
            // is global namespace, i.e. default namespace is "" and the file is located at project 
            // root directory, and no namespace declaration in the document, respectively.

            var service = document.GetRequiredLanguageService<IChangeNamespaceService>();

            var title = state.TargetNamespace.Length == 0
                ? FeaturesResources.Change_to_global_namespace
                : string.Format(FeaturesResources.Change_namespace_to_0, state.TargetNamespace);
            var solutionChangeAction = CodeAction.Create(
                title,
                token => service.ChangeNamespaceAsync(document, state.Container, state.TargetNamespace, token),
                title);

            context.RegisterRefactoring(solutionChangeAction, textSpan);
        }
    }
}
