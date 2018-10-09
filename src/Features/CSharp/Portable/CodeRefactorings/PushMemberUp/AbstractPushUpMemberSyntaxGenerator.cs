using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal abstract class AbstractPushUpMemberSyntaxGenerator
    {
        internal virtual MemberDeclarationSyntax CreateMemberSyntax(
            MemberDeclarationSyntax memberDeclarationSyntax,
            VariableDeclaratorSyntax variableDeclaratorSyntax = null)
        {
            MemberDeclarationSyntax memberToPush = default;
            switch (memberDeclarationSyntax)
            {
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    memberToPush = CreateMethodSyntax(methodDeclarationSyntax);
                    break;
                case EventFieldDeclarationSyntax eventFieldDeclarationSyntax:
                    memberToPush = CreateEventSyntax(eventFieldDeclarationSyntax, variableDeclaratorSyntax);
                    break;
                case PropertyDeclarationSyntax propertyFieldDeclarationSyntax:
                    memberToPush = CreatePropertySyntax(propertyFieldDeclarationSyntax);
                    break;
                case IndexerDeclarationSyntax indexerDeclarationSyntax:
                    memberToPush = CreateIndexerSyntax(indexerDeclarationSyntax);
                    break;
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    memberToPush = CreateFieldSyntax(fieldDeclarationSyntax, variableDeclaratorSyntax);
                    break;
                default:
                    throw new ArgumentException($"{nameof(memberDeclarationSyntax)}'s type should be method, event, property, indexer or Field");
            }
            SyntaxAnnotation[] formatter = { Formatter.Annotation };
            return memberToPush.WithAdditionalAnnotations(formatter);
        }


        internal virtual MemberDeclarationSyntax CreateMemberSyntaxWithFix(
            MemberDeclarationSyntax memberDeclarationSyntax,
            VariableDeclaratorSyntax variableDeclaratorSyntax = null)
        {
            MemberDeclarationSyntax memberToPush = default;
            switch (memberDeclarationSyntax)
            {
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    memberToPush = CreateMethodSyntaxWithFix(methodDeclarationSyntax);
                    break;
                case EventFieldDeclarationSyntax eventFieldDeclarationSyntax:
                    memberToPush = CreateEventSyntaxWithFix(eventFieldDeclarationSyntax, variableDeclaratorSyntax);
                    break;
                case PropertyDeclarationSyntax propertyFieldDeclarationSyntax:
                    memberToPush = CreatePropertySyntaxWithFix(propertyFieldDeclarationSyntax);
                    break;
                case IndexerDeclarationSyntax indexerDeclarationSyntax:
                    memberToPush = CreateIndexerSyntaxWithFix(indexerDeclarationSyntax);
                    break;
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    memberToPush = CreateFieldSyntaxWithFix(fieldDeclarationSyntax, variableDeclaratorSyntax);
                    break;
                default:
                    throw new ArgumentException($"{nameof(memberDeclarationSyntax)}'s type should be method, event, property, indexer or Field");
            }

            SyntaxAnnotation[] formatter = { Formatter.Annotation };
            return memberToPush.WithAdditionalAnnotations(formatter);
        }

        protected virtual EventFieldDeclarationSyntax CreateEventSyntax(
            EventFieldDeclarationSyntax eventFieldDeclarationSyntax,
            VariableDeclaratorSyntax variableDeclaratorNode)
        {
            var identifierSyntax = variableDeclaratorNode.Parent.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            var declarationList = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            declarationList = declarationList.Add(variableDeclaratorNode);
            var variableDeclaration = SyntaxFactory.VariableDeclaration(identifierSyntax, declarationList);
            return SyntaxFactory.EventFieldDeclaration(variableDeclaration);
        }

        protected virtual FieldDeclarationSyntax CreateFieldSyntax(
            FieldDeclarationSyntax fieldDeclarationSyntax,
            VariableDeclaratorSyntax variableDeclaratorNode)
        {
            var preTypeSyntax = variableDeclaratorNode.Parent.DescendantNodes().OfType<PredefinedTypeSyntax>().First();
            var declarationList = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            declarationList = declarationList.Add(variableDeclaratorNode);
            var variableDeclaration = SyntaxFactory.VariableDeclaration(preTypeSyntax, declarationList);
            return SyntaxFactory.FieldDeclaration(fieldDeclarationSyntax.AttributeLists, fieldDeclarationSyntax.Modifiers, variableDeclaration);
        }
        internal abstract Task<Solution> CreateChangedSolution(IEnumerable<MemberDeclarationSyntax> memberToPushUp, SyntaxNode targetSyntaxNode, Document contextDocument, CancellationToken cancellationToken);

        internal abstract Task<Document> CreateChangedDocument(MemberDeclarationSyntax memberToPushUp, SyntaxNode targetSyntaxNode, Document contextDocument, CancellationToken cancellation);

        protected abstract MethodDeclarationSyntax CreateMethodSyntax(MethodDeclarationSyntax methodDeclarationSyntax);

        protected abstract PropertyDeclarationSyntax CreatePropertySyntax(PropertyDeclarationSyntax propertyFieldDeclarationSyntax);

        protected abstract IndexerDeclarationSyntax CreateIndexerSyntax(IndexerDeclarationSyntax indexerDeclarationSyntax);

        protected abstract IndexerDeclarationSyntax CreateIndexerSyntaxWithFix(IndexerDeclarationSyntax indexerDeclarationSyntax);

        protected abstract PropertyDeclarationSyntax CreatePropertySyntaxWithFix(PropertyDeclarationSyntax propertyFieldDeclarationSyntax);

        protected abstract MethodDeclarationSyntax CreateMethodSyntaxWithFix(MethodDeclarationSyntax methodDeclarationSyntax);

        protected abstract FieldDeclarationSyntax CreateFieldSyntaxWithFix(FieldDeclarationSyntax fieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode);

        protected abstract EventFieldDeclarationSyntax CreateEventSyntaxWithFix(EventFieldDeclarationSyntax eventFieldDeclarationSyntax, VariableDeclaratorSyntax variableDeclaratorNode);
    }
}
