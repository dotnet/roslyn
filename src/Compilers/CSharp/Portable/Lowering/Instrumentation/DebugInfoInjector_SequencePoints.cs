// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DebugInfoInjector
    {
        private BoundStatement AddSequencePoint(BoundStatement node)
        {
            return new BoundSequencePoint(node.Syntax, node);
        }

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
            Debug.Assert(declarationSyntax.Initializer != null);
            int start = declarationSyntax.Initializer.Value.SpanStart;
            int end = declarationSyntax.Initializer.Span.End;
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

        private static TextSpan CreateSpanForConstructorInitializer(ConstructorDeclarationSyntax constructorSyntax)
        {
            if (constructorSyntax.Initializer != null)
            {
                //  [SomeAttribute] public MyCtorName(params int[] values): [|base()|] { ... }
                var start = constructorSyntax.Initializer.ThisOrBaseKeyword.SpanStart;
                var end = constructorSyntax.Initializer.ArgumentList.CloseParenToken.Span.End;
                return TextSpan.FromBounds(start, end);
            }

            if (constructorSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                // [SomeAttribute] static MyCtorName(...) [|{|] ... }
                var start = constructorSyntax.Body.OpenBraceToken.SpanStart;
                var end = constructorSyntax.Body.OpenBraceToken.Span.End;
                return TextSpan.FromBounds(start, end);
            }

            //  [SomeAttribute] [|public MyCtorName(params int[] values)|] { ... }
            return CreateSpan(constructorSyntax.Modifiers, constructorSyntax.Identifier, constructorSyntax.ParameterList.CloseParenToken);
        }

        private static TextSpan CreateSpan(SyntaxTokenList startOpt, SyntaxNodeOrToken startFallbackOpt, SyntaxNodeOrToken endOpt)
        {
            Debug.Assert(startFallbackOpt != default(SyntaxNodeOrToken) || endOpt != default(SyntaxNodeOrToken));

            int startPos;
            if (startOpt.Count > 0)
            {
                startPos = startOpt.First().SpanStart;
            }
            else if (startFallbackOpt != default(SyntaxNodeOrToken))
            {
                startPos = startFallbackOpt.SpanStart;
            }
            else
            {
                startPos = endOpt.SpanStart;
            }

            int endPos;
            if (endOpt != default(SyntaxNodeOrToken))
            {
                endPos = GetEndPosition(endOpt);
            }
            else
            {
                endPos = GetEndPosition(startFallbackOpt);
            }

            return TextSpan.FromBounds(startPos, endPos);
        }

        private static int GetEndPosition(SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsToken)
            {
                return nodeOrToken.Span.End;
            }
            else
            {
                return nodeOrToken.AsNode().GetLastToken().Span.End;
            }
        }

        internal static void GetBreakpointSpan(VariableDeclaratorSyntax declaratorSyntax, out SyntaxNode node, out TextSpan? part)
        {
            var declarationSyntax = (VariableDeclarationSyntax)declaratorSyntax.Parent;

            if (declarationSyntax.Variables.First() == declaratorSyntax)
            {
                switch (declarationSyntax.Parent.Kind())
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
                        throw ExceptionUtilities.UnexpectedValue(declarationSyntax.Parent.Kind());
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

        private static BoundExpression AddConditionSequencePoint(BoundExpression condition, SyntaxNode synthesizedVariableSyntax, SyntheticBoundNodeFactory factory)
        {
            if (!factory.Compilation.Options.EnableEditAndContinue)
            {
                return condition;
            }

            // The local has to be associated with a syntax that is tracked by EnC source mapping.
            // At most one ConditionalBranchDiscriminator variable shall be associated with any given EnC tracked syntax node.
            var local = factory.SynthesizedLocal(condition.Type, synthesizedVariableSyntax, kind: SynthesizedLocalKind.ConditionalBranchDiscriminator);

            // Add hidden sequence point unless the condition is a constant expression.
            // Constant expression must stay a const to not invalidate results of control flow analysis.
            var valueExpression = (condition.ConstantValue == null) ?
                new BoundSequencePointExpression(syntax: null, expression: factory.Local(local), type: condition.Type) :
                condition;

            return new BoundSequence(
                condition.Syntax,
                ImmutableArray.Create(local),
                ImmutableArray.Create<BoundExpression>(factory.AssignmentExpression(factory.Local(local), condition)),
                valueExpression,
                condition.Type);
        }
    }
}
