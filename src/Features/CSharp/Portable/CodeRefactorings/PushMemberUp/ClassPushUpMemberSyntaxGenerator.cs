using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class ClassPushUpMemberSyntaxGenerator : AbstractPushUpMemberSyntaxGenerator
    {
       protected override EventFieldDeclarationSyntax CreateEventSyntax(
           EventFieldDeclarationSyntax eventFieldDeclarationSyntax,
           VariableDeclaratorSyntax variableDeclaratorNode)
        {
            var eventNodeWithoutModifier = base.CreateEventSyntax(eventFieldDeclarationSyntax, variableDeclaratorNode);
            return eventNodeWithoutModifier.WithModifiers(eventFieldDeclarationSyntax.Modifiers);
        }

        protected override IndexerDeclarationSyntax CreateIndexerSyntax(IndexerDeclarationSyntax indexerDeclarationSyntax)
        {
            return indexerDeclarationSyntax;
        }

        protected override MethodDeclarationSyntax CreateMethodSyntax(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax;
        }

        protected override PropertyDeclarationSyntax CreatePropertySyntax(PropertyDeclarationSyntax propertyFieldDeclarationSyntax)
        {
            return propertyFieldDeclarationSyntax;
        }

        protected override PropertyDeclarationSyntax CreatePropertySyntaxWithFix(PropertyDeclarationSyntax propertyFieldDeclarationSyntax)
        {
            throw new NotImplementedException();
        }

        protected override IndexerDeclarationSyntax CreateIndexerSyntaxWithFix(IndexerDeclarationSyntax indexerDeclarationSyntax)
        {
            throw new NotImplementedException();
        }

        internal override Task<Solution> CreateChangedSolution(IEnumerable<MemberDeclarationSyntax> memberToPushUp, SyntaxNode targetSyntaxNode, Document contextDocument, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override Task<Document> CreateChangedDocument(MemberDeclarationSyntax memberToPushUp, SyntaxNode targetSyntaxNode, Document contextDocument, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        protected override MethodDeclarationSyntax CreateMethodSyntaxWithFix(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            throw new NotImplementedException();
        }

        protected override FieldDeclarationSyntax CreateFieldSyntaxWithFix(FieldDeclarationSyntax fieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode)
        {
            throw new NotImplementedException();
        }

        protected override EventFieldDeclarationSyntax CreateEventSyntaxWithFix(EventFieldDeclarationSyntax eventFieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode)
        {
            throw new NotImplementedException();
        }
    }
}
