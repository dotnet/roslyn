// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

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
                                    append(catchBlock.ExceptionTypeOpt?.Name);
                                    append(" ");
                                    appendSource(catchBlock.ExceptionSourceOpt);
                                    append(")");
                                    if (catchBlock.ExceptionFilterOpt is { } exceptionFilter)
                                    {
                                        if (catchBlock.ExceptionFilterPrologueOpt is { } exceptionFilterPrologue)
                                        {
                                            appendLine("");
                                            appendLine("{");
                                            appendSource(exceptionFilterPrologue);
                                            appendLine("}");
                                        }
                                        else
                                        {
                                            append(" ");
                                        }
                                        append("when (");
                                        appendSource(exceptionFilter);
                                        append(")");
                                    }
                                    appendLine("");

                                    appendSource(catchBlock.Body);
                                }
                            }

                            var finallyBlock = tryStatement.FinallyBlockOpt;
                            if (finallyBlock != null)
                            {
                                appendLine("finally");
                                appendLine("{");
                                appendSource(finallyBlock);
                                appendLine("}");
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
                            if (statements.Length == 0)
                            {
                                break;
                            }

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
                            append(gotoStatement.JumpIfTrue ? "" : "!(");
                            appendSource(gotoStatement.Condition);
                            append(gotoStatement.JumpIfTrue ? "" : ")");
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
                            appendSourceItems(call.Arguments);
                            append(")");
                            break;
                        }
                    case BoundLiteral literal:
                        {
                            ConstantValue? constantValueOpt = literal.ConstantValueOpt;
                            appendConstantValue(constantValueOpt);
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
                    case BoundConversion conversion:
                        {
                            append($" {conversion.Conversion} ");
                            appendSource(conversion.Operand);
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
                            string relation = (binary.OperatorKind & BinaryOperatorKind.OpMask) switch
                            {
                                BinaryOperatorKind.GreaterThan => ">",
                                BinaryOperatorKind.GreaterThanOrEqual => ">=",
                                BinaryOperatorKind.LessThan => "<",
                                BinaryOperatorKind.LessThanOrEqual => "<=",
                                BinaryOperatorKind.Equal => "==",
                                BinaryOperatorKind.NotEqual => "!=",
                                BinaryOperatorKind.Addition => "+",
                                BinaryOperatorKind.Subtraction => "-",
                                _ => binary.OperatorKind.ToString()
                            };

                            appendSource(binary.Left);
                            append(" ");
                            append(relation);
                            append(" ");
                            appendSource(binary.Right);
                            break;
                        }
                    case BoundBinaryPattern binaryPattern:
                        {
                            append("(");
                            appendSource(binaryPattern.Left);
                            append(binaryPattern.Disjunction ? " or " : " and ");
                            appendSource(binaryPattern.Right);
                            append(")");
                            break;
                        }
                    case BoundConstantPattern constantPattern:
                        {
                            appendConstantValue(constantPattern.ConstantValue);
                            break;
                        }
                    case BoundNegatedPattern negatedPattern:
                        {
                            append("(not ");
                            appendSource(negatedPattern.Negated);
                            append(")");
                            break;
                        }
                    case BoundListPattern listPattern:
                        {
                            append("[");
                            appendSourceItems(listPattern.Subpatterns);
                            append("]");
                            break;
                        }
                    case BoundSlicePattern slicePattern:
                        {
                            append("..");
                            if (slicePattern.Pattern is not null)
                            {
                                appendSource(slicePattern.Pattern);
                            }
                            break;
                        }
                    case BoundDiscardPattern:
                        {
                            append("_");
                            break;
                        }
                    case BoundTypePattern typePattern:
                        {
                            append(typePattern.DeclaredType.Type.ToString());
                            break;
                        }
                    case BoundRecursivePattern recursivePattern:
                        {
                            if (recursivePattern.DeclaredType is { } declaredType)
                            {
                                append(declaredType.Type.Name);
                                append(" ");
                            }

                            if (recursivePattern.Deconstruction is { IsDefault: false } deconstruction)
                            {
                                append("(");
                                appendSourceItems(deconstruction);
                                append(")");
                            }

                            if (recursivePattern.Properties is { IsDefault: false } properties)
                            {
                                append("{");
                                appendSourceItems(properties);
                                append("}");
                            }
                            break;
                        }
                    case BoundPositionalSubpattern positionalSubpattern:
                        {
                            appendSource(positionalSubpattern.Pattern);
                            break;
                        }
                    case BoundPropertySubpattern propertySubpattern:
                        {
                            append(propertySubpattern.Member?.Symbol?.Name);
                            append(": ");
                            appendSource(propertySubpattern.Pattern);
                            break;
                        }
                    case BoundDeclarationPattern declarationPattern:
                        {
                            if (declarationPattern.IsVar)
                            {
                                append("var ");
                            }
                            else
                            {
                                append(declarationPattern.DeclaredType.Type.Name);
                                append(" ");
                            }

                            append(declarationPattern.Variable?.Name);
                            break;
                        }
                    case BoundITuplePattern ituplePattern:
                        {
                            append("(");
                            appendSourceItems(ituplePattern.Subpatterns);
                            append(")");
                            break;
                        }
                    case BoundRelationalPattern relationalPattern:
                        {
                            string relation = (relationalPattern.Relation & BinaryOperatorKind.OpMask) switch
                            {
                                BinaryOperatorKind.GreaterThan => ">",
                                BinaryOperatorKind.GreaterThanOrEqual => ">=",
                                BinaryOperatorKind.LessThan => "<",
                                BinaryOperatorKind.LessThanOrEqual => "<=",
                                _ => relationalPattern.Relation.ToString()
                            };

                            append(relation);
                            append(" ");
                            appendConstantValue(relationalPattern.ConstantValue);
                            break;
                        }
                    case BoundObjectCreationExpression objectCreation:
                        {
                            append("new ");
                            append(objectCreation.Type.Name);

                            if (objectCreation.Type is NamedTypeSymbol { TypeArgumentsWithAnnotationsNoUseSiteDiagnostics: var typeArguments }
                                && typeArguments.Length > 0)
                            {
                                appendTypeArguments(typeArguments);
                            }

                            append("(");
                            appendSourceItems(objectCreation.Arguments);
                            append(")");
                            break;
                        }
                    case BoundSwitchDispatch switchDispatch:
                        {
                            append("switch dispatch(");
                            appendSource(switchDispatch.Expression);
                            appendLine(")");
                            appendLine("{");
                            incrementIndent();

                            foreach (var (value, label) in switchDispatch.Cases)
                            {
                                append("case ");
                                appendConstantValue(value);
                                append(" => ");
                                appendLine(label.ToString());
                            }

                            append("default => ");
                            appendLine(switchDispatch.DefaultLabel.ToString());
                            decrementIndent();
                            appendLine("}");
                            break;
                        }
                    case BoundPropertyAccess propertyAccess:
                        {
                            var receiver = propertyAccess.ReceiverOpt;
                            if (receiver != null)
                            {
                                appendSource(receiver);
                                append(".");
                            }
                            append(propertyAccess.PropertySymbol.Name);
                            break;
                        }
                    case BoundParameter parameter:
                        {
                            append(parameter.ParameterSymbol.Name);
                            break;
                        }
                    case BoundBaseReference baseReference:
                        {
                            append("base");
                            break;
                        }
                    case BoundPassByCopy passByCopy:
                        {
                            append("passByCopy ");
                            appendSource(passByCopy.Expression);
                            break;
                        }
                    case BoundAsOperator asOperator:
                        {
                            appendSource(asOperator.Operand);
                            append(" as ");
                            appendSource(asOperator.TargetType);
                            break;
                        }
                    default:
                        appendLine(node.Kind.ToString());
                        break;
                }

                void appendSource(BoundNode? n)
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

                void appendSourceItems<T>(ImmutableArray<T> nodes) where T : BoundNode
                {
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        if (i != 0) append(", ");
                        appendSource(nodes[i]);
                    }
                }

                void append(string? s)
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

                void appendConstantValue(ConstantValue? constantValueOpt)
                {
                    var value = constantValueOpt?.Value?.ToString();
                    if (value is null)
                    {
                        append("null");
                        return;
                    }

                    switch (constantValueOpt?.Discriminator)
                    {
                        case ConstantValueTypeDiscriminator.String:
                            append($@"""{value}""");
                            break;
                        default:
                            append(value);
                            break;
                    }
                }

                void appendTypeArguments(ImmutableArray<TypeWithAnnotations> typeArguments)
                {
                    append("<");
                    bool first = true;
                    foreach (var typeArgument in typeArguments)
                    {
                        if (!first)
                        {
                            append(", ");
                        }

                        first = false;
                        append(typeArgument.Type.Name); // Note: we should handle types more generally
                    }

                    append(">");
                }
            }
        }
#endif
    }
}
