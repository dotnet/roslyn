// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private enum SyntaxType
        {
            Unspecified,
            Statement,
            Expression,
        }

        public SyntaxNode? TryGenerate(IOperation? operation)
            => TryGenerate(operation, SyntaxType.Unspecified);

        public StatementSyntax? TryGenerateStatement(IOperation? operation)
            => (StatementSyntax?)TryGenerate(operation, SyntaxType.Statement);

        public ExpressionSyntax? TryGenerateExpression(IOperation? operation)
            => (ExpressionSyntax?)TryGenerate(operation, SyntaxType.Expression);

        private SyntaxNode? TryGenerate(IOperation? operation, SyntaxType type)
        {
            if (operation == null)
                return null;

            switch (operation.Kind)
            {
                case OperationKind.None:
                    break;
                case OperationKind.Invalid:
                    break;
                case OperationKind.Block:
                    return TryGenerateBlock((IBlockOperation)operation, type);
                case OperationKind.VariableDeclarationGroup:
                    break;
                case OperationKind.Switch:
                    break;
                case OperationKind.Loop:
                    break;
                case OperationKind.Labeled:
                    return TryGenerateLabeledStatement((ILabeledOperation)operation, type);
                case OperationKind.Branch:
                    break;
                case OperationKind.Empty:
                    return TryGenerateEmptyStatement((IEmptyOperation)operation, type);
                case OperationKind.Return:
                case OperationKind.YieldBreak:
                case OperationKind.YieldReturn:
                    return TryGenerateReturnOrYieldStatement((IReturnOperation)operation, type);
                case OperationKind.Lock:
                    break;
                case OperationKind.Try:
                    break;
                case OperationKind.Using:
                    break;
                case OperationKind.ExpressionStatement:
                    break;
                case OperationKind.LocalFunction:
                    break;
                case OperationKind.Stop:
                    break;
                case OperationKind.End:
                    break;
                case OperationKind.RaiseEvent:
                    break;
                case OperationKind.Literal:
                    return TryGenerateLiteralExpression((ILiteralOperation)operation, type);
                case OperationKind.Conversion:
                    break;
                case OperationKind.Invocation:
                    break;
                case OperationKind.ArrayElementReference:
                    break;
                case OperationKind.LocalReference:
                    return TryGenerateLocalReference((ILocalReferenceOperation)operation, type);
                case OperationKind.ParameterReference:
                    break;
                case OperationKind.FieldReference:
                    break;
                case OperationKind.MethodReference:
                    break;
                case OperationKind.PropertyReference:
                    break;
                case OperationKind.EventReference:
                    break;
                case OperationKind.Unary:
                    break;
                case OperationKind.Binary:
                    break;
                case OperationKind.Conditional:
                    break;
                case OperationKind.Coalesce:
                    break;
                case OperationKind.AnonymousFunction:
                    break;
                case OperationKind.ObjectCreation:
                    break;
                case OperationKind.TypeParameterObjectCreation:
                    break;
                case OperationKind.ArrayCreation:
                    break;
                case OperationKind.InstanceReference:
                    break;
                case OperationKind.IsType:
                    break;
                case OperationKind.Await:
                    break;
                case OperationKind.SimpleAssignment:
                    break;
                case OperationKind.CompoundAssignment:
                    break;
                case OperationKind.Parenthesized:
                    break;
                case OperationKind.EventAssignment:
                    break;
                case OperationKind.ConditionalAccess:
                    break;
                case OperationKind.ConditionalAccessInstance:
                    break;
                case OperationKind.InterpolatedString:
                    break;
                case OperationKind.AnonymousObjectCreation:
                    break;
                case OperationKind.ObjectOrCollectionInitializer:
                    break;
                case OperationKind.MemberInitializer:
                    break;
                case OperationKind.NameOf:
                    break;
                case OperationKind.Tuple:
                    break;
                case OperationKind.DynamicObjectCreation:
                    break;
                case OperationKind.DynamicMemberReference:
                    break;
                case OperationKind.DynamicInvocation:
                    break;
                case OperationKind.DynamicIndexerAccess:
                    break;
                case OperationKind.TranslatedQuery:
                    break;
                case OperationKind.DelegateCreation:
                    break;
                case OperationKind.DefaultValue:
                    break;
                case OperationKind.TypeOf:
                    break;
                case OperationKind.SizeOf:
                    break;
                case OperationKind.AddressOf:
                    break;
                case OperationKind.IsPattern:
                    break;
                case OperationKind.Increment:
                    break;
                case OperationKind.Throw:
                    break;
                case OperationKind.Decrement:
                    break;
                case OperationKind.DeconstructionAssignment:
                    break;
                case OperationKind.DeclarationExpression:
                    break;
                case OperationKind.OmittedArgument:
                    break;
                case OperationKind.FieldInitializer:
                    break;
                case OperationKind.VariableInitializer:
                    break;
                case OperationKind.PropertyInitializer:
                    break;
                case OperationKind.ParameterInitializer:
                    break;
                case OperationKind.ArrayInitializer:
                    break;
                case OperationKind.VariableDeclarator:
                    break;
                case OperationKind.VariableDeclaration:
                    break;
                case OperationKind.Argument:
                    break;
                case OperationKind.CatchClause:
                    break;
                case OperationKind.SwitchCase:
                    break;
                case OperationKind.CaseClause:
                    break;
                case OperationKind.InterpolatedStringText:
                    break;
                case OperationKind.Interpolation:
                    break;
                case OperationKind.ConstantPattern:
                    break;
                case OperationKind.DeclarationPattern:
                    break;
                case OperationKind.TupleBinary:
                    break;
                case OperationKind.MethodBody:
                    break;
                case OperationKind.ConstructorBody:
                    break;
                case OperationKind.Discard:
                    break;
                case OperationKind.FlowCapture:
                    break;
                case OperationKind.FlowCaptureReference:
                    break;
                case OperationKind.IsNull:
                    break;
                case OperationKind.CaughtException:
                    break;
                case OperationKind.StaticLocalInitializationSemaphore:
                    break;
                case OperationKind.FlowAnonymousFunction:
                    break;
                case OperationKind.CoalesceAssignment:
                    break;
                case OperationKind.Range:
                    break;
                case OperationKind.ReDim:
                    break;
                case OperationKind.ReDimClause:
                    break;
                case OperationKind.RecursivePattern:
                    break;
                case OperationKind.DiscardPattern:
                    break;
                case OperationKind.SwitchExpression:
                    break;
                case OperationKind.SwitchExpressionArm:
                    break;
                case OperationKind.PropertySubpattern:
                    break;
                case OperationKind.UsingDeclaration:
                    break;
                case OperationKind.NegatedPattern:
                    break;
                case OperationKind.BinaryPattern:
                    break;
                case OperationKind.TypePattern:
                    break;
                case OperationKind.RelationalPattern:
                    break;
                default:
                    break;
            }

            throw new NotImplementedException();
        }
    }
}
