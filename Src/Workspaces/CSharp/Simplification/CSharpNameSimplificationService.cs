using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.CSharp.Extensions;
using Roslyn.Services.CSharp.SemanticTransformation;
using Roslyn.Services.Shared.Collections;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Services.Simplification;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Simplification
{
    internal partial class CSharpNameSimplificationService : AbstractNameSimplificationService
    {
        public override SimplificationResult SimplifyNames(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            var semanticModel = document.GetCSharpSemanticModel(cancellationToken);
            var rewriter = new Rewriter(semanticModel, SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, spans), cancellationToken);
            var newRoot = rewriter.Visit(semanticModel.SyntaxTree.GetRoot(cancellationToken));

            return new SimplificationResult(document.WithSyntaxRoot(newRoot), rewriter.SimplifiedNodes);
        }

        private static SyntaxNode Simplify(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (!node.ContainsInterleavedDirective(cancellationToken))
            {
                if (node is NameSyntax)
                {
                    var name = (NameSyntax)node;
                    if (CanSimplify(name))
                    {
                        return Simplify(semanticModel, name);
                    }
                }
                else if (node is MemberAccessExpressionSyntax)
                {
                    return Simplify(semanticModel, (MemberAccessExpressionSyntax)node);
                }
                
                // TODO: handle crefs & param names of xml doc comments (Workitem 18036) 
            }

            // Don't know how to simplify this.
            return node;
        }

        private static bool CanSimplify(NameSyntax name)
        {
            // Can't simplify a SimpleName.  It's simplified by definition :)
            if (name is SimpleNameSyntax) 
            {
                return false;
            }

            // We can simplify Qualified names and AliasQualifiedNames. Generally, if we have 
            // something like "A.B.C.D", we only consider the full thing something we can simplify.
            // However, in the case of "A.B.C<>.D", then we'll only consider simplifying up to the 
            // first open name.  This is because if we remove the open name, we'll often change 
            // meaning as "D" will bind to C<T>.D which is different than C<>.D!
            if (name is QualifiedNameSyntax)
            {
                var left = ((QualifiedNameSyntax)name).Left;
                if (ContainsOpenName(left))
                {
                    // Don't simplify A.B<>.C
                    return false;
                }

                // We're a qualified name.  We can be simplified if there's not yet another name
                // on the right of us.  
                if (name.IsLeftSideOfQualifiedName())
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsOpenName(NameSyntax name)
        {
            if (name is QualifiedNameSyntax)
            {
                var qualifiedName = (QualifiedNameSyntax)name;
                return ContainsOpenName(qualifiedName.Left) || ContainsOpenName(qualifiedName.Right);
            }
            else if (name is GenericNameSyntax)
            {
                return ((GenericNameSyntax)name).IsUnboundGenericName;
            }
            else
            {
                return false;
            }
        }

        private static ExpressionSyntax Simplify(
            SemanticModel semanticModel,
            MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (!IsQualifiedName(memberAccessExpression))
            {
                return memberAccessExpression;
            }

            var result = Simplify(semanticModel, (ExpressionSyntax)memberAccessExpression);
            if (result != memberAccessExpression)
            {
                return result;
            }

            // We have something of the form "TypeName.memberName" or "this.memberName".  Simplify to
            // "memberName" if we can.
            if (!IsThisOrNamedType(memberAccessExpression.Expression, semanticModel))
            {
                return memberAccessExpression;
            }

            var originalSymbolInfo = semanticModel.GetSymbolInfo(memberAccessExpression);
            var originalTypeInfo = semanticModel.GetTypeInfo(memberAccessExpression);

            var symbolInfo = semanticModel.GetSpeculativeSymbolInfo(memberAccessExpression.Span.Start, memberAccessExpression.Name, SpeculativeBindingOption.BindAsExpression);
            var typeInfo = semanticModel.GetSpeculativeTypeInfo(memberAccessExpression.Span.Start, memberAccessExpression.Name, SpeculativeBindingOption.BindAsExpression);

            if (!IsValidSymbolInfo(originalSymbolInfo.Symbol) ||
                !IsValidSymbolInfo(symbolInfo.Symbol) ||
                !object.Equals(originalSymbolInfo.Symbol, symbolInfo.Symbol) ||
                !object.Equals(originalTypeInfo.Type, typeInfo.Type))
            {
                return memberAccessExpression;
            }

            var name = memberAccessExpression.Name;
            if (WillConflictWithExistingLocal(memberAccessExpression, name))
            {
                return memberAccessExpression;
            }

            var escapedIdentifierToken = name.Identifier.IsVerbatimIdentifier() 
                ? name.Identifier
                : CSharpSemanticTransformationService.TryEscapeIdentifierToken(name.Identifier).WithAdditionalAnnotations(CodeAnnotations.Simplify);

            if (name.Kind == SyntaxKind.IdentifierName)
            {
                return memberAccessExpression.CopyAnnotationsTo(
                    ((IdentifierNameSyntax)name)
                        .WithIdentifier(escapedIdentifierToken)
                            .WithLeadingTrivia(memberAccessExpression.GetLeadingTrivia()));
            }
            else
            {
                Debug.Assert(name.Kind == SyntaxKind.GenericName);

                return memberAccessExpression.CopyAnnotationsTo(
                    ((GenericNameSyntax)name)
                        .WithIdentifier(escapedIdentifierToken)
                            .WithLeadingTrivia(memberAccessExpression.GetLeadingTrivia()));
            }
        }

        private static bool IsThisOrNamedType(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression.Kind == SyntaxKind.ThisExpression)
            {
                return true;
            }

            var expressionInfo = semanticModel.GetSymbolInfo(expression);
            if (IsValidSymbolInfo(expressionInfo.Symbol))
            {
                if (expressionInfo.Symbol is INamedTypeSymbol)
                {
                    return true;
                }
            }

            return false;
        }

        private static ExpressionSyntax Simplify(
            SemanticModel semanticModel,
            ExpressionSyntax expression)
        {
            var simplifiedNode = SimplifyWorker(semanticModel, expression);

            // Special case.  if this new minimal name parses out to a predefined type, then we
            // have to make sure that we're not in a using alias.   That's the one place where the
            // language doesn't allow predefined types.  You have to use the fully qualified name
            // instead.
            var invalidTransformation1 = IsNonNameSyntaxInUsingDirective(expression, simplifiedNode);
            var invalidTransformation2 = WillConflictWithExistingLocal(expression, simplifiedNode);
            var invalidTransformation3 = IsAmbiguousCast(expression, simplifiedNode);

            if (invalidTransformation1 || invalidTransformation2 || invalidTransformation3)
            {
                return expression;
            }

            return simplifiedNode;
        }

        private static bool IsMemberAccessOffOfNullable(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            return expression.IsParentKind(SyntaxKind.MemberAccessExpression) && simplifiedNode.IsKind(SyntaxKind.NullableType);
        }

        private static bool IsAmbiguousCast(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            // Can't simplify a type name in a cast expression if it would then cause the cast to be
            // parsed differently.  For example:  (Foo::Bar)+1  is a cast.  But if that simplifies to
            // (Bar)+1  then that's an arithmetic expression.
            if (expression.IsParentKind(SyntaxKind.CastExpression))
            {
                var castExpression = (CastExpressionSyntax)expression.Parent;
                if (castExpression.Type == expression)
                {
                    var newCastExpression = castExpression.ReplaceNode(castExpression.Type, simplifiedNode);
                    var reparsedCastExpression = Syntax.ParseExpression(newCastExpression.ToString());

                    if (!reparsedCastExpression.IsKind(SyntaxKind.CastExpression))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool WillConflictWithExistingLocal(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            if (simplifiedNode.Kind == SyntaxKind.IdentifierName && !expression.IsInNamespaceOrTypeContext())
            {
                var identifierName = (IdentifierNameSyntax)simplifiedNode;
                var enclosingDeclarationSpace = FindImmediatelyEnclosingLocalVariableDeclarationSpace(expression);
                var enclosingMemberDeclaration = expression.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                if (enclosingDeclarationSpace != null && enclosingMemberDeclaration != null)
                {
                    var locals = enclosingMemberDeclaration.GetLocalDeclarationMap()[identifierName.Identifier.ValueText];
                    foreach (var token in locals)
                    {
                        if (token.GetAncestors<SyntaxNode>().Contains(enclosingDeclarationSpace))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static SyntaxNode FindImmediatelyEnclosingLocalVariableDeclarationSpace(SyntaxNode syntax)
        {
            for (var declSpace = syntax; declSpace != null; declSpace = declSpace.Parent)
            {
                switch (declSpace.Kind)
                {
                    // These are declaration-space-defining syntaxes, by the spec:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.Block:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.SwitchStatement:
                    case SyntaxKind.ForEachKeyword:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.UsingStatement:

                    // SPEC VIOLATION: We also want to stop walking out if, say, we are in a field
                    // initializer. Technically according to the wording of the spec it should be
                    // legal to use a simple name inconsistently inside a field initializer because
                    // it does not define a local variable declaration space. In practice of course
                    // we want to check for that. (As the native compiler does as well.)

                    case SyntaxKind.FieldDeclaration:
                        return declSpace;
                }
            }

            return null;
        }

        private static bool IsNonNameSyntaxInUsingDirective(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            return
                expression.IsParentKind(SyntaxKind.UsingDirective) &&
                !(simplifiedNode is NameSyntax);
        }

        private static ExpressionSyntax SimplifyWorker(
            ISemanticModel semanticModel,
            ExpressionSyntax expression)
        {
            // 1. see whether binding the name binds to a symbol/type. if not, it is ambiguous and
            //    nothing we can do here.
            var symbol = GetOriginalSymbolInfo(semanticModel, expression);
            if (symbol != null)
            {
                // treat constructor names as types
                var method = symbol as IMethodSymbol;
                if (method.IsConstructor())
                {
                    symbol = method.ContainingType;
                }

                var namedType = symbol as INamedTypeSymbol;
                if (namedType != null)
                {
                    return SimplifyNamedType(semanticModel, expression, namedType);
                }
            }

            return expression;
        }

        private static ExpressionSyntax SimplifyNamedType(
            ISemanticModel semanticModel,
            ExpressionSyntax expression,
            INamedTypeSymbol namedType)
        {
            var format = SyntaxFacts.IsAttributeName(expression)
                ? TypeNameWithoutAttributeSuffixFormat
                : TypeNameFormat;

            var simplifiedNameParts = namedType.ToMinimalDisplayParts(
                expression.GetLocation(), semanticModel, format);

            simplifiedNameParts = simplifiedNameParts.Select(p => p.MassageErrorTypeNames()).AsReadOnly();

            var expressionTokens = expression.DescendantTokens();
            if (GetNonWhitespaceParts(simplifiedNameParts).Count >= expressionTokens.Count())
            {
                // No point simplifying if it didn't decrease the token count.
                return expression;
            }

            var typeNode = Syntax.ParseTypeName(simplifiedNameParts.ToDisplayString());
            if (IsMemberAccessOffOfNullable(expression, typeNode))
            {
                return expression;
            }

            var node = expression is MemberAccessExpressionSyntax
                ? Syntax.ParseExpression(simplifiedNameParts.ToDisplayString())
                : typeNode;
            node = expression.CopyAnnotationsTo(node);
            return node.WithLeadingTrivia(expression.GetLeadingTrivia())
                       .WithTrailingTrivia(expression.GetTrailingTrivia());
        }

        private static bool IsQualifiedName(MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (memberAccessExpression.Kind == SyntaxKind.MemberAccessExpression)
            {
                if (memberAccessExpression.Expression is MemberAccessExpressionSyntax)
                {
                    return IsQualifiedName((MemberAccessExpressionSyntax)memberAccessExpression.Expression);
                }
                else if (memberAccessExpression.Expression is NameSyntax)
                {
                    return true;
                }
            }

            return false;
        }
    }
}