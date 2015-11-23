// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class ObjectInitializerCompletionProvider : AbstractObjectInitializerCompletionProvider
    {
        protected override async Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // We're exclusive if this context could only be an object initializer and not also a
            // collection initializer. If we're initializing something that could be initialized as
            // an object or as a collection, say we're not exclusive. That way the rest of
            // intellisense can be used in the collection initializer.
            // 
            // Consider this case:

            // class c : IEnumerable<int> 
            // { 
            // public void Add(int addend) { }
            // public int foo; 
            // }

            // void foo()
            // {
            //    var b = new c {|
            // }

            // There we could initialize b using either an object initializer or a collection
            // initializer. Since we don't know which the user will use, we'll be non-exclusive, so
            // the other providers can help the user write the collection initializer, if they want
            // to.
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            if (tree.IsInNonUserCode(position, cancellationToken))
            {
                return false;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Parent == null)
            {
                return false;
            }

            var expression = token.Parent.Parent as ExpressionSyntax;
            if (expression == null)
            {
                return false;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(expression, cancellationToken).ConfigureAwait(false);
            var initializedType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (initializedType == null)
            {
                return false;
            }

            var enclosingSymbol = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            // Non-exclusive if initializedType can be initialized as a collection.
            if (initializedType.CanSupportCollectionInitializer(enclosingSymbol))
            {
                return false;
            }

            // By default, only our member names will show up.
            return true;
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options) || text[characterPosition] == ' ';
        }

        protected override Tuple<ITypeSymbol, Location> GetInitializedType(
            Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var tree = semanticModel.SyntaxTree;
            if (tree.IsInNonUserCode(position, cancellationToken))
            {
                return null;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() != SyntaxKind.CommaToken && token.Kind() != SyntaxKind.OpenBraceToken)
            {
                return null;
            }

            if (token.Parent == null || token.Parent.Parent == null)
            {
                return null;
            }

            // If we got a comma, we can syntactically find out if we're in an ObjectInitializerExpression
            if (token.Kind() == SyntaxKind.CommaToken &&
                token.Parent.Kind() != SyntaxKind.ObjectInitializerExpression)
            {
                return null;
            }

            // new Foo { bar = $$
            if (token.Parent.Parent.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                var objectCreation = token.Parent.Parent as ObjectCreationExpressionSyntax;
                if (objectCreation == null)
                {
                    return null;
                }

                var ctor = semanticModel.GetSymbolInfo(objectCreation, cancellationToken).Symbol;
                var type = ctor != null ? ctor.ContainingType : null;

                return Tuple.Create<ITypeSymbol, Location>(type, token.GetLocation());
            }

            // Nested: new Foo { bar = { $$
            if (token.Parent.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                // Use the type inferrer to get the type being initialized.
                var typeInferenceService = document.Project.LanguageServices.GetService<ITypeInferenceService>();
                var parentInitializer = token.GetAncestor<InitializerExpressionSyntax>();
                var expectedType = typeInferenceService.InferType(semanticModel, parentInitializer, objectAsDefault: false, cancellationToken: cancellationToken);
                return Tuple.Create(expectedType, token.GetLocation());
            }

            return null;
        }

        protected override HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
                            .GetPreviousTokenIfTouchingWord(position);

            // We should have gotten back a { or ,
            if (token.Kind() == SyntaxKind.CommaToken || token.Kind() == SyntaxKind.OpenBraceToken)
            {
                if (token.Parent != null)
                {
                    var initializer = token.Parent as InitializerExpressionSyntax;

                    if (initializer != null)
                    {
                        return new HashSet<string>(initializer.Expressions.OfType<AssignmentExpressionSyntax>()
                            .Where(b => b.OperatorToken.Kind() == SyntaxKind.EqualsToken)
                            .Select(b => b.Left)
                            .OfType<IdentifierNameSyntax>()
                            .Select(i => i.Identifier.ValueText));
                    }
                }
            }

            return new HashSet<string>();
        }

        protected override TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        protected override bool IsInitializable(ISymbol member, INamedTypeSymbol containingType)
        {
            if (member is IPropertySymbol && ((IPropertySymbol)member).Parameters.Any(p => !p.IsOptional))
            {
                return false;
            }

            return base.IsInitializable(member, containingType);
        }
    }
}
