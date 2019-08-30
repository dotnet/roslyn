// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundNode
    {
#if DEBUG
        /// <summary>
        /// Gives an approximate printout of a bound node as C# code.
        /// </summary>
        internal string DumpSource()
        {
            int indentSize = 4;
            var builder = new StringBuilder();
            appendSourceCore(this, indent: 0, tempIdentifiers: new Dictionary<SynthesizedLocal, int>());
            return builder.ToString();

            void appendSourceCore(BoundNode node, int indent, Dictionary<SynthesizedLocal, int> tempIdentifiers)
            {
                switch (node)
                {
                    case BoundTryStatement tryStatement:
                        {
                            appendLine("try");
                            appendSource(tryStatement.TryBlock);

                            var catchBlocks = tryStatement.CatchBlocks;
                            if (catchBlocks != null)
                            {
                                foreach (var catchBlock in catchBlocks)
                                {
                                    append("catch (");
                                    append(catchBlock.ExceptionTypeOpt.Name);
                                    append(") ");
                                    if (catchBlock.ExceptionFilterOpt != null)
                                    {
                                        append("... exception filter ommitted ...");
                                    }
                                    appendLine("");

                                    appendSource(catchBlock.Body);
                                }
                            }

                            var finallyBlock = tryStatement.FinallyBlockOpt;
                            if (finallyBlock != null)
                            {
                                appendLine("finally");
                                appendSource(finallyBlock);
                            }
                            break;
                        }
                    case BoundThrowStatement throwStatement:
                        {
                            append("throw ");
                            if (throwStatement.ExpressionOpt != null)
                            {
                                appendSource(throwStatement.ExpressionOpt);
                            }
                            appendLine(";");
                            break;
                        }
                    case BoundBlock block:
                        {
                            var statements = block.Statements;
                            if (statements.Length == 1 && block.Locals.IsEmpty)
                            {
                                appendSource(statements[0]);
                                break;
                            }

                            appendLine("{");
                            foreach (var local in block.Locals)
                            {
                                if (local is SynthesizedLocal synthesized)
                                {
                                    appendLine($"{local.TypeWithAnnotations.ToDisplayString()} {name(synthesized)};");
                                }
                                else
                                {
                                    appendLine($"({local.GetDebuggerDisplay()});");
                                }
                            }
                            foreach (var statement in statements)
                            {
                                appendSource(statement);
                            }
                            appendLine("}");
                            break;
                        }
                    case BoundStateMachineScope stateMachineScope:
                        {
                            appendSource(stateMachineScope.Statement);
                            break;
                        }
                    case BoundSequencePoint seqPoint:
                        {
                            var statement = seqPoint.StatementOpt;
                            if (statement != null)
                            {
                                appendSource(statement);
                            }
                            break;
                        }
                    case BoundSequencePointExpression seqPoint:
                        {
                            var expression = seqPoint.Expression;
                            appendSource(expression);
                            break;
                        }
                    case BoundSequencePointWithSpan seqPoint:
                        {
                            var statement = seqPoint.StatementOpt;
                            if (statement != null)
                            {
                                appendSource(statement);
                            }
                            break;
                        }
                    case BoundYieldReturnStatement yieldStatement:
                        {
                            append("yield return ");
                            appendSource(yieldStatement.Expression);
                            appendLine(";");
                            break;
                        }
                    case BoundReturnStatement returnStatement:
                        {
                            append("return");
                            var value = returnStatement.ExpressionOpt;
                            if (value != null)
                            {
                                append(" ");
                                appendSource(value);
                            }
                            appendLine(";");
                            break;
                        }
                    case BoundGotoStatement gotoStatement:
                        {
                            append("goto ");
                            append(gotoStatement.Label.ToString());
                            appendLine(";");
                            break;
                        }
                    case BoundConditionalGoto gotoStatement:
                        {
                            append("if (");
                            append(gotoStatement.JumpIfTrue ? "" : "!");
                            appendSource(gotoStatement.Condition);
                            append(") ");

                            append("goto ");
                            append(gotoStatement.Label.ToString());
                            appendLine(";");
                            break;
                        }
                    case BoundLabelStatement label:
                        {
                            append(label.Label.ToString());
                            appendLine(": ;");
                            break;
                        }
                    case BoundTypeExpression type:
                        {
                            append(type.Type.Name);
                            break;
                        }
                    case BoundLocal local:
                        {
                            var symbol = local.LocalSymbol;
                            appendLocal(symbol);
                            break;
                        }
                    case BoundNoOpStatement noop:
                        {
                            break;
                        }
                    case BoundExpressionStatement expressionStatement:
                        {
                            appendSource(expressionStatement.Expression);
                            appendLine(";");
                            break;
                        }
                    case BoundAwaitExpression awaitExpression:
                        {
                            append("await ");
                            appendSource(awaitExpression.Expression);
                            break;
                        }
                    case BoundCall call:
                        {
                            var receiver = call.ReceiverOpt;
                            if (receiver != null)
                            {
                                appendSource(receiver);
                                append(".");
                            }

                            append(call.Method.Name);
                            append("(");
                            bool first = true;
                            foreach (var argument in call.Arguments)
                            {
                                if (!first)
                                {
                                    append(", ");
                                }
                                first = false;
                                appendSource(argument);
                            }
                            append(")");
                            break;
                        }
                    case BoundLiteral literal:
                        {
                            var value = literal.ConstantValue.Value?.ToString();
                            if (value is null)
                            {
                                append("null");
                                break;
                            }
                            switch (literal.ConstantValue.Discriminator)
                            {
                                case ConstantValueTypeDiscriminator.String:
                                    append($@"""{value}""");
                                    break;
                                default:
                                    append(value);
                                    break;
                            }
                            break;
                        }
                    case BoundAssignmentOperator assignment:
                        {
                            appendSource(assignment.Left);
                            append(" = ");
                            appendSource(assignment.Right);
                            break;
                        }
                    case BoundThisReference thisReference:
                        {
                            append("this");
                            break;
                        }
                    case BoundFieldAccess fieldAccess:
                        {
                            var receiver = fieldAccess.ReceiverOpt;
                            if (receiver != null)
                            {
                                appendSource(receiver);
                                append(".");
                            }

                            append(fieldAccess.FieldSymbol.Name);
                            break;
                        }
                    case BoundSwitchStatement switchStatement:
                        {
                            append("switch (");
                            appendSource(switchStatement.Expression);
                            appendLine(")");
                            appendLine("{");

                            foreach (BoundSwitchSection section in switchStatement.SwitchSections)
                            {
                                foreach (var label in section.SwitchLabels)
                                {
                                    append("case ");
                                    appendSource(label);
                                    appendLine(":");
                                }

                                incrementIndent();

                                foreach (var statement in section.Statements)
                                {
                                    appendSource(statement);
                                }

                                appendLine("break;");

                                decrementIndent();
                            }
                            appendLine("}");
                            break;
                        }
                    case BoundSwitchLabel label:
                        {
                            appendSource(label.Pattern);
                            break;
                        }
                    case BoundUnaryOperator unary:
                        {
                            append($" {unary.OperatorKind.ToString()} ");
                            appendSource(unary.Operand);
                            break;
                        }
                    case BoundStatementList list:
                        {
                            foreach (var statement in list.Statements)
                            {
                                appendSource(statement);
                            }
                            break;
                        }
                    case BoundSequence sequence:
                        {
                            append("{ ");
                            foreach (var effect in sequence.SideEffects)
                            {
                                appendSource(effect);
                                append("; ");
                            }
                            appendSource(sequence.Value);
                            append(" }");
                            break;
                        }
                    case BoundDefaultLiteral _:
                    case BoundDefaultExpression _:
                        {
                            append("default");
                            break;
                        }
                    case BoundBinaryOperator binary:
                        {
                            appendSource(binary.Left);
                            append(" ");
                            append(binary.OperatorKind.ToString());
                            append(" ");
                            appendSource(binary.Right);
                            break;
                        }
                    default:
                        appendLine(node.Kind.ToString());
                        break;
                }

                void appendSource(BoundNode n)
                {
                    if (n is null)
                    {
                        append("NULL");
                    }
                    else
                    {
                        appendSourceCore(n, indent, tempIdentifiers);
                    }
                }

                void append(string s)
                {
                    builder.Append(s);
                }

                void incrementIndent()
                {
                    indent += indentSize;
                    builder.Append(' ', indentSize);
                }

                void decrementIndent()
                {
                    indent -= indentSize;
                    builder.Remove(builder.Length - indentSize, indentSize);
                }

                void appendLine(string s)
                {
                    if (s == "{")
                    {
                        indent += indentSize;
                        builder.AppendLine(s);
                        builder.Append(' ', indent);
                    }
                    else if (s == "}")
                    {
                        builder.Remove(builder.Length - indentSize, indentSize);
                        builder.AppendLine(s);
                        indent -= indentSize;
                        builder.Append(' ', indent);
                    }
                    else
                    {
                        builder.AppendLine(s);
                        builder.Append(' ', indent);
                    }
                }

                string name(SynthesizedLocal local)
                {
                    if (!tempIdentifiers.TryGetValue(local, out int identifier))
                    {
                        identifier = tempIdentifiers.Count + 1;
                        tempIdentifiers.Add(local, identifier);
                    }

                    return "temp" + identifier.ToString();
                }

                void appendLocal(LocalSymbol symbol)
                {
                    if (symbol is SynthesizedLocal synthesized)
                    {
                        append(name(synthesized));
                    }
                    else
                    {
                        append($"({symbol.GetDebuggerDisplay()})");
                    }
                }
            }
        }
#endif
    }
}
