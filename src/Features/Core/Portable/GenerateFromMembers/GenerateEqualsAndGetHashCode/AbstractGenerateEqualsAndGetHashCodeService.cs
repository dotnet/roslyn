// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateEqualsAndGetHashCode
{
    internal abstract partial class AbstractGenerateEqualsAndGetHashCodeService<TService, TMemberDeclarationSyntax> :
        AbstractGenerateFromMembersService<TMemberDeclarationSyntax>, IGenerateEqualsAndGetHashCodeService
        where TService : AbstractGenerateEqualsAndGetHashCodeService<TService, TMemberDeclarationSyntax>
        where TMemberDeclarationSyntax : SyntaxNode
    {
        private const string EqualsName = "Equals";
        private const string GetHashCodeName = "GetHashCode";
        private const string ObjName = "ObjName";

        protected AbstractGenerateEqualsAndGetHashCodeService()
        {
        }

        public async Task<IGenerateEqualsAndGetHashCodeResult> GenerateEqualsAndGetHashCodeAsync(
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
                            var codeRefactoring = CreateCodeRefactoring(
                                info.SelectedDeclarations,
                                CreateActions(document, textSpan, info.ContainingType, info.SelectedMembers, hasEquals, hasGetHashCode));

                            return new GenerateEqualsAndGetHashCodeResult(codeRefactoring);
                        }
                    }
                }

                return GenerateEqualsAndGetHashCodeResult.Failure;
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
                yield return new GenerateEqualsAndHashCodeAction((TService)this, document, textSpan, containingType, selectedMembers, generateEquals: true);
            }

            if (!hasGetHashCode)
            {
                yield return new GenerateEqualsAndHashCodeAction((TService)this, document, textSpan, containingType, selectedMembers, generateGetHashCode: true);
            }

            if (!hasEquals && !hasGetHashCode)
            {
                yield return new GenerateEqualsAndHashCodeAction((TService)this, document, textSpan, containingType, selectedMembers, generateEquals: true, generateGetHashCode: true);
            }
        }
    }
}
