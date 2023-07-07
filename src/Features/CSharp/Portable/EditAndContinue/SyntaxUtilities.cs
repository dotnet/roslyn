// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal static partial class SyntaxUtilities
    {
        public static LambdaBody CreateLambdaBody(SyntaxNode node)
            => new CSharpLambdaBody(node);

        public static MemberBody? TryGetDeclarationBody(SyntaxNode node)
            => node switch
            {
                MethodDeclarationSyntax methodDeclaration => CreateSimpleBody(BlockOrExpression(methodDeclaration.Body, methodDeclaration.ExpressionBody)),
                ConversionOperatorDeclarationSyntax conversionDeclaration => CreateSimpleBody(BlockOrExpression(conversionDeclaration.Body, conversionDeclaration.ExpressionBody)),
                OperatorDeclarationSyntax operatorDeclaration => CreateSimpleBody(BlockOrExpression(operatorDeclaration.Body, operatorDeclaration.ExpressionBody)),
                AccessorDeclarationSyntax accessorDeclaration => CreateSimpleBody(BlockOrExpression(accessorDeclaration.Body, accessorDeclaration.ExpressionBody)),
                DestructorDeclarationSyntax destructorDeclaration => CreateSimpleBody(BlockOrExpression(destructorDeclaration.Body, destructorDeclaration.ExpressionBody)),

                // We associate the body of expression-bodied property/indexer with the ArrowExpressionClause
                // since that's the syntax node associated with the getter symbol.
                // The property/indexer itself is considered to not have a body unless the property has an initializer.
                ArrowExpressionClauseSyntax { Parent: (kind: SyntaxKind.PropertyDeclaration) or (kind: SyntaxKind.IndexerDeclaration) } arrowClause => CreateSimpleBody(arrowClause.Expression),
                PropertyDeclarationSyntax propertyDeclaration => CreateSimpleBody(propertyDeclaration.Initializer?.Value),

                ConstructorDeclarationSyntax constructorDeclaration when constructorDeclaration.Body != null || constructorDeclaration.ExpressionBody != null
                    => constructorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
                       ? CreateSimpleBody(BlockOrExpression(constructorDeclaration.Body, constructorDeclaration.ExpressionBody))
                       : (constructorDeclaration.Initializer != null)
                       ? new InstanceConstructorWithExplicitInitializerDeclarationBody(constructorDeclaration)
                       : new InstanceConstructorWithImplicitInitializerDeclarationBody(constructorDeclaration),

                CompilationUnitSyntax unit when unit.ContainsGlobalStatements()
                    => new TopLevelCodeDeclarationBody(unit),

                VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax fieldDeclaration, Initializer: { } } variableDeclarator
                    when !fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword)
                    => new FieldWithInitializerDeclarationBody(variableDeclarator),

                _ => null
            };

        internal static MemberBody? CreateSimpleBody(SyntaxNode? body)
        {
            if (body == null)
            {
                return null;
            }

            AssertIsBody(body, allowLambda: false);
            return new SimpleMemberBody(body);
        }

        public static SyntaxNode? BlockOrExpression(BlockSyntax? blockBody, ArrowExpressionClauseSyntax? expressionBody)
             => (SyntaxNode?)blockBody ?? expressionBody?.Expression;

        [Conditional("DEBUG")]
        public static void AssertIsBody(SyntaxNode syntax, bool allowLambda)
        {
            // lambda/query
            if (LambdaUtilities.IsLambdaBody(syntax))
            {
                Debug.Assert(allowLambda);
                Debug.Assert(syntax is ExpressionSyntax or BlockSyntax);
                return;
            }

            // block body
            if (syntax is BlockSyntax)
            {
                return;
            }

            // expression body
            if (syntax is ExpressionSyntax { Parent: ArrowExpressionClauseSyntax })
            {
                return;
            }

            // field initializer
            if (syntax is ExpressionSyntax { Parent.Parent: VariableDeclaratorSyntax })
            {
                return;
            }

            // property initializer
            if (syntax is ExpressionSyntax { Parent.Parent: PropertyDeclarationSyntax })
            {
                return;
            }

            // special case for top level statements, which have no containing block other than the compilation unit
            if (syntax is CompilationUnitSyntax unit && unit.ContainsGlobalStatements())
            {
                return;
            }

            Debug.Assert(false);
        }

        public static bool ContainsGlobalStatements(this CompilationUnitSyntax compilationUnit)
            => compilationUnit.Members is [GlobalStatementSyntax, ..];

        public static bool Any(TypeParameterListSyntax? list)
            => list != null && list.ChildNodesAndTokens().Count != 0;

        public static SyntaxNode? TryGetEffectiveGetterBody(SyntaxNode declaration)
        {
            if (declaration is PropertyDeclarationSyntax property)
            {
                return TryGetEffectiveGetterBody(property.ExpressionBody, property.AccessorList);
            }

            if (declaration is IndexerDeclarationSyntax indexer)
            {
                return TryGetEffectiveGetterBody(indexer.ExpressionBody, indexer.AccessorList);
            }

            return null;
        }

        public static SyntaxNode? TryGetEffectiveGetterBody(ArrowExpressionClauseSyntax? propertyBody, AccessorListSyntax? accessorList)
        {
            if (propertyBody != null)
            {
                return propertyBody.Expression;
            }

            var firstGetter = accessorList?.Accessors.Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)).FirstOrDefault();
            if (firstGetter == null)
            {
                return null;
            }

            return (SyntaxNode?)firstGetter.Body ?? firstGetter.ExpressionBody?.Expression;
        }

        public static SyntaxTokenList? TryGetFieldOrPropertyModifiers(SyntaxNode node)
        {
            if (node is FieldDeclarationSyntax fieldDecl)
                return fieldDecl.Modifiers;

            if (node is PropertyDeclarationSyntax propertyDecl)
                return propertyDecl.Modifiers;

            return null;
        }

        public static bool IsParameterlessConstructor(SyntaxNode declaration)
        {
            if (declaration is not ConstructorDeclarationSyntax ctor)
            {
                return false;
            }

            return ctor.ParameterList.Parameters.Count == 0;
        }

        public static bool HasBackingField(PropertyDeclarationSyntax property)
        {
            if (property.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                property.Modifiers.Any(SyntaxKind.ExternKeyword))
            {
                return false;
            }

            return property.ExpressionBody == null
                && property.AccessorList!.Accessors.Any(e => e.Body == null && e.ExpressionBody == null);
        }

        /// <summary>
        /// True if the specified declaration node is an async method, anonymous function, lambda, local function.
        /// </summary>
        public static bool IsAsyncDeclaration(SyntaxNode declaration)
        {
            // lambdas and anonymous functions
            if (declaration is AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                return anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
            }

            // expression bodied methods/local functions:
            if (declaration.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                Contract.ThrowIfNull(declaration.Parent);
                declaration = declaration.Parent;
            }

            return declaration switch
            {
                MethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                LocalFunctionStatementSyntax localFunction => localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword),
                _ => false
            };
        }

        /// <summary>
        /// Returns a list of all await expressions, await foreach statements, await using declarations and yield statements in the given body,
        /// in the order in which they occur.
        /// </summary>
        /// <returns>
        /// <see cref="AwaitExpressionSyntax"/> for await expressions,
        /// <see cref="YieldStatementSyntax"/> for yield return statements,
        /// <see cref="CommonForEachStatementSyntax"/> for await foreach statements,
        /// <see cref="VariableDeclaratorSyntax"/> for await using declarators.
        /// <see cref="UsingStatementSyntax"/> for await using statements.
        /// </returns>
        public static IEnumerable<SyntaxNode> GetSuspensionPoints(SyntaxNode body)
            => body.DescendantNodesAndSelf(LambdaUtilities.IsNotLambda).Where(SyntaxBindingUtilities.BindsToResumableStateMachineState);

        // Presence of yield break or yield return indicates state machine, but yield break does not bind to a resumable state. 
        public static bool IsIterator(SyntaxNode body)
            => body.DescendantNodesAndSelf(LambdaUtilities.IsNotLambda).Any(n => n is YieldStatementSyntax);
    }
}
