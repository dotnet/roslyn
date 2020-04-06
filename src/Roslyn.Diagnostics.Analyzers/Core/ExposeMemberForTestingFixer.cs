// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class ExposeMemberForTestingFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RoslynDiagnosticIds.ExposeMemberForTestingRuleId);

        public sealed override FixAllProvider? GetFixAllProvider()
        {
            // This is a refactoring for one-off test accessor creation. Batch fixing is disabled.
            return null;
        }

        protected abstract bool HasRefReturns { get; }

        protected abstract SyntaxNode GetTypeDeclarationForNode(SyntaxNode reportedNode);

        protected abstract SyntaxNode GetByRefType(SyntaxNode type, RefKind refKind);

        protected abstract SyntaxNode GetByRefExpression(SyntaxNode expression);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var syntaxTree = diagnostic.Location.SourceTree;
                var syntaxRoot = await syntaxTree.GetRootAsync(context.CancellationToken).ConfigureAwait(false);
                var reportedNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var testAccessorTypeDeclaration = GetTypeDeclarationForNode(reportedNode);
                var testAccessorType = (ITypeSymbol)semanticModel.GetDeclaredSymbol(testAccessorTypeDeclaration, context.CancellationToken);
                var containingType = testAccessorType.ContainingSymbol as ITypeSymbol;
                if (containingType is null)
                {
                    continue;
                }

                foreach (var member in containingType.GetMembers())
                {
                    var memberName = member.Name;
                    if (testAccessorType.GetMembers(GetTestAccessorName(member)).Any())
                    {
                        continue;
                    }

                    switch (member)
                    {
                        case IFieldSymbol _:
                        case IPropertySymbol _:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    memberName,
                                    cancellationToken => AddMemberToTestAccessorAsync(context.Document, diagnostic, memberName, member.GetDocumentationCommentId(), cancellationToken),
                                    member.GetDocumentationCommentId()),
                                diagnostic);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private static string GetTestAccessorName(ISymbol symbol)
        {
            var name = symbol.Name.TrimStart('_');
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private async Task<Solution> AddMemberToTestAccessorAsync(Document document, Diagnostic diagnostic, string memberName, string memberDocumentationCommentId, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxTree = diagnostic.Location.SourceTree;
            var syntaxRoot = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var reportedNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
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
