// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    // [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, 
    //      Name = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await this.GenerateEqualsAndGetHashCodeFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }

        private const string EqualsName = "Equals";
        private const string GetHashCodeName = "GetHashCode";
        private const string ObjName = nameof(ObjName);

        public async Task<ImmutableArray<CodeAction>> GenerateEqualsAndGetHashCodeFromMembersAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_GenerateEqualsAndGetHashCode, cancellationToken))
            {
                var info = await this.GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (info != null &&
                    info.SelectedMembers.All(IsInstanceFieldOrProperty))
                {
                    if (info.ContainingType != null && info.ContainingType.TypeKind != TypeKind.Interface)
                    {
                        var hasEquals =
                            info.ContainingType.GetMembers(EqualsName)
                                               .OfType<IMethodSymbol>()
                                               .Any(m => m.Parameters.Length == 1 && !m.IsStatic);

                        var hasGetHashCode =
                            info.ContainingType.GetMembers(GetHashCodeName)
                                               .OfType<IMethodSymbol>()
                                               .Any(m => m.Parameters.Length == 0 && !m.IsStatic);

                        if (!hasEquals || !hasGetHashCode)
                        {
                            return CreateActions(
                                document, textSpan, info.ContainingType, info.SelectedMembers, hasEquals, hasGetHashCode).AsImmutableOrNull();
                        }
                    }
                }

                return default(ImmutableArray<CodeAction>);
            }
        }

        private IEnumerable<CodeAction> CreateActions(
            Document document,
            TextSpan textSpan,
            INamedTypeSymbol containingType,
            IList<ISymbol> selectedMembers,
            bool hasEquals,
            bool hasGetHashCode)
        {
            if (!hasEquals)
            {
                yield return new GenerateEqualsAndHashCodeAction(this, document, textSpan, containingType, selectedMembers, generateEquals: true);
            }

            if (!hasGetHashCode)
            {
                yield return new GenerateEqualsAndHashCodeAction(this, document, textSpan, containingType, selectedMembers, generateGetHashCode: true);
            }

            if (!hasEquals && !hasGetHashCode)
            {
                yield return new GenerateEqualsAndHashCodeAction(this, document, textSpan, containingType, selectedMembers, generateEquals: true, generateGetHashCode: true);
            }
        }
    }
}