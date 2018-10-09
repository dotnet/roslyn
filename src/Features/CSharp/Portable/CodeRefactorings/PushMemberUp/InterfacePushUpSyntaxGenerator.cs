using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class InterfacePushUpSyntaxGenerator : AbstractPushUpMemberSyntaxGenerator
    {
        private SemanticModel SemanticModel { get; }

        internal InterfacePushUpSyntaxGenerator(SemanticModel semanticModel)
        {
            SemanticModel = semanticModel;
        }

        internal override async Task<Solution> CreateChangedSolution(
            IEnumerable<MemberDeclarationSyntax> memberToPushUp,
            SyntaxNode targetSyntaxNode,
            Document contextDocument,
            CancellationToken cancellationToken)
        {
            var solution = contextDocument.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var targetDocumentEditor = await solutionEditor.
                GetDocumentEditorAsync(solution.GetDocument(targetSyntaxNode.SyntaxTree).Id, cancellationToken).ConfigureAwait(false);

            foreach (var member in memberToPushUp)
            {
                targetDocumentEditor.AddMember(targetSyntaxNode, member);
            }
            return solutionEditor.GetChangedSolution();
        }       

        internal override async Task<Document> CreateChangedDocument(
            MemberDeclarationSyntax memberToPushUp,
            SyntaxNode targetSyntaxNode, Document contextDocument,
            CancellationToken cancellation)
        {
           var documentEditor = await DocumentEditor.CreateAsync(contextDocument, cancellation).ConfigureAwait(false);
           documentEditor.AddMember(targetSyntaxNode, memberToPushUp);
           return documentEditor.GetChangedDocument();
        }

        protected override MethodDeclarationSyntax CreateMethodSyntax(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return SyntaxFactory.MethodDeclaration(
                        methodDeclarationSyntax.AttributeLists,
                        default,
                        methodDeclarationSyntax.ReturnType,
                        default,
                        methodDeclarationSyntax.Identifier,
                        methodDeclarationSyntax.TypeParameterList,
                        methodDeclarationSyntax.ParameterList,
                        methodDeclarationSyntax.ConstraintClauses,
                        default,
                        default,
                        SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        protected override IndexerDeclarationSyntax CreateIndexerSyntaxWithFix(IndexerDeclarationSyntax indexerDeclarationSyntax)
        {
            var propertySymbol = SemanticModel.GetDeclaredSymbol(indexerDeclarationSyntax);
            var accessorList = CreateAccessors(propertySymbol);

            return SyntaxFactory.IndexerDeclaration(
                indexerDeclarationSyntax.AttributeLists,
                default,
                indexerDeclarationSyntax.Type,
                default,
                indexerDeclarationSyntax.ParameterList,
                accessorList);
        }

        protected override PropertyDeclarationSyntax CreatePropertySyntaxWithFix(
            PropertyDeclarationSyntax propertyFieldDeclarationSyntax)
        {
            var propertySymbol = SemanticModel.GetDeclaredSymbol(propertyFieldDeclarationSyntax);
            var accessorList = CreateAccessors(propertySymbol);

            return SyntaxFactory.PropertyDeclaration(
                propertyFieldDeclarationSyntax.AttributeLists,
                default,
                propertyFieldDeclarationSyntax.Type,
                default,
                propertyFieldDeclarationSyntax.Identifier,
                accessorList);
        }

        protected override IndexerDeclarationSyntax CreateIndexerSyntax(IndexerDeclarationSyntax indexerDeclarationSyntax)
        {
            var propertySymbol = SemanticModel.GetDeclaredSymbol(indexerDeclarationSyntax);
            
            var accessorList = CreateAccessorsAndFilterNonPublicAccessor(propertySymbol);

            return SyntaxFactory.IndexerDeclaration(
                indexerDeclarationSyntax.AttributeLists,
                default,
                indexerDeclarationSyntax.Type,
                default,
                indexerDeclarationSyntax.ParameterList,
                accessorList);
        }

        protected override PropertyDeclarationSyntax CreatePropertySyntax(PropertyDeclarationSyntax propertyFieldDeclarationSyntax)
        {
            var propertySymbol = SemanticModel.GetDeclaredSymbol(propertyFieldDeclarationSyntax);

            var accessorList = CreateAccessorsAndFilterNonPublicAccessor(propertySymbol);
               
            return SyntaxFactory.PropertyDeclaration(
                propertyFieldDeclarationSyntax.AttributeLists,
                default,
                propertyFieldDeclarationSyntax.Type,
                default,
                propertyFieldDeclarationSyntax.Identifier,
                accessorList);
        }

        private AccessorListSyntax CreateAccessorsAndFilterNonPublicAccessor(IPropertySymbol propertySymbol)
        {
            var accessorList = SyntaxFactory.AccessorList();
            var syntaxList = new SyntaxList<AccessorDeclarationSyntax>();
            if (propertySymbol.GetMethod != null &&
                propertySymbol.GetMethod.DeclaredAccessibility == Accessibility.Public)
            {
               syntaxList = syntaxList.Add(CreateGetterAccessor());
            }
            if (propertySymbol.SetMethod != null &&
                propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Public)
            {
                syntaxList = syntaxList.Add(CreateSetterAccessor());
            }
            return accessorList.WithAccessors(syntaxList);
        }

        private AccessorListSyntax CreateAccessors(IPropertySymbol propertySymbol)
        {
            var accessorList = SyntaxFactory.AccessorList();
            var syntaxList = new SyntaxList<AccessorDeclarationSyntax>();

            if (propertySymbol.GetMethod != null)
            {
                syntaxList = syntaxList.Add(CreateGetterAccessor());
            }
            if (propertySymbol.SetMethod != null)
            {
                syntaxList = syntaxList.Add(CreateSetterAccessor());
            }
            return accessorList.WithAccessors(syntaxList);
        }
        
        private AccessorDeclarationSyntax CreateGetterAccessor()
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).
                WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private AccessorDeclarationSyntax CreateSetterAccessor()
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).
                WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        protected override MethodDeclarationSyntax CreateMethodSyntaxWithFix(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return CreateMethodSyntax(methodDeclarationSyntax);
        }

        protected override FieldDeclarationSyntax CreateFieldSyntaxWithFix(FieldDeclarationSyntax fieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode)
        {
            return CreateFieldSyntax(fieldDeclarationSyntax, variableDeclaratorNode);
        }

        protected override EventFieldDeclarationSyntax CreateEventSyntaxWithFix(EventFieldDeclarationSyntax eventFieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode)
        {
            throw new NotImplementedException();
        }
    }
}
