// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    // These analyzers are not intended for any actual use. They exist solely to test IOperation support.

    /// <summary>Analyzer used to test for bad statements and expressions.</summary>
    public class BadStuffTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor InvalidExpressionDescriptor = new DiagnosticDescriptor(
            "InvalidExpression",
            "Invalid Expression",
            "Invalid expression found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidStatementDescriptor = new DiagnosticDescriptor(
            "InvalidStatement",
            "Invalid Statement",
            "Invalid statement found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor IsInvalidDescriptor = new DiagnosticDescriptor(
            "IsInvalid",
            "Is Invalid",
            "Operation found that is invalid.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(InvalidExpressionDescriptor, InvalidStatementDescriptor, IsInvalidDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(InvalidExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.InvalidExpression);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(InvalidStatementDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.InvalidStatement);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     if (operationContext.Operation.IsInvalid)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(IsInvalidDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.InvocationExpression,
                 OperationKind.InvalidExpression,
                 OperationKind.InvalidStatement);
        }
    }

    /// <summary>Analyzer used to test for loop IOperations.</summary>
    public class BigForTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor BigForDescriptor = new DiagnosticDescriptor(
            "BigForRule",
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
                     ILoopStatement loop = (ILoopStatement)operationContext.Operation;
                     if (loop.LoopKind == LoopKind.For)
                     {
                         IForLoopStatement forLoop = (IForLoopStatement)loop;
                         IExpression forCondition = forLoop.Condition;

                         if (forCondition.Kind == OperationKind.BinaryOperatorExpression)
                         {
                             IBinaryOperatorExpression condition = (IBinaryOperatorExpression)forCondition;
                             IExpression conditionLeft = condition.Left;
                             IExpression conditionRight = condition.Right;

                             if (conditionRight.ConstantValue.HasValue &&
                                 conditionRight.ResultType.SpecialType == SpecialType.System_Int32 &&
                                 conditionLeft.Kind == OperationKind.LocalReferenceExpression)
                             {
                                 // Test is known to be a comparison of a local against a constant.

                                 int testValue = (int)conditionRight.ConstantValue.Value;
                                 ILocalSymbol testVariable = ((ILocalReferenceExpression)conditionLeft).Local;

                                 if (forLoop.Before.Length == 1)
                                 {
                                     IStatement setup = forLoop.Before[0];
                                     if (setup.Kind == OperationKind.ExpressionStatement && ((IExpressionStatement)setup).Expression.Kind == OperationKind.AssignmentExpression)
                                     {
                                         IAssignmentExpression setupAssignment = (IAssignmentExpression)((IExpressionStatement)setup).Expression;
                                         if (setupAssignment.Target.Kind == OperationKind.LocalReferenceExpression &&
                                             ((ILocalReferenceExpression)setupAssignment.Target).Local == testVariable &&
                                             setupAssignment.Value.ConstantValue.HasValue &&
                                             setupAssignment.Value.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             // Setup is known to be an assignment of a constant to the local used in the test.

                                             int initialValue = (int)setupAssignment.Value.ConstantValue.Value;

                                             if (forLoop.AtLoopBottom.Length == 1)
                                             {
                                                 IStatement advance = forLoop.AtLoopBottom[0];
                                                 if (advance.Kind == OperationKind.ExpressionStatement)
                                                 {
                                                     IExpression advanceExpression = ((IExpressionStatement)advance).Expression;
                                                     IExpression advanceIncrement = null;
                                                     BinaryOperationKind advanceOperationCode = BinaryOperationKind.None;

                                                     if (advanceExpression.Kind == OperationKind.AssignmentExpression)
                                                     {
                                                         IAssignmentExpression advanceAssignment = (IAssignmentExpression)advanceExpression;

                                                         if (advanceAssignment.Target.Kind == OperationKind.LocalReferenceExpression &&
                                                             ((ILocalReferenceExpression)advanceAssignment.Target).Local == testVariable &&
                                                             advanceAssignment.Value.Kind == OperationKind.BinaryOperatorExpression &&
                                                             advanceAssignment.Value.ResultType.SpecialType == SpecialType.System_Int32)
                                                         {
                                                             // Advance is known to be an assignment of a binary operation to the local used in the test.

                                                             IBinaryOperatorExpression advanceOperation = (IBinaryOperatorExpression)advanceAssignment.Value;
                                                             if (!advanceOperation.UsesOperatorMethod &&
                                                                 advanceOperation.Left.Kind == OperationKind.LocalReferenceExpression &&
                                                                 ((ILocalReferenceExpression)advanceOperation.Left).Local == testVariable &&
                                                                 advanceOperation.Right.ConstantValue.HasValue &&
                                                                 advanceOperation.Right.ResultType.SpecialType == SpecialType.System_Int32)
                                                             {
                                                                 // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                                                                 advanceIncrement = advanceOperation.Right;
                                                                 advanceOperationCode = advanceOperation.BinaryOperationKind;
                                                             }
                                                         }
                                                     }
                                                     else if (advanceExpression.Kind == OperationKind.CompoundAssignmentExpression || advanceExpression.Kind == OperationKind.IncrementExpression)
                                                     {
                                                         ICompoundAssignmentExpression advanceAssignment = (ICompoundAssignmentExpression)advanceExpression;

                                                         if (advanceAssignment.Target.Kind == OperationKind.LocalReferenceExpression &&
                                                             ((ILocalReferenceExpression)advanceAssignment.Target).Local == testVariable &&
                                                             advanceAssignment.Value.ConstantValue.HasValue &&
                                                             advanceAssignment.Value.ResultType.SpecialType == SpecialType.System_Int32)
                                                         {
                                                             // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                                                             advanceIncrement = advanceAssignment.Value;
                                                             advanceOperationCode = advanceAssignment.BinaryKind;
                                                         }
                                                     }

                                                     if (advanceIncrement != null)
                                                     {
                                                         int incrementValue = (int)advanceIncrement.ConstantValue.Value;
                                                         if (advanceOperationCode == BinaryOperationKind.IntegerSubtract)
                                                         {
                                                             advanceOperationCode = BinaryOperationKind.IntegerAdd;
                                                             incrementValue = -incrementValue;
                                                         }

                                                         if (advanceOperationCode == BinaryOperationKind.IntegerAdd &&
                                                             incrementValue != 0 &&
                                                             (condition.BinaryOperationKind == BinaryOperationKind.IntegerLessThan ||
                                                              condition.BinaryOperationKind == BinaryOperationKind.IntegerLessThanOrEqual ||
                                                              condition.BinaryOperationKind == BinaryOperationKind.IntegerNotEquals ||
                                                              condition.BinaryOperationKind == BinaryOperationKind.IntegerGreaterThan ||
                                                              condition.BinaryOperationKind == BinaryOperationKind.IntegerGreaterThanOrEqual))
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
    public class SwitchTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor SparseSwitchDescriptor = new DiagnosticDescriptor(
            "SparseSwitchRule",
            "Sparse switch",
            "Switch has less than one percept density",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NoDefaultSwitchDescriptor = new DiagnosticDescriptor(
            "NoDefaultSwitchRule",
            "No default switch",
            "Switch has no default case",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor OnlyDefaultSwitchDescriptor = new DiagnosticDescriptor(
            "OnlyDefaultSwitchRule",
            "Only default switch",
            "Switch only has a default case",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(SparseSwitchDescriptor,
                                              NoDefaultSwitchDescriptor,
                                              OnlyDefaultSwitchDescriptor);
            }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     ISwitchStatement switchOperation = (ISwitchStatement)operationContext.Operation;
                     long minCaseValue = long.MaxValue;
                     long maxCaseValue = long.MinValue;
                     long caseValueCount = 0;
                     bool hasDefault = false;
                     bool hasNonDefault = false;
                     foreach (ICase switchCase in switchOperation.Cases)
                     {
                         foreach (ICaseClause clause in switchCase.Clauses)
                         {
                             switch (clause.CaseKind)
                             {
                                 case CaseKind.SingleValue:
                                     {
                                         hasNonDefault = true;
                                         ISingleValueCaseClause singleValueClause = (ISingleValueCaseClause)clause;
                                         IExpression singleValueExpression = singleValueClause.Value;
                                         if (singleValueExpression != null &&
                                             singleValueExpression.ConstantValue.HasValue &&
                                             singleValueExpression.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             int singleValue = (int)singleValueExpression.ConstantValue.Value;
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
                                         hasNonDefault = true;
                                         IRangeCaseClause rangeClause = (IRangeCaseClause)clause;
                                         IExpression rangeMinExpression = rangeClause.MinimumValue;
                                         IExpression rangeMaxExpression = rangeClause.MaximumValue;
                                         if (rangeMinExpression != null &&
                                             rangeMinExpression.ConstantValue.HasValue &&
                                             rangeMinExpression.ResultType.SpecialType == SpecialType.System_Int32 &&
                                             rangeMaxExpression != null &&
                                             rangeMaxExpression.ConstantValue.HasValue &&
                                             rangeMaxExpression.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             int rangeMinValue = (int)rangeMinExpression.ConstantValue.Value;
                                             int rangeMaxValue = (int)rangeMaxExpression.ConstantValue.Value;
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
                                         hasNonDefault = true;
                                         IRelationalCaseClause relationalClause = (IRelationalCaseClause)clause;
                                         IExpression relationalValueExpression = relationalClause.Value;
                                         if (relationalValueExpression != null &&
                                             relationalValueExpression.ConstantValue.HasValue &&
                                             relationalValueExpression.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             int rangeMinValue = int.MaxValue;
                                             int rangeMaxValue = int.MinValue;
                                             int relationalValue = (int)relationalValueExpression.ConstantValue.Value;
                                             switch (relationalClause.Relation)
                                             {
                                                 case BinaryOperationKind.IntegerEquals:
                                                     rangeMinValue = relationalValue;
                                                     rangeMaxValue = relationalValue;
                                                     break;
                                                 case BinaryOperationKind.IntegerNotEquals:
                                                     return;
                                                 case BinaryOperationKind.IntegerLessThan:
                                                     rangeMinValue = int.MinValue;
                                                     rangeMaxValue = relationalValue - 1;
                                                     break;
                                                 case BinaryOperationKind.IntegerLessThanOrEqual:
                                                     rangeMinValue = int.MinValue;
                                                     rangeMaxValue = relationalValue;
                                                     break;
                                                 case BinaryOperationKind.IntegerGreaterThanOrEqual:
                                                     rangeMinValue = relationalValue;
                                                     rangeMaxValue = int.MaxValue;
                                                     break;
                                                 case BinaryOperationKind.IntegerGreaterThan:
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
                                         hasDefault = true;
                                         break;
                                     }
                             }
                         }
                     }

                     long span = maxCaseValue - minCaseValue + 1;
                     if (caseValueCount == 0 && !hasDefault ||
                         caseValueCount != 0 && span / caseValueCount > 100)
                     {
                         Report(operationContext, switchOperation.Value.Syntax, SparseSwitchDescriptor);
                     }
                     if (!hasDefault)
                     {
                         Report(operationContext, switchOperation.Value.Syntax, NoDefaultSwitchDescriptor);
                     }
                     if (hasDefault && !hasNonDefault)
                     {
                         Report(operationContext, switchOperation.Value.Syntax, OnlyDefaultSwitchDescriptor);
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

    /// <summary>Analyzer used to test invocaton IOperations.</summary>
    public class InvocationTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor BigParamArrayArgumentsDescriptor = new DiagnosticDescriptor(
            "BigParamarrayRule",
            "Big Paramarray",
            "Paramarray has more than 10 elements",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor OutOfNumericalOrderArgumentsDescriptor = new DiagnosticDescriptor(
            "OutOfOrderArgumentsRule",
            "Out of order arguments",
            "Argument values are not in increasing order",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UseDefaultArgumentDescriptor = new DiagnosticDescriptor(
            "UseDefaultArgument",
            "Use default argument",
            "Invocation uses default argument {0}",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(BigParamArrayArgumentsDescriptor,
                                             OutOfNumericalOrderArgumentsDescriptor,
                                             UseDefaultArgumentDescriptor);
            }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IInvocationExpression invocation = (IInvocationExpression)operationContext.Operation;
                     long priorArgumentValue = long.MinValue;
                     foreach (IArgument argument in invocation.ArgumentsInParameterOrder)
                     {
                         if (argument.ArgumentKind == ArgumentKind.DefaultValue)
                         {
                             operationContext.ReportDiagnostic(Diagnostic.Create(UseDefaultArgumentDescriptor, invocation.Syntax.GetLocation(), argument.Parameter.Name));
                         }

                         TestAscendingArgument(operationContext, argument.Value, ref priorArgumentValue);

                         if (argument.ArgumentKind == ArgumentKind.ParamArray)
                         {
                             IArrayCreationExpression arrayArgument = argument.Value as IArrayCreationExpression;
                             if (arrayArgument != null)
                             {
                                 var initializer = arrayArgument.Initializer;
                                 if (initializer != null)
                                 {
                                     if (initializer.ElementValues.Length > 10)
                                     {
                                         Report(operationContext, invocation.Syntax, BigParamArrayArgumentsDescriptor);
                                     }

                                     foreach (IExpression element in initializer.ElementValues)
                                     {
                                         TestAscendingArgument(operationContext, element, ref priorArgumentValue);
                                     }
                                 }
                             }
                         }
                     }
                 },
                 OperationKind.InvocationExpression);
        }

        private static void TestAscendingArgument(OperationAnalysisContext operationContext, IExpression argument, ref long priorArgumentValue)
        {
            Optional<object> argumentValue = argument.ConstantValue;
            if (argumentValue.HasValue && argument.ResultType.SpecialType == SpecialType.System_Int32)
            {
                int integerArgument = (int)argumentValue.Value;
                if (integerArgument < priorArgumentValue)
                {
                    Report(operationContext, argument.Syntax, OutOfNumericalOrderArgumentsDescriptor);
                }

                priorArgumentValue = integerArgument;
            }
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }

    /// <summary>Analyzer used to test various contexts in which IOperations can occur.</summary>
    public class SeventeenTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor SeventeenDescriptor = new DiagnosticDescriptor(
            "SeventeenRule",
            "Seventeen",
            "Seventeen is a recognized value",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(SeventeenDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     ILiteralExpression literal = (ILiteralExpression)operationContext.Operation;
                     if (literal.ResultType.SpecialType == SpecialType.System_Int32 &&
                         literal.ConstantValue.HasValue &&
                         (int)literal.ConstantValue.Value == 17)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(SeventeenDescriptor, literal.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.LiteralExpression);
        }
    }

    /// <summary>Analyzer used to test IArgument IOperations.</summary>
    public class NullArgumentTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor NullArgumentsDescriptor = new DiagnosticDescriptor(
            "NullArgumentRule",
            "Null Argument",
            "Value of the argument is null",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(NullArgumentsDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var argument = (IArgument)operationContext.Operation;
                     if (argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value == null)
                     {
                         Report(operationContext, argument.Syntax, NullArgumentsDescriptor);
                     }
                 },
                 OperationKind.Argument);
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }

    /// <summary>Analyzer used to test IMemberInitializer IOperations.</summary>
    public class MemberInitializerTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor DoNotUseFieldInitializerDescriptor = new DiagnosticDescriptor(
            "DoNotUseFieldInitializer",
            "Do Not Use Field Initializer",
            "a field initializer is used for object creation",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DoNotUsePropertyInitializerDescriptor = new DiagnosticDescriptor(
            "DoNotUsePropertyInitializer",
            "Do Not Use Property Initializer",
            "A property initializer is used for object creation",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DoNotUseFieldInitializerDescriptor, DoNotUsePropertyInitializerDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var initializer = (ISymbolInitializer)operationContext.Operation;
                     Report(operationContext, initializer.Syntax, initializer.Kind == OperationKind.FieldInitializerInCreation ? DoNotUseFieldInitializerDescriptor : DoNotUsePropertyInitializerDescriptor);
                 },
                 OperationKind.FieldInitializerInCreation,
                 OperationKind.PropertyInitializerInCreation);
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }

    /// <summary>Analyzer used to test IAssignmentExpression IOperations.</summary>
    public class AssignmentTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Reliability".</summary>
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor DoNotUseMemberAssignmentDescriptor = new DiagnosticDescriptor(
            "DoNotUseMemberAssignment",
            "Do Not Use Member Assignment",
            "Do not assign values to object members",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
        
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DoNotUseMemberAssignmentDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var assignment = (IAssignmentExpression)operationContext.Operation;
                     var kind = assignment.Target.Kind;
                     if (kind == OperationKind.FieldReferenceExpression ||
                         kind == OperationKind.PropertyReferenceExpression)
                     {
                         Report(operationContext, assignment.Syntax, DoNotUseMemberAssignmentDescriptor);
                     }
                 },
                 OperationKind.AssignmentExpression);
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }

    /// <summary>Analyzer used to test IArrayInitializer IOperations.</summary>
    public class ArrayInitializerTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Maintainability".</summary>
        private const string Maintainability = "Maintainability";

        public static readonly DiagnosticDescriptor DoNotUseLargeListOfArrayInitializersDescriptor = new DiagnosticDescriptor(
            "DoNotUseLongListToInitializeArray",
            "Do not use long list to initialize array",
            "a list of more than 5 elements is used for an array initialization",
            Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DoNotUseLargeListOfArrayInitializersDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var initializer = (IArrayInitializer)operationContext.Operation;
                     if (initializer.ElementValues.Length > 5)
                     {
                         Report(operationContext, initializer.Syntax, DoNotUseLargeListOfArrayInitializersDescriptor);
                     }
                 },
                 OperationKind.ArrayInitializer);
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }

    /// <summary>Analyzer used to test IVariableDeclarationStatement IOperations.</summary>
    public class VariableDeclarationTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Maintainability".</summary>
        private const string Maintainability = "Maintainability";

        public static readonly DiagnosticDescriptor TooManyLocalVarDeclarationsDescriptor = new DiagnosticDescriptor(
            "TooManyLocalVarDeclarations",
            "Too many local variable declarations",
            "A declaration statement shouldn't have more than 3 variable declarations",
            Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor LocalVarInitializedDeclarationDescriptor = new DiagnosticDescriptor(
            "LocalVarInitializedDeclaration",
            "Local var initialized at declaration",
            "A local variable is initialized at declaration.",
            Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(TooManyLocalVarDeclarationsDescriptor, LocalVarInitializedDeclarationDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var declarationStatement = (IVariableDeclarationStatement)operationContext.Operation;
                     if (declarationStatement.Variables.Length > 3)
                     {
                         Report(operationContext, declarationStatement.Syntax, TooManyLocalVarDeclarationsDescriptor);
                     }

                     foreach (var decl in declarationStatement.Variables)
                     {
                         if (decl.InitialValue != null && !decl.InitialValue.IsInvalid)
                         {
                             Report(operationContext, decl.Syntax, LocalVarInitializedDeclarationDescriptor);
                         }
                     }
                 },
                 OperationKind.VariableDeclarationStatement);
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }

    /// <summary>Analyzer used to test ICase and ICaseClause.</summary>
    public class CaseTestAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Maintainability".</summary>
        private const string Maintainability = "Maintainability";

        public static readonly DiagnosticDescriptor HasDefaultCaseDescriptor = new DiagnosticDescriptor(
            "HasDefaultCase",
            "Has Default Case",
            "A default case clause is encountered",
            Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleCaseClausesDescriptor = new DiagnosticDescriptor(
            "MultipleCaseClauses",
            "Multiple Case Clauses",
            "A switch section has multiple case clauses",
            Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(HasDefaultCaseDescriptor, MultipleCaseClausesDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     switch (operationContext.Operation.Kind)
                     {
                         case OperationKind.SingleValueCaseClause:
                         case OperationKind.RelationalCaseClause:
                         case OperationKind.RangeCaseClause:
                             var caseClause = (ICaseClause)operationContext.Operation;
                             if (caseClause.CaseKind == CaseKind.Default)
                             {
                                 Report(operationContext, caseClause.Syntax, HasDefaultCaseDescriptor);
                             }
                             break;
                         case OperationKind.SwitchSection:
                             var switchSection = (ICase)operationContext.Operation;
                             if (!switchSection.IsInvalid && switchSection.Clauses.Length > 1)
                             {
                                 Report(operationContext, switchSection.Syntax, MultipleCaseClausesDescriptor);
                             }
                             break;
                     }
                 },
                 OperationKind.SwitchSection,
                 OperationKind.SingleValueCaseClause,
                 OperationKind.RangeCaseClause,
                 OperationKind.RelationalCaseClause);
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }
    
    /// <summary>Analyzer used to test for explicit vs. implicit instance references.</summary>
    public class ExplicitVsImplicitInstanceAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor ImplicitInstanceDescriptor = new DiagnosticDescriptor(
            "ImplicitInstance",
            "Implicit Instance",
            "Implicit instance found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ExplicitInstanceDescriptor = new DiagnosticDescriptor(
            "ExplicitInstance",
            "Explicit Instance",
            "Explicit instance found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ImplicitInstanceDescriptor, ExplicitInstanceDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IInstanceReferenceExpression instanceReference = (IInstanceReferenceExpression)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(instanceReference.IsExplicit? ExplicitInstanceDescriptor : ImplicitInstanceDescriptor, instanceReference.Syntax.GetLocation()));
                 },
                 OperationKind.InstanceReferenceExpression,
                 OperationKind.BaseClassInstanceReferenceExpression);
        }
    }

    /// <summary>Analyzer used to test for member references.</summary>
    public class MemberReferenceAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor EventReferenceDescriptor = new DiagnosticDescriptor(
            "EventReference",
            "Event Reference",
            "Event reference found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor HandlerAddedDescriptor = new DiagnosticDescriptor(
            "HandlerAdded",
            "Handler Added",
            "Event handler added.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor HandlerRemovedDescriptor = new DiagnosticDescriptor(
            "HandlerRemoved",
            "Handler Removed",
            "Event handler removed.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PropertyReferenceDescriptor = new DiagnosticDescriptor(
            "PropertyReference",
            "Property Reference",
            "Property reference found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FieldReferenceDescriptor = new DiagnosticDescriptor(
            "FieldReference",
            "Field Reference",
            "Field reference found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodBindingDescriptor = new DiagnosticDescriptor(
            "MethodBinding",
            "Method Binding",
            "Method binding found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EventReferenceDescriptor, HandlerAddedDescriptor, HandlerRemovedDescriptor, PropertyReferenceDescriptor, FieldReferenceDescriptor, MethodBindingDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(EventReferenceDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.EventReferenceExpression);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IEventAssignmentExpression eventAssignment = (IEventAssignmentExpression)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(eventAssignment.Adds? HandlerAddedDescriptor : HandlerRemovedDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.EventAssignmentExpression);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(PropertyReferenceDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.PropertyReferenceExpression);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(FieldReferenceDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.FieldReferenceExpression);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(MethodBindingDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.MethodBindingExpression);
        }
    }
    
    /// <summary>Analyzer used to test IOperation treatment of params array arguments.</summary>
    public class ParamsArrayTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor LongParamsDescriptor = new DiagnosticDescriptor(
            "LongParams",
            "Long Params",
            "Params array argument has more than 3 elements.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(LongParamsDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IInvocationExpression invocation = (IInvocationExpression)operationContext.Operation;

                     foreach (IArgument argument in invocation.ArgumentsInSourceOrder)
                     {
                         if (argument.Parameter.IsParams)
                         {
                             IArrayCreationExpression arrayValue = argument.Value as IArrayCreationExpression;
                             if (arrayValue != null)
                             {
                                 Optional<object> dimensionSize = arrayValue.DimensionSizes[0].ConstantValue;
                                 if (dimensionSize.HasValue && IntegralValue(dimensionSize.Value) > 3)
                                 {
                                     operationContext.ReportDiagnostic(Diagnostic.Create(LongParamsDescriptor, argument.Value.Syntax.GetLocation()));
                                 }
                             }
                         }
                     }

                     foreach (IArgument argument in invocation.ArgumentsInParameterOrder)
                     {
                         if (argument.Parameter.IsParams)
                         {
                             IArrayCreationExpression arrayValue = argument.Value as IArrayCreationExpression;
                             if (arrayValue != null)
                             {
                                 Optional<object> dimensionSize = arrayValue.DimensionSizes[0].ConstantValue;
                                 if (dimensionSize.HasValue && IntegralValue(dimensionSize.Value) > 3)
                                 {
                                     operationContext.ReportDiagnostic(Diagnostic.Create(LongParamsDescriptor, argument.Value.Syntax.GetLocation()));
                                 }
                             }
                         }
                     }
                 },
                 OperationKind.InvocationExpression);
        }

        private static long IntegralValue(object value)
        {
            if (value is long)
            {
                return (long)value;
            }

            if (value is int)
            {
                return (int)value;
            }

            return 0;
        }
    }

    /// <summary>Analyzer used to test for initializer constructs for members and parameters.</summary>
    public class EqualsValueTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor EqualsValueDescriptor = new DiagnosticDescriptor(
            "EqualsValue",
            "Equals Value",
            "Equals value found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EqualsValueDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IFieldInitializer equalsValue = (IFieldInitializer)operationContext.Operation;
                     if (equalsValue.InitializedFields[0].Name.StartsWith("F"))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(EqualsValueDescriptor, equalsValue.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.FieldInitializerAtDeclaration);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IParameterInitializer equalsValue = (IParameterInitializer)operationContext.Operation;
                     if (equalsValue.Parameter.Name.StartsWith("F"))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(EqualsValueDescriptor, equalsValue.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.ParameterInitializerAtDeclaration);
        }
    }

    /// <summary>Analyzer used to test None IOperations.</summary>
    public class NoneOperationTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        // We should not see this warning triggered by any code
        public static readonly DiagnosticDescriptor NoneOperationDescriptor = new DiagnosticDescriptor(
            "NoneOperation",
            "None operation found",
            "An IOperation of None kind is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
        
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(NoneOperationDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(NoneOperationDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 // None kind is only supposed to be used internally and will not actually register actions.
                 OperationKind.None);
        }
    }

    /// <summary>Analyzer used to test LambdaExpression IOperations.</summary>
    public class LambdaTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";
        
        public static readonly DiagnosticDescriptor LambdaExpressionDescriptor = new DiagnosticDescriptor(
            "LambdaExpression",
            "Lambda expressionn found",
            "An Lambda expression is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TooManyStatementsInLambdaExpressionDescriptor = new DiagnosticDescriptor(
            "TooManyStatementsInLambdaExpression",
            "Too many statements in a Lambda expression",
            "More than 3 statements in a Lambda expression",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // This warning should never be triggered.
        public static readonly DiagnosticDescriptor NoneOperationInLambdaExpressionDescriptor = new DiagnosticDescriptor(
            "NoneOperationInLambdaExpression",
            "None Operation found in Lambda expression",
            "None Operation is found Lambda expression",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(LambdaExpressionDescriptor, 
                                  TooManyStatementsInLambdaExpressionDescriptor,
                                  NoneOperationInLambdaExpressionDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var lambdaExpression = (ILambdaExpression)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(LambdaExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     var block = lambdaExpression.Body;
                     // TODO: Can this possibly be null? Remove check if not.
                     if (block == null)
                     {
                         return;
                     }
                     if (block.Statements.Length > 3)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(TooManyStatementsInLambdaExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     }
                     bool flag = false;
                     foreach (var statement in block.Statements)
                     {
                         if (statement.Kind == OperationKind.None)
                         {
                             flag = true;
                             break;
                         }
                     }
                     if (flag)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(NoneOperationInLambdaExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.LambdaExpression);
        }
    }
    
    public class StaticMemberTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";
        
        public static readonly DiagnosticDescriptor StaticMemberDescriptor = new DiagnosticDescriptor(
            "StaticMember",
            "Static member found",
            "A static member reference expression is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // We should not see this warning triggered by any code
        public static readonly DiagnosticDescriptor StaticMemberWithInstanceDescriptor = new DiagnosticDescriptor(
            "StaticMemberWithInstance",
            "Static member with non null Instance found",
            "A static member reference with non null Instance is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(StaticMemberDescriptor,
                                               StaticMemberWithInstanceDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var operation = operationContext.Operation;
                     ISymbol memberSymbol;
                     IExpression receiver;
                     switch (operation.Kind)
                     {
                         case OperationKind.FieldReferenceExpression:
                             memberSymbol = ((IFieldReferenceExpression)operation).Field;
                             receiver = ((IFieldReferenceExpression)operation).Instance;
                             break;
                         case OperationKind.PropertyReferenceExpression:
                             memberSymbol = ((IPropertyReferenceExpression)operation).Property;
                             receiver = ((IPropertyReferenceExpression)operation).Instance;
                             break;
                         case OperationKind.EventReferenceExpression:
                             memberSymbol = ((IEventReferenceExpression)operation).Event;
                             receiver = ((IEventReferenceExpression)operation).Instance;
                             break;
                         case OperationKind.MethodBindingExpression:
                             memberSymbol = ((IMethodBindingExpression)operation).Method;
                             receiver = ((IMethodBindingExpression)operation).Instance;
                             break;
                         case OperationKind.InvocationExpression:
                             memberSymbol = ((IInvocationExpression)operation).TargetMethod;
                             receiver = ((IInvocationExpression)operation).Instance;
                             break;
                         case OperationKind.EventAssignmentExpression:
                             memberSymbol = ((IEventAssignmentExpression)operation).Event;
                             receiver = ((IEventAssignmentExpression)operation).EventInstance;
                             break;
                         default:
                             throw new ArgumentException();
                     }
                     if (memberSymbol.IsStatic)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(StaticMemberDescriptor, operation.Syntax.GetLocation()));

                         if (receiver != null)
                         {
                             operationContext.ReportDiagnostic(Diagnostic.Create(StaticMemberWithInstanceDescriptor, operation.Syntax.GetLocation()));
                         }
                     }
                 },
                 OperationKind.FieldReferenceExpression,
                 OperationKind.PropertyReferenceExpression,
                 OperationKind.EventReferenceExpression,
                 OperationKind.MethodBindingExpression,
                 OperationKind.InvocationExpression,
                 OperationKind.EventAssignmentExpression);
        }
    }
}