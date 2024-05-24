// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DebugInfoInjector
    {
        private static BoundStatement AddSequencePoint(BoundStatement node)
        {
            return new BoundSequencePoint(node.Syntax, node);
        }

        internal static BoundStatement AddSequencePoint(VariableDeclaratorSyntax declaratorSyntax, BoundStatement rewrittenStatement)
        {
            GetBreakpointSpan(declaratorSyntax, out _, out TextSpan? part);
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

        private static TextSpan CreateSpan(ParameterSyntax parameter)
            // exclude attributes and default value:
            // [A] [|in T p|] = default
            => CreateSpan(parameter.Modifiers, parameter.Type, parameter.Identifier);

        private static TextSpan CreateSpan(SyntaxTokenList startOpt, SyntaxNodeOrToken startFallbackOpt, SyntaxNodeOrToken endOpt)
        {
            Debug.Assert(startFallbackOpt != default || endOpt != default);

            int startPos;
            if (startOpt.Count > 0)
            {
                startPos = startOpt.First().SpanStart;
            }
            else if (startFallbackOpt != default)
            {
                startPos = startFallbackOpt.SpanStart;
            }
            else
            {
                startPos = endOpt.SpanStart;
            }

            int endPos;
            if (endOpt != default)
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
            => nodeOrToken.AsNode(out var node) ? node.GetLastToken().Span.End : nodeOrToken.Span.End;

        internal static void GetBreakpointSpan(VariableDeclaratorSyntax declaratorSyntax, out SyntaxNode node, out TextSpan? part)
        {
            Debug.Assert(declaratorSyntax.Parent != null);
            var declarationSyntax = (VariableDeclarationSyntax)declaratorSyntax.Parent;
            Debug.Assert(declarationSyntax.Parent != null);

            if (declarationSyntax.Variables.First() == declaratorSyntax)
            {
                switch (declarationSyntax.Parent.Kind())
                {
                    case SyntaxKind.EventFieldDeclaration:
                    case SyntaxKind.FieldDeclaration:
                        {
                            var modifiers = ((BaseFieldDeclarationSyntax)declarationSyntax.Parent).Modifiers;
                            GetFirstLocalOrFieldBreakpointSpan(modifiers.Any() ? modifiers[0] : (SyntaxToken?)null, declaratorSyntax, out node, out part);
                        }
                        break;

                    case SyntaxKind.LocalDeclarationStatement:
                        {
                            var parent = (LocalDeclarationStatementSyntax)declarationSyntax.Parent;
                            var modifiers = parent.Modifiers;
                            Debug.Assert(!modifiers.Any(SyntaxKind.ConstKeyword)); // const locals don't have a sequence point
                            var firstToken =
                                modifiers.Any() ? modifiers[0] :
                                parent.UsingKeyword == default ? (SyntaxToken?)null :
                                parent.AwaitKeyword == default ? parent.UsingKeyword :
                                parent.AwaitKeyword;
                            GetFirstLocalOrFieldBreakpointSpan(firstToken, declaratorSyntax, out node, out part);
                        }
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

        internal static void GetFirstLocalOrFieldBreakpointSpan(SyntaxToken? firstToken, VariableDeclaratorSyntax declaratorSyntax, out SyntaxNode node, out TextSpan? part)
        {
            Debug.Assert(declaratorSyntax.Parent != null);

            var declarationSyntax = (VariableDeclarationSyntax)declaratorSyntax.Parent;

            Debug.Assert(declarationSyntax.Parent != null);

            // The first token may be a modifier (like public) or using or await
            int start = firstToken?.SpanStart ?? declarationSyntax.SpanStart;

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
            Debug.Assert(condition.Type is not null);

            if (!factory.Compilation.Options.EnableEditAndContinue)
            {
                return condition;
            }

            // The local has to be associated with a syntax that is tracked by EnC source mapping.
            // At most one ConditionalBranchDiscriminator variable shall be associated with any given EnC tracked syntax node.
            var local = factory.SynthesizedLocal(condition.Type, synthesizedVariableSyntax, kind: SynthesizedLocalKind.ConditionalBranchDiscriminator);

            // Add hidden sequence point unless the condition is a constant expression.
            // Constant expression must stay a const to not invalidate results of control flow analysis.
            var valueExpression = (condition.ConstantValueOpt == null) ?
                new BoundSequencePointExpression(syntax: null!, expression: factory.Local(local), type: condition.Type) :
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
