// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.Performance
{
    /// <summary>Analyzer used to test IOperation.</summary>
    public class OperationTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        internal static readonly DiagnosticDescriptor BigForDescriptor = new DiagnosticDescriptor(
            "OTA1",
            "Big For Loop",
            "For loop iterates more than one million times",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(BigForDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction((compilationContext) =>
            {
                INamedTypeSymbol booleanType = compilationContext.Compilation.GetSpecialType(SpecialType.System_Boolean);

                if (booleanType != null)
                {

                }
            });

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     ILoop loop = (ILoop)operationContext.Operation;
                     if (loop.LoopClass == LoopKind.For)
                     {
                         IFor forLoop = (IFor)loop;
                         IExpression forCondition = forLoop.Condition;

                         if (forCondition.Kind == OperationKind.RelationalOperator)
                         {
                             IRelational condition = (IRelational)forCondition;
                             IExpression conditionLeft = condition.Left;
                             IExpression conditionRight = condition.Right;

                             if (conditionRight.ConstantValue != null &&
                                 conditionRight.ResultType.SpecialType == SpecialType.System_Int32 &&
                                 conditionLeft.Kind == OperationKind.LocalReference)
                             {
                                 // Test is known to be a comparison of a local against a constant.

                                 int testValue = (int)conditionRight.ConstantValue;
                                 ILocalSymbol testVariable = ((ILocalReference)conditionLeft).Local;

                                 if (forLoop.Before.Length == 1)
                                 {
                                     IStatement setup = forLoop.Before[0];
                                     if (setup.Kind == OperationKind.ExpressionStatement && ((IExpressionStatement)setup).Expression.Kind == OperationKind.Assignment)
                                     {
                                         IAssignment setupAssignment = (IAssignment)((IExpressionStatement)setup).Expression;
                                         if (setupAssignment.Target.Kind == OperationKind.LocalReference &&
                                             ((ILocalReference)setupAssignment.Target).Local == testVariable &&
                                             setupAssignment.Value.ConstantValue != null &&
                                             setupAssignment.Value.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             // Setup is known to be an assignment of a constant to the local used in the test.

                                             int initialValue = (int)setupAssignment.Value.ConstantValue;

                                             if (forLoop.AtLoopBottom.Length == 1)
                                             {
                                                 IStatement advance = forLoop.AtLoopBottom[0];
                                                 if (advance.Kind == OperationKind.ExpressionStatement)
                                                 {
                                                     IExpression advanceExpression = ((IExpressionStatement)advance).Expression;
                                                     IExpression advanceIncrement = null;
                                                     BinaryOperatorCode advanceOperationCode = BinaryOperatorCode.None;

                                                     if (advanceExpression.Kind == OperationKind.Assignment)
                                                     {
                                                         IAssignment advanceAssignment = (IAssignment)advanceExpression;

                                                         if (advanceAssignment.Target.Kind == OperationKind.LocalReference &&
                                                             ((ILocalReference)advanceAssignment.Target).Local == testVariable &&
                                                             advanceAssignment.Value.Kind == OperationKind.BinaryOperator &&
                                                             advanceAssignment.Value.ResultType.SpecialType == SpecialType.System_Int32)
                                                         {
                                                             // Advance is known to be an assignment of a binary operation to the local used in the test.

                                                             IBinary advanceOperation = (IBinary)advanceAssignment.Value;
                                                             if (!advanceOperation.UsesOperatorMethod &&
                                                                 advanceOperation.Left.Kind == OperationKind.LocalReference &&
                                                                 ((ILocalReference)advanceOperation.Left).Local == testVariable &&
                                                                 advanceOperation.Right.ConstantValue != null &&
                                                                 advanceOperation.Right.ResultType.SpecialType == SpecialType.System_Int32)
                                                             {
                                                                 // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                                                                 advanceIncrement = advanceOperation.Right;
                                                                 advanceOperationCode = advanceOperation.Operation;
                                                             }
                                                         }
                                                     }
                                                     else if (advanceExpression.Kind == OperationKind.CompoundAssignment || advanceExpression.Kind == OperationKind.Increment)
                                                     {
                                                         ICompoundAssignment advanceAssignment = (ICompoundAssignment)advanceExpression;

                                                         if (advanceAssignment.Target.Kind == OperationKind.LocalReference &&
                                                             ((ILocalReference)advanceAssignment.Target).Local == testVariable &&
                                                             advanceAssignment.Value.ConstantValue != null &&
                                                             advanceAssignment.Value.ResultType.SpecialType == SpecialType.System_Int32)
                                                         {
                                                             // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                                                             advanceIncrement = advanceAssignment.Value;
                                                             advanceOperationCode = advanceAssignment.Operation;
                                                         }
                                                     }

                                                     if (advanceIncrement != null)
                                                     {
                                                         int incrementValue = (int)advanceIncrement.ConstantValue;
                                                         if ((advanceOperationCode == BinaryOperatorCode.IntegerAdd &&
                                                              (condition.RelationalCode == RelationalOperatorCode.IntegerLess ||
                                                               condition.RelationalCode == RelationalOperatorCode.IntegerLessEqual ||
                                                               condition.RelationalCode == RelationalOperatorCode.IntegerNotEqual)) ||
                                                             (advanceOperationCode == BinaryOperatorCode.IntegerSubtract &&
                                                              (condition.RelationalCode == RelationalOperatorCode.IntegerGreater ||
                                                               condition.RelationalCode == RelationalOperatorCode.IntegerGreaterEqual ||
                                                               condition.RelationalCode == RelationalOperatorCode.IntegerNotEqual)))
                                                         {
                                                             int iterationCount = Abs(testValue - initialValue) / incrementValue;
                                                             if (iterationCount >= 1000000)
                                                             {
                                                                 Report(operationContext, forLoop.Syntax, BigForDescriptor);
                                                             }
                                                         }
                                                     }
                                                 }
                                             }
                                         }
                                     }
                                 }
                             }
                         }
                     }
                 },
                 OperationKind.LoopStatement);
        }

        internal static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        internal void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }
}