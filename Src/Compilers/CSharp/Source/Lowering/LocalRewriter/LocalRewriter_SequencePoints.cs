// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class LocalRewriter
    {
        internal static BoundStatement AddSequencePoint(VariableDeclaratorSyntax declaratorSyntax, BoundStatement rewrittenStatement)
        {
            SyntaxNode node;
            TextSpan? part;
            GetBreakpointSpan(declaratorSyntax, out node, out part);
            var result = BoundSequencePoint.Create(declaratorSyntax, part, rewrittenStatement);
            result.WasCompilerGenerated = rewrittenStatement.WasCompilerGenerated;
            return result;
        }

        internal static BoundStatement AddSequencePoint(PropertyDeclarationSyntax declarationSyntax, BoundStatement rewrittenStatement)
        {
            SyntaxTokenList modifiers = declarationSyntax.Modifiers;
            // Skip attributes
            int start = modifiers.Any() ? modifiers[0].SpanStart : declarationSyntax.Type.SpanStart;
            int end = declarationSyntax.Span.End;
            TextSpan part = TextSpan.FromBounds(start, end);

            var result = BoundSequencePoint.Create(declarationSyntax, part, rewrittenStatement);
            result.WasCompilerGenerated = rewrittenStatement.WasCompilerGenerated;
            return result;
        }

        internal static BoundStatement AddSequencePoint(UsingStatementSyntax usingSyntax, BoundStatement rewrittenStatement)
        {
            int start = usingSyntax.Span.Start;
            int end = usingSyntax.CloseParenToken.Span.End;
            TextSpan span = TextSpan.FromBounds(start, end);
            return new BoundSequencePointWithSpan(usingSyntax, rewrittenStatement, span);
        }

        internal static void GetBreakpointSpan(VariableDeclaratorSyntax declaratorSyntax, out SyntaxNode node, out TextSpan? part)
        {
            var declarationSyntax = (VariableDeclarationSyntax)declaratorSyntax.Parent;

            if (declarationSyntax.Variables.First() == declaratorSyntax)
            {
                switch (declarationSyntax.Parent.Kind)
                {
                    case SyntaxKind.EventFieldDeclaration:
                    case SyntaxKind.FieldDeclaration:
                        var modifiers = ((BaseFieldDeclarationSyntax)declarationSyntax.Parent).Modifiers;
                        GetFirstLocalOrFieldBreakpointSpan(modifiers, declaratorSyntax, out node, out part);
                        break;

                    case SyntaxKind.LocalDeclarationStatement:
                        // only const locals have modifiers and those don't have a sequence point:
                        Debug.Assert(!((LocalDeclarationStatementSyntax)declarationSyntax.Parent).Modifiers.Any());
                        GetFirstLocalOrFieldBreakpointSpan(default(SyntaxTokenList), declaratorSyntax, out node, out part);
                        break;

                    case SyntaxKind.UsingStatement:
                    case SyntaxKind.FixedStatement:
                    case SyntaxKind.ForStatement:
                        // for ([|int i = 1|]; i < 10; i++)
                        // for ([|int i = 1|], j = 0; i < 10; i++)
                        node = declarationSyntax;
                        part = TextSpan.FromBounds(declarationSyntax.SpanStart, declaratorSyntax.Span.End);
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }
            else
            {
                // int x = 1, [|y = 2|];
                // public static int x = 1, [|y = 2|];
                // for (int i = 1, [|j = 0|]; i < 10; i++)
                node = declaratorSyntax;
                part = null;
            }
        }

        internal static void GetFirstLocalOrFieldBreakpointSpan(SyntaxTokenList modifiers, VariableDeclaratorSyntax declaratorSyntax, out SyntaxNode node, out TextSpan? part)
        {
            var declarationSyntax = (VariableDeclarationSyntax)declaratorSyntax.Parent;

            int start = modifiers.Any() ? modifiers[0].SpanStart : declarationSyntax.SpanStart;

            int end;
            if (declarationSyntax.Variables.Count == 1)
            {
                // [|int x = 1;|]
                // [|public static int x = 1;|]
                end = declarationSyntax.Parent.Span.End;
            }
            else
            {
                // [|int x = 1|], y = 2;
                // [|public static int x = 1|], y = 2;
                end = declaratorSyntax.Span.End;
            }

            part = TextSpan.FromBounds(start, end);
            node = declarationSyntax.Parent;
        }
    }
}
