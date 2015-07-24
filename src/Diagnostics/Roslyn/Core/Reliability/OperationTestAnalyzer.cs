// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.Performance
{
    /// <summary>Analyzer used to test for loop IOperations.</summary>
    public class BigForTestAnalyzer : DiagnosticAnalyzer
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
                                                         if (advanceOperationCode == BinaryOperatorCode.IntegerSubtract)
                                                         {
                                                             advanceOperationCode = BinaryOperatorCode.IntegerAdd;
                                                             incrementValue = -incrementValue;
                                                         }

                                                         if (advanceOperationCode == BinaryOperatorCode.IntegerAdd &&
                                                             incrementValue != 0 &&
                                                             (condition.RelationalCode == RelationalOperatorCode.IntegerLess ||
                                                              condition.RelationalCode == RelationalOperatorCode.IntegerLessEqual ||
                                                              condition.RelationalCode == RelationalOperatorCode.IntegerNotEqual ||
                                                              condition.RelationalCode == RelationalOperatorCode.IntegerGreater ||
                                                              condition.RelationalCode == RelationalOperatorCode.IntegerGreaterEqual))
                                                         {
                                                             int iterationCount = (testValue - initialValue) / incrementValue;
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

        static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }

    /// <summary>Analyzer used to test switch IOperations.</summary>
    public class SparseSwitchTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        internal static readonly DiagnosticDescriptor SparseSwitchDescriptor = new DiagnosticDescriptor(
            "OTA2",
            "Sparse switch",
            "Switch has less than one percept density",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(SparseSwitchDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     ISwitch switchOperation = (ISwitch)operationContext.Operation;
                     long minCaseValue = long.MaxValue;
                     long maxCaseValue = long.MinValue;
                     long caseValueCount = 0;
                     foreach (ICase switchCase in switchOperation.Cases)
                     {
                         foreach (ICaseClause clause in switchCase.Clauses)
                         {
                            switch (clause.CaseClass)
                             {
                                 case CaseKind.SingleValue:
                                     {
                                         ISingleValueCaseClause singleValueClause = (ISingleValueCaseClause)clause;
                                         IExpression singleValueExpression = singleValueClause.Value;
                                         if (singleValueExpression.ConstantValue != null &&
                                             singleValueExpression.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             int singleValue = (int)singleValueExpression.ConstantValue;
                                             caseValueCount += IncludeClause(singleValue, singleValue, ref minCaseValue, ref maxCaseValue);
                                         }
                                         else
                                         {
                                             return;
                                         }

                                         break;
                                     }
                                 case CaseKind.Range:
                                     {
                                         IRangeCaseClause rangeClause = (IRangeCaseClause)clause;
                                         IExpression rangeMinExpression = rangeClause.MinimumValue;
                                         IExpression rangeMaxExpression = rangeClause.MaximumValue;
                                         if (rangeMinExpression.ConstantValue != null &&
                                             rangeMinExpression.ResultType.SpecialType == SpecialType.System_Int32 &&
                                             rangeMaxExpression.ConstantValue != null &&
                                             rangeMaxExpression.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             int rangeMinValue = (int)rangeMinExpression.ConstantValue;
                                             int rangeMaxValue = (int)rangeMaxExpression.ConstantValue;
                                             caseValueCount += IncludeClause(rangeMinValue, rangeMaxValue, ref minCaseValue, ref maxCaseValue);
                                         }
                                         else
                                         {
                                             return;
                                         }

                                         break;
                                     }
                                 case CaseKind.Relational:
                                     {
                                         IRelationalCaseClause relationalClause = (IRelationalCaseClause)clause;
                                         IExpression relationalValueExpression = relationalClause.Value;
                                         if (relationalValueExpression.ConstantValue != null &&
                                             relationalValueExpression.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             int rangeMinValue = int.MaxValue;
                                             int rangeMaxValue = int.MinValue;
                                             int relationalValue = (int)relationalValueExpression.ConstantValue;
                                             switch (relationalClause.Relation)
                                             {
                                                 case RelationalOperatorCode.IntegerEqual:
                                                     rangeMinValue = relationalValue;
                                                     rangeMaxValue = relationalValue;
                                                     break;
                                                 case RelationalOperatorCode.IntegerNotEqual:
                                                     return;
                                                 case RelationalOperatorCode.IntegerLess:
                                                     rangeMinValue = int.MinValue;
                                                     rangeMaxValue = relationalValue - 1;
                                                     break;
                                                 case RelationalOperatorCode.IntegerLessEqual:
                                                     rangeMinValue = int.MinValue;
                                                     rangeMaxValue = relationalValue;
                                                     break;
                                                 case RelationalOperatorCode.IntegerGreaterEqual:
                                                     rangeMinValue = relationalValue;
                                                     rangeMaxValue = int.MaxValue;
                                                     break;
                                                 case RelationalOperatorCode.IntegerGreater:
                                                     rangeMinValue = relationalValue + 1;
                                                     rangeMaxValue = int.MaxValue;
                                                     break;
                                             }

                                             caseValueCount += IncludeClause(rangeMinValue, rangeMaxValue, ref minCaseValue, ref maxCaseValue);
                                         }
                                         else
                                         {
                                             return;
                                         }

                                         break;
                                     }
                                 case CaseKind.Default:
                                     {
                                         break;
                                     }
                             }
                         }
                     }

                     long span = maxCaseValue - minCaseValue + 1;
                     if (caseValueCount == 0 || span / caseValueCount > 100)
                     {
                         Report(operationContext, switchOperation.Syntax, SparseSwitchDescriptor);
                     }
                 },
                 OperationKind.SwitchStatement);
        }

        static int IncludeClause(int clauseMinValue, int clauseMaxValue, ref long minCaseValue, ref long maxCaseValue)
        {
            if (clauseMinValue < minCaseValue)
            {
                minCaseValue = clauseMinValue;
            }

            if (clauseMaxValue > maxCaseValue)
            {
                maxCaseValue = clauseMaxValue;
            }

            return clauseMaxValue - clauseMinValue + 1;
        }

        void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }
}