﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    /// <summary>
    /// Helper class that allows us to share lots of logic between the diagnostic analyzer and the
    /// code refactoring provider.  Those can't share a common base class due to their own inheritance
    /// requirements with <see cref="DiagnosticAnalyzer"/> and <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    internal abstract class UseExpressionBodyHelper<TDeclaration> : UseExpressionBodyHelper
        where TDeclaration : SyntaxNode
    {
        public override Option<CodeStyleOption<ExpressionBodyPreference>> Option { get; }
        public override LocalizableString UseExpressionBodyTitle { get; }
        public override LocalizableString UseBlockBodyTitle { get; }
        public override string DiagnosticId { get; }
        public override ImmutableArray<SyntaxKind> SyntaxKinds { get; }

        protected UseExpressionBodyHelper(
            string diagnosticId,
            LocalizableString useExpressionBodyTitle,
            LocalizableString useBlockBodyTitle,
            Option<CodeStyleOption<ExpressionBodyPreference>> option,
            ImmutableArray<SyntaxKind> syntaxKinds)
        {
            DiagnosticId = diagnosticId;
            Option = option;
            UseExpressionBodyTitle = useExpressionBodyTitle;
            UseBlockBodyTitle = useBlockBodyTitle;
            SyntaxKinds = syntaxKinds;
        }

        protected static AccessorDeclarationSyntax GetSingleGetAccessor(AccessorListSyntax accessorList)
        {
            if (accessorList != null &&
                accessorList.Accessors.Count == 1 &&
                accessorList.Accessors[0].AttributeLists.Count == 0 &&
                accessorList.Accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration))
            {
                return accessorList.Accessors[0];
            }

            return null;
        }


        protected static BlockSyntax GetBodyFromSingleGetAccessor(AccessorListSyntax accessorList)
            => GetSingleGetAccessor(accessorList)?.Body;

        public override BlockSyntax GetBody(SyntaxNode declaration)
            => GetBody((TDeclaration)declaration);

        public override ArrowExpressionClauseSyntax GetExpressionBody(SyntaxNode declaration)
            => GetExpressionBody((TDeclaration)declaration);

        public override bool CanOfferUseExpressionBody(OptionSet optionSet, SyntaxNode declaration, bool forAnalyzer)
            => CanOfferUseExpressionBody(optionSet, (TDeclaration)declaration, forAnalyzer);

        public override (bool canOffer, bool fixesError) CanOfferUseBlockBody(OptionSet optionSet, SyntaxNode declaration, bool forAnalyzer)
            => CanOfferUseBlockBody(optionSet, (TDeclaration)declaration, forAnalyzer);

        public override SyntaxNode Update(SyntaxNode declaration, OptionSet options, ParseOptions parseOptions, bool useExpressionBody)
            => Update((TDeclaration)declaration, options, parseOptions, useExpressionBody);

        public override Location GetDiagnosticLocation(SyntaxNode declaration)
            => GetDiagnosticLocation((TDeclaration)declaration);

        protected virtual Location GetDiagnosticLocation(TDeclaration declaration)
            => this.GetBody(declaration).Statements[0].GetLocation();

        public bool CanOfferUseExpressionBody(
            OptionSet optionSet, TDeclaration declaration, bool forAnalyzer)
        {
            var preference = optionSet.GetOption(this.Option).Value;
            var userPrefersExpressionBodies = preference != ExpressionBodyPreference.Never;

            // If the user likes expression bodies, then we offer expression bodies from the diagnostic analyzer.
            // If the user does not like expression bodies then we offer expression bodies from the refactoring provider.
            if (userPrefersExpressionBodies == forAnalyzer)
            {
                var expressionBody = this.GetExpressionBody(declaration);
                if (expressionBody == null)
                {
                    // They don't have an expression body.  See if we could convert the block they 
                    // have into one.

                    var options = declaration.SyntaxTree.Options;
                    var conversionPreference = forAnalyzer ? preference : ExpressionBodyPreference.WhenPossible;

                    return TryConvertToExpressionBody(declaration, options, conversionPreference,
                        out var expressionWhenOnSingleLine, out var semicolonWhenOnSingleLine);
                }
            }

            return false;
        }

        protected virtual bool TryConvertToExpressionBody(
            TDeclaration declaration,
            ParseOptions options, ExpressionBodyPreference conversionPreference, 
            out ArrowExpressionClauseSyntax expressionWhenOnSingleLine, 
            out SyntaxToken semicolonWhenOnSingleLine)
        {
            return TryConvertToExpressionBodyWorker(
                declaration, options, conversionPreference,
                out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
        }

        private bool TryConvertToExpressionBodyWorker(
            SyntaxNode declaration, ParseOptions options, ExpressionBodyPreference conversionPreference,
            out ArrowExpressionClauseSyntax expressionWhenOnSingleLine, out SyntaxToken semicolonWhenOnSingleLine)
        {
            var body = this.GetBody(declaration);

            return body.TryConvertToExpressionBody(
                declaration.Kind(), options, conversionPreference,
                out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
        }

        protected bool TryConvertToExpressionBodyForBaseProperty(
            BasePropertyDeclarationSyntax declaration, ParseOptions options,
            ExpressionBodyPreference conversionPreference,
            out ArrowExpressionClauseSyntax arrowExpression,
            out SyntaxToken semicolonToken)
        {
            if (this.TryConvertToExpressionBodyWorker(
                    declaration, options, conversionPreference,
                    out arrowExpression, out semicolonToken))
            {
                return true;
            }

            var getAccessor = GetSingleGetAccessor(declaration.AccessorList);
            if (getAccessor?.ExpressionBody != null &&
                BlockSyntaxExtensions.MatchesPreference(getAccessor.ExpressionBody.Expression, conversionPreference))
            {
                arrowExpression = SyntaxFactory.ArrowExpressionClause(getAccessor.ExpressionBody.Expression);
                semicolonToken = getAccessor.SemicolonToken;
                return true;
            }

            return false;
        }

        public (bool canOffer, bool fixesError) CanOfferUseBlockBody(
            OptionSet optionSet, TDeclaration declaration, bool forAnalyzer)
        {
            var preference = optionSet.GetOption(this.Option).Value;
            var userPrefersBlockBodies = preference == ExpressionBodyPreference.Never;

            var expressionBodyOpt = this.GetExpressionBody(declaration);
            var canOffer = expressionBodyOpt?.TryConvertToBlock(
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken), false, out var block) == true;
            if (!canOffer)
            {
                return (canOffer, fixesError: false);
            }

            var languageVersion = ((CSharpParseOptions)declaration.SyntaxTree.Options).LanguageVersion;
            if (expressionBodyOpt.Expression.IsKind(SyntaxKind.ThrowExpression) &&
                languageVersion < LanguageVersion.CSharp7)
            {
                // If they're using a throw expression in a declaration and it's prior to C# 7
                // then always mark this as something that can be fixed by the analyzer.  This way
                // we'll also get 'fix all' working to fix all these cases.
                return (canOffer, fixesError: true);
            }

            var isAccessorOrConstructor = declaration is AccessorDeclarationSyntax ||
                                          declaration is ConstructorDeclarationSyntax;
            if (isAccessorOrConstructor &&
                languageVersion < LanguageVersion.CSharp7)
            {
                // If they're using expression bodies for accessors/constructors and it's prior to C# 7
                // then always mark this as something that can be fixed by the analyzer.  This way
                // we'll also get 'fix all' working to fix all these cases.
                return (canOffer, fixesError: true);
            }
            else if (languageVersion < LanguageVersion.CSharp6)
            {
                // If they're using expression bodies prior to C# 6, then always mark this as something
                // that can be fixed by the analyzer.  This way we'll also get 'fix all' working to fix 
                // all these cases.
                return (canOffer, fixesError: true);
            }

            // If the user likes block bodies, then we offer block bodies from the diagnostic analyzer.
            // If the user does not like block bodies then we offer block bodies from the refactoring provider.
            return (userPrefersBlockBodies == forAnalyzer, fixesError: false);
        }

        public TDeclaration Update(
            TDeclaration declaration, OptionSet options, 
            ParseOptions parseOptions, bool useExpressionBody)
        {
            if (useExpressionBody)
            {
                TryConvertToExpressionBody(
                    declaration, declaration.SyntaxTree.Options, ExpressionBodyPreference.WhenPossible,
                    out var expressionBody, out var semicolonToken);

                var trailingTrivia = semicolonToken.TrailingTrivia
                                                   .Where(t => t.Kind() != SyntaxKind.EndOfLineTrivia)
                                                   .Concat(declaration.GetTrailingTrivia());
                semicolonToken = semicolonToken.WithTrailingTrivia(trailingTrivia);

                return WithSemicolonToken(
                           WithExpressionBody(
                               WithBody(declaration, body: null),
                               expressionBody),
                           semicolonToken);
            }
            else
            {
                return WithSemicolonToken(
                           WithExpressionBody(
                               WithGenerateBody(declaration, options, parseOptions),
                               expressionBody: null),
                           default(SyntaxToken));
            }
        }

        protected abstract BlockSyntax GetBody(TDeclaration declaration);

        protected abstract ArrowExpressionClauseSyntax GetExpressionBody(TDeclaration declaration);

        protected abstract bool CreateReturnStatementForExpression(TDeclaration declaration);

        protected abstract SyntaxToken GetSemicolonToken(TDeclaration declaration);

        protected abstract TDeclaration WithSemicolonToken(TDeclaration declaration, SyntaxToken token);
        protected abstract TDeclaration WithExpressionBody(TDeclaration declaration, ArrowExpressionClauseSyntax expressionBody);
        protected abstract TDeclaration WithBody(TDeclaration declaration, BlockSyntax body);

        protected virtual TDeclaration WithGenerateBody(
            TDeclaration declaration, OptionSet options, ParseOptions parseOptions)
        {
            var expressionBody = GetExpressionBody(declaration);
            var semicolonToken = GetSemicolonToken(declaration);

            if (expressionBody.TryConvertToBlock(
                    GetSemicolonToken(declaration),
                    CreateReturnStatementForExpression(declaration),
                    out var block))
            {
                return WithBody(declaration, block);
            }

            return declaration;
        }

        protected TDeclaration WithAccessorList(
            TDeclaration declaration, OptionSet options, ParseOptions parseOptions)
        {
            var expressionBody = GetExpressionBody(declaration);
            var semicolonToken = GetSemicolonToken(declaration);

            // When converting an expression-bodied property to a block body, always attempt to 
            // create an accessor with a block body (even if the user likes expression bodied
            // accessors.  While this technically doesn't match their preferences, it fits with
            // the far more likely scenario that the user wants to convert this property into
            // a full property so that they can flesh out the body contents.  If we keep around
            // an expression bodied accessor they'll just have to convert that to a block as well
            // and that means two steps to take instead of one.

            expressionBody.TryConvertToBlock(GetSemicolonToken(declaration), CreateReturnStatementForExpression(declaration), out var block);

            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            accessor = block != null
                ? accessor.WithBody(block)
                : accessor.WithExpressionBody(expressionBody)
                          .WithSemicolonToken(semicolonToken);

            return WithAccessorList(declaration, SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(accessor)));
        }

        protected virtual TDeclaration WithAccessorList(TDeclaration declaration, AccessorListSyntax accessorListSyntax)
        {
            throw new NotImplementedException();
        }
    }
}
