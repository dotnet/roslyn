// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal sealed class CSharpUseImplicitTypeHelper : CSharpTypeStyleHelper
    {
        public static readonly CSharpUseImplicitTypeHelper Instance = new CSharpUseImplicitTypeHelper();

        private CSharpUseImplicitTypeHelper()
        {
        }

        public override TypeStyleResult AnalyzeTypeName(
            TypeSyntax typeName, SemanticModel semanticModel,
            OptionSet optionSet, CancellationToken cancellationToken)
        {
            if (typeName.StripRefIfNeeded().IsVar)
            {
                return default;
            }

            if (!optionSet.GetOption(SimplificationOptions.PreferImplicitTypeInLocalDeclaration))
            {
                return default;
            }

            if (typeName.HasAnnotation(DoNotAllowVarAnnotation.Annotation))
            {
                return default;
            }

            return base.AnalyzeTypeName(
                typeName, semanticModel, optionSet, cancellationToken);
        }

        protected override bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var type = variableDeclaration.Type.StripRefIfNeeded();
            if (type.IsVar)
            {
                // If the type is already 'var' or 'ref var', this analyzer has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeVariableDeclaration(variableDeclaration, semanticModel, cancellationToken);
        }

        protected override bool ShouldAnalyzeForEachStatement(ForEachStatementSyntax forEachStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var type = forEachStatement.Type;
            if (type.IsVar || (type.Kind() == SyntaxKind.RefType && ((RefTypeSyntax)type).Type.IsVar))
            {
                // If the type is already 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeForEachStatement(forEachStatement, semanticModel, cancellationToken);
        }

        protected override bool IsStylePreferred(
            SemanticModel semanticModel, OptionSet optionSet,
            State state, CancellationToken cancellationToken)
        {
            var stylePreferences = state.TypeStylePreference;

            if (state.IsInIntrinsicTypeContext)
            {
                return stylePreferences.HasFlag(UseVarPreference.ForBuiltInTypes);
            }
            else if (state.IsTypeApparentInContext)
            {
                return stylePreferences.HasFlag(UseVarPreference.WhenTypeIsApparent);
            }
            else
            {
                return stylePreferences.HasFlag(UseVarPreference.Elsewhere);
            }
        }

        internal override bool TryAnalyzeVariableDeclaration(
            TypeSyntax typeName, SemanticModel semanticModel,
            OptionSet optionSet, CancellationToken cancellationToken)
        {
            Debug.Assert(!typeName.StripRefIfNeeded().IsVar, "'var' special case should have prevented analysis of this variable.");

            var candidateReplacementNode = SyntaxFactory.IdentifierName("var");

            // If there exists a type named var, return.
            var conflict = semanticModel.GetSpeculativeSymbolInfo(typeName.SpanStart, candidateReplacementNode, SpeculativeBindingOption.BindAsTypeOrNamespace).Symbol;
            if (conflict?.IsKind(SymbolKind.NamedType) == true)
            {
                return false;
            }

            if (typeName.Parent.IsKind(SyntaxKind.VariableDeclaration) &&
                typeName.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.UsingStatement))
            {
                var variableDeclaration = (VariableDeclarationSyntax)typeName.Parent;

                // implicitly typed variables cannot be constants.
                if (variableDeclaration is
                {
                    Parent: LocalDeclarationStatementSyntax { IsConst: true }
                }
)
                {
                    return false;
                }

                if (variableDeclaration.Variables.Count != 1)
                {
                    return false;
                }

                var variable = variableDeclaration.Variables[0];
                if (variable.Initializer == null)
                {
                    return false;
                }

                var initializer = variable.Initializer.Value;

                // Do not suggest var replacement for stackalloc span expressions.
                // This will change the bound type from a span to a pointer.
                if (!variableDeclaration.Type.IsKind(SyntaxKind.PointerType))
                {
                    var containsStackAlloc = initializer
                        .DescendantNodesAndSelf(descendIntoChildren: node => !node.IsAnyLambdaOrAnonymousMethod())
                        .Any(node => node.IsKind(SyntaxKind.StackAllocArrayCreationExpression));

                    if (containsStackAlloc)
                    {
                        return false;
                    }
                }

                if (AssignmentSupportsStylePreference(
                        variable.Identifier, typeName, initializer,
                        semanticModel, optionSet, cancellationToken))
                {
                    return true;
                }
            }
            else if (typeName.Parent is ForEachStatementSyntax foreachStatement &&
                     foreachStatement.Type == typeName)
            {
                var foreachStatementInfo = semanticModel.GetForEachStatementInfo(foreachStatement);
                if (foreachStatementInfo.ElementConversion.IsIdentity)
                {
                    return true;
                }
            }
            else if (typeName.Parent is DeclarationExpressionSyntax declarationExpression &&
                     TryAnalyzeDeclarationExpression(declarationExpression, semanticModel, optionSet, cancellationToken))
            {
                return true;
            }

            return false;
        }

        private bool TryAnalyzeDeclarationExpression(
            DeclarationExpressionSyntax declarationExpression,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            // It's not always safe to convert a decl expression like "Method(out int i)" to
            // "Method(out var i)".  Changing to 'var' may cause overload resolution errors.
            // Have to see if using 'var' means not resolving to the same type as before.
            // Note: this is fairly expensive, so we try to avoid this if we can by seeing if
            // there are multiple candidates with the original call.  If not, then we don't
            // have to do anything.
            if (declarationExpression.Parent is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList &&
                argumentList.Parent is InvocationExpressionSyntax invocationExpression)
            {
                // If there was only one member in the group, and it was non-generic itself,
                // then this change is safe to make without doing any complex analysis.
                // Multiple methods mean that switching to 'var' might remove information
                // that affects overload resolution.  And if the method is generic, then
                // switching to 'var' may mean that inference might not work properly.
                var memberGroup = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken);
                if (memberGroup.Length == 1 &&
                    memberGroup[0].GetTypeParameters().IsEmpty)
                {
                    return true;
                }
            }

            if (!semanticModel.SyntaxTree.HasCompilationUnitRoot)
            {
                return false;
            }

            // Do the expensive check.  Note: we can't use the SpeculationAnalyzer (or any
            // speculative analyzers) here.  This is due to
            // https://github.com/dotnet/roslyn/issues/20724. Specifically, all the speculative
            // helpers do not deal with  changes to code that introduces a variable (in this case,
            // the declaration expression).  The compiler sees this as an error because there are
            // now two colliding variables, which causes all sorts of errors to be reported.
            var tree = semanticModel.SyntaxTree;
            var root = tree.GetRoot(cancellationToken);
            var annotation = new SyntaxAnnotation();

            var declarationTypeNode = declarationExpression.Type;
            var declarationType = semanticModel.GetTypeInfo(declarationTypeNode, cancellationToken).Type;

            var newRoot = root.ReplaceNode(
                declarationTypeNode,
                SyntaxFactory.IdentifierName("var").WithTriviaFrom(declarationTypeNode).WithAdditionalAnnotations(annotation));

            var newTree = tree.WithRootAndOptions(newRoot, tree.Options);
            var newSemanticModel = semanticModel.Compilation.ReplaceSyntaxTree(tree, newTree).GetSemanticModel(newTree);

            var newDeclarationTypeNode = newTree.GetRoot(cancellationToken).GetAnnotatedNodes(annotation).Single();
            var newDeclarationType = newSemanticModel.GetTypeInfo(newDeclarationTypeNode, cancellationToken).Type;

            return SymbolEquivalenceComparer.Instance.Equals(declarationType, newDeclarationType);
        }

        /// <summary>
        /// Analyzes the assignment expression and rejects a given declaration if it is unsuitable for implicit typing.
        /// </summary>
        /// <returns>
        /// false, if implicit typing cannot be used.
        /// true, otherwise.
        /// </returns>
        protected override bool AssignmentSupportsStylePreference(
            SyntaxToken identifier,
            TypeSyntax typeName,
            ExpressionSyntax initializer,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            var expression = GetInitializerExpression(initializer);

            // var cannot be assigned null
            if (expression.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return false;
            }

            // cannot use implicit typing on method group or on dynamic
            var declaredType = semanticModel.GetTypeInfo(typeName.StripRefIfNeeded(), cancellationToken).Type;
            if (declaredType != null && declaredType.TypeKind == TypeKind.Dynamic)
            {
                return false;
            }

            // variables declared using var cannot be used further in the same initialization expression.
            if (initializer.DescendantNodesAndSelf()
                .Where(n => n is IdentifierNameSyntax id && id.Identifier.ValueText.Equals(identifier.ValueText))
                .Any(n =>
                {
                    // case of variable direct use: int x = x * 2;
                    if (semanticModel.GetSymbolInfo(n, cancellationToken).Symbol.IsKind(SymbolKind.Local) == true)
                    {
                        return true;
                    }

                    // case of qualification starting with the variable name: SomeEnum SomeEnum = SomeEnum.EnumVal1;
                    // note that: SomeEnum SomeEnum = global::SomeEnum.EnumVal1; // is ok and 'var' can be offered
                    // https://github.com/dotnet/roslyn/issues/26894
                    if (n.Parent is MemberAccessExpressionSyntax memberAccessParent && memberAccessParent.Expression == n)
                    {
                        return true;
                    }

                    return false;
                }))
            {
                return false;
            }

            // Get the conversion that occurred between the expression's type and type implied by the expression's context
            // and filter out implicit conversions. If an implicit conversion (other than identity) exists
            // and if we're replacing the declaration with 'var' we'd be changing the semantics by inferring type of
            // initializer expression and thereby losing the conversion.
            var conversion = semanticModel.GetConversion(expression, cancellationToken);
            if (conversion.IsIdentity)
            {
                // final check to compare type information on both sides of assignment.
                var initializerType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
                return declaredType.Equals(initializerType);
            }

            return false;
        }

        internal static ExpressionSyntax GetInitializerExpression(ExpressionSyntax initializer)
            => initializer is CheckedExpressionSyntax
                ? ((CheckedExpressionSyntax)initializer).Expression.WalkDownParentheses()
                : initializer.WalkDownParentheses();

        protected override bool ShouldAnalyzeDeclarationExpression(DeclarationExpressionSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (declaration.Type.IsVar)
            {
                // If the type is already 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeDeclarationExpression(declaration, semanticModel, cancellationToken);
        }
    }
}
