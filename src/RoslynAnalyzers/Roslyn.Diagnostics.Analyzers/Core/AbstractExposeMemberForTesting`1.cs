// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class AbstractExposeMemberForTesting<TTypeDeclarationSyntax> : CodeRefactoringProvider
        where TTypeDeclarationSyntax : SyntaxNode
    {
        protected AbstractExposeMemberForTesting()
        {
        }

        private protected abstract IRefactoringHelpers RefactoringHelpers { get; }

        protected abstract bool HasRefReturns { get; }

        protected abstract SyntaxNode GetTypeDeclarationForNode(SyntaxNode reportedNode);

        protected abstract SyntaxNode GetByRefType(SyntaxNode type, RefKind refKind);

        protected abstract SyntaxNode GetByRefExpression(SyntaxNode expression);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var type = await GetRelevantTypeFromHeaderAsync(context).ConfigureAwait(false);
            if (type is null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var testAccessorType = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(type, context.CancellationToken);
            if (!IsClassOrStruct(testAccessorType))
                return;

            if (testAccessorType.Name != TestAccessorHelper.TestAccessorTypeName)
                return;

            var location = testAccessorType.Locations.FirstOrDefault(location => location.IsInSource && Equals(location.SourceTree, semanticModel.SyntaxTree));
            if (location is null)
                return;

            if (testAccessorType.ContainingSymbol is not ITypeSymbol containingType)
                return;

            foreach (var member in containingType.GetMembers())
            {
                var memberName = member.Name;
                if (testAccessorType.GetMembers(GetTestAccessorName(member)).Any())
                {
                    continue;
                }

                switch (member)
                {
                    case IFieldSymbol:
                    case IPropertySymbol:
                        context.RegisterRefactoring(
                            CodeAction.Create(
                                memberName,
                                cancellationToken => AddMemberToTestAccessorAsync(context.Document, location.SourceSpan, memberName, member.GetDocumentationCommentId(), cancellationToken),
                                member.GetDocumentationCommentId()));
                        break;

                    default:
                        break;
                }
            }
        }

        private async Task<TTypeDeclarationSyntax?> GetRelevantTypeFromHeaderAsync(CodeRefactoringContext context)
        {
            var type = await context.TryGetRelevantNodeAsync<TTypeDeclarationSyntax>(RefactoringHelpers).ConfigureAwait(false);
            if (type is null)
                return null;

            return type;
        }

        private static bool IsClassOrStruct(ITypeSymbol typeSymbol)
            => typeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct;

        private static string GetTestAccessorName(ISymbol symbol)
        {
            var name = symbol.Name.TrimStart('_');
            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        private async Task<Solution> AddMemberToTestAccessorAsync(Document document, TextSpan sourceSpan, string memberName, string memberDocumentationCommentId, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var reportedNode = syntaxRoot.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var testAccessorTypeDeclaration = GetTypeDeclarationForNode(reportedNode);
            var testAccessorType = (ITypeSymbol)semanticModel.GetDeclaredSymbol(testAccessorTypeDeclaration, cancellationToken);
            var containingType = (ITypeSymbol)testAccessorType.ContainingSymbol;
            var member = containingType.GetMembers(memberName).First(m => m.GetDocumentationCommentId() == memberDocumentationCommentId);

            var accessorField = testAccessorType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(field => field.Type.Equals(containingType));
            if (accessorField is null)
            {
                return document.Project.Solution;
            }

            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            SyntaxNode newMember;
            switch (member)
            {
                case IFieldSymbol fieldSymbol:
                    newMember = GenerateTestAccessorForField(fieldSymbol, memberName, syntaxGenerator, accessorField);
                    break;

                case IPropertySymbol propertySymbol:
                    SyntaxNode? getAccessor = null;
                    SyntaxNode? setAccessor = null;
                    if (!propertySymbol.IsWriteOnly)
                    {
                        getAccessor = syntaxGenerator.ReturnStatement(syntaxGenerator.MemberAccessExpression(syntaxGenerator.IdentifierName(accessorField.Name), syntaxGenerator.IdentifierName(memberName)));
                    }

                    if (!propertySymbol.IsReadOnly)
                    {
                        setAccessor = syntaxGenerator.AssignmentStatement(syntaxGenerator.MemberAccessExpression(syntaxGenerator.IdentifierName(accessorField.Name), syntaxGenerator.IdentifierName(memberName)), syntaxGenerator.IdentifierName("value"));
                    }

                    DeclarationModifiers modifiers;
                    if (propertySymbol.IsWriteOnly)
                    {
                        modifiers = DeclarationModifiers.WriteOnly;
                    }
                    else if (propertySymbol.IsReadOnly)
                    {
                        modifiers = DeclarationModifiers.ReadOnly;
                    }
                    else
                    {
                        modifiers = DeclarationModifiers.None;
                    }

                    newMember = syntaxGenerator.PropertyDeclaration(
                        GetTestAccessorName(member),
                        syntaxGenerator.TypeExpression(propertySymbol.Type),
                        Accessibility.Internal,
                        modifiers,
                        getAccessorStatements: getAccessor != null ? new[] { getAccessor } : null,
                        setAccessorStatements: setAccessor != null ? new[] { setAccessor } : null);
                    break;

                default:
                    return document.Project.Solution;
            }

            var newTypeDeclaration = syntaxGenerator.AddMembers(testAccessorTypeDeclaration, newMember);
            return document.WithSyntaxRoot(syntaxRoot.ReplaceNode(testAccessorTypeDeclaration, newTypeDeclaration)).Project.Solution;
        }

        private SyntaxNode GenerateTestAccessorForField(IFieldSymbol fieldSymbol, string memberName, SyntaxGenerator syntaxGenerator, IFieldSymbol accessorField)
        {
            var getAccessor = syntaxGenerator.ReturnStatement(GetByRefExpression(syntaxGenerator.MemberAccessExpression(syntaxGenerator.IdentifierName(accessorField.Name), syntaxGenerator.IdentifierName(memberName))));
            SyntaxNode? setAccessor = null;
            if (!fieldSymbol.IsReadOnly && !HasRefReturns)
            {
                setAccessor = syntaxGenerator.AssignmentStatement(syntaxGenerator.MemberAccessExpression(syntaxGenerator.IdentifierName(accessorField.Name), syntaxGenerator.IdentifierName(memberName)), syntaxGenerator.IdentifierName("value"));
            }

            DeclarationModifiers modifiers;
            if (setAccessor is null)
            {
                modifiers = DeclarationModifiers.ReadOnly;
            }
            else
            {
                modifiers = DeclarationModifiers.None;
            }

            return syntaxGenerator.PropertyDeclaration(
                GetTestAccessorName(fieldSymbol),
                GetByRefType(syntaxGenerator.TypeExpression(fieldSymbol.Type), fieldSymbol.IsReadOnly ? RefKind.RefReadOnly : RefKind.Ref),
                Accessibility.Internal,
                modifiers,
                getAccessorStatements: new[] { getAccessor },
                setAccessorStatements: setAccessor != null ? new[] { setAccessor } : null);
        }
    }
}
