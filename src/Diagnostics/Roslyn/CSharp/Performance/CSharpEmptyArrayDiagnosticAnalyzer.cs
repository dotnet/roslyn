// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Performance
{
    /// <summary>Provides a C# analyzer that finds empty array allocations that can be replaced with a call to Array.Empty.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpEmptyArrayDiagnosticAnalyzer : EmptyArrayDiagnosticAnalyzer
    {
        /// <summary>
        /// The kinds of array creation syntax kinds to be analyzed.  We care about ArrayCreationExpression (e.g. "new byte[2]" or "new byte[] { 1, 2, 3 }")
        /// and ArrayInitializerExpression (e.g. "byte[] arr = { }"), but not ImplicitArrayCreationExpression, because you can't have an
        /// implicitly typed array of length 0 (e.g. "new[] { }" is erroneous).
        /// </summary>
        private static readonly SyntaxKind[] s_expressionKinds = new[] { SyntaxKind.ArrayCreationExpression, SyntaxKind.ArrayInitializerExpression };

        /// <summary>Called once at compilation start to register actions in the compilation context.</summary>
        /// <param name="context">The analysis context.</param>
        internal override void RegisterSyntaxNodeAction(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ctx =>
            {
                // We can't replace array allocations in attributes, as they're persisted to metadata
                if (ctx.Node.Parent is AttributeArgumentSyntax)
                {
                    return;
                }

                InitializerExpressionSyntax initializerExpr = null;
                bool isArrayCreationExpression = ctx.Node.Kind() == SyntaxKind.ArrayCreationExpression;
                if (isArrayCreationExpression)
                {
                    // This is an ArrayCreationExpression (e.g. "new byte[2]" or "new byte[] { 1, 2 }").
                    // If it has only a single rank, and if the input to it is a constant 0, report it.
                    ArrayCreationExpressionSyntax arrayCreationExpr = (ArrayCreationExpressionSyntax)ctx.Node;
                    var arrayType = arrayCreationExpr.Type;
                    if (arrayType.ElementType is PointerTypeSyntax) // pointers can't be used as generic arguments
                    {
                        return;
                    }

                    if (arrayType.RankSpecifiers.Count >= 1)
                    {
                        // Make sure this is a single dimensional array.
                        ArrayRankSpecifierSyntax rankSpecifier = arrayType.RankSpecifiers[0];
                        if (rankSpecifier.Rank != 1)
                        {
                            return;
                        }

                        // Parse out the numerical length from the rank specifier.  It must be a constant zero.
                        LiteralExpressionSyntax sizeSpecifier = rankSpecifier.ChildNodes().FirstOrDefault(child => child.Kind() == SyntaxKind.NumericLiteralExpression) as LiteralExpressionSyntax;
                        if (sizeSpecifier != null)
                        {
                            if (sizeSpecifier.Token.Value is int && ((int)sizeSpecifier.Token.Value) == 0)
                            {
                                Report(ctx, arrayCreationExpr);
                            }

                            return;
                        }

                        // We couldn't determine the length based on the rank specifier.
                        // We'll try to do so based on any initializer expression the array has.
                        initializerExpr = arrayCreationExpr.Initializer;
                    }
                }
                else if (!(ctx.Node.Parent is ArrayCreationExpressionSyntax))
                {
                    // This is an ArrayInitializerExpression (e.g. { 1, 2 }).  However, we only want to examine it
                    // if it's not part of a larger ArrayCreationExpressionSyntax; otherwise, we'll end up
                    // flagging it twice and recommending incorrect transforms.
                    initializerExpr = ctx.Node as InitializerExpressionSyntax;
                }

                // If we needed to examine the initializer in order to get the size and if it's 0, 
                // report it, but make sure to report the right syntax node.
                if (initializerExpr != null && initializerExpr.Expressions.Count == 0)
                {
                    Report(ctx, isArrayCreationExpression ? ctx.Node : initializerExpr);
                }
            }, s_expressionKinds);
        }
    }
}
