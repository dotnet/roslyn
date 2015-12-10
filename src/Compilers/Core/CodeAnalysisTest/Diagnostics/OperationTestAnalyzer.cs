// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                             if (conditionRight.ConstantValue != null &&
                                 conditionRight.ResultType.SpecialType == SpecialType.System_Int32 &&
                                 conditionLeft.Kind == OperationKind.LocalReferenceExpression)
                             {
                                 // Test is known to be a comparison of a local against a constant.

                                 int testValue = (int)conditionRight.ConstantValue;
                                 ILocalSymbol testVariable = ((ILocalReferenceExpression)conditionLeft).Local;

                                 if (forLoop.Before.Length == 1)
                                 {
                                     IStatement setup = forLoop.Before[0];
                                     if (setup.Kind == OperationKind.ExpressionStatement && ((IExpressionStatement)setup).Expression.Kind == OperationKind.AssignmentExpression)
                                     {
                                         IAssignmentExpression setupAssignment = (IAssignmentExpression)((IExpressionStatement)setup).Expression;
                                         if (setupAssignment.Target.Kind == OperationKind.LocalReferenceExpression &&
                                             ((ILocalReferenceExpression)setupAssignment.Target).Local == testVariable &&
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
                                                                 advanceOperation.Right.ConstantValue != null &&
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
                                                             advanceAssignment.Value.ConstantValue != null &&
                                                             advanceAssignment.Value.ResultType.SpecialType == SpecialType.System_Int32)
                                                         {
                                                             // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                                                             advanceIncrement = advanceAssignment.Value;
                                                             advanceOperationCode = advanceAssignment.BinaryKind;
                                                         }
                                                     }

                                                     if (advanceIncrement != null)
                                                     {
                                                         int incrementValue = (int)advanceIncrement.ConstantValue;
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
            get { return ImmutableArray.Create(SparseSwitchDescriptor, 
                                                NoDefaultSwitchDescriptor,
                                                OnlyDefaultSwitchDescriptor); }
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
                                         if (singleValueExpression?.ConstantValue != null &&
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
                                         hasNonDefault = true;
                                         IRangeCaseClause rangeClause = (IRangeCaseClause)clause;
                                         IExpression rangeMinExpression = rangeClause.MinimumValue;
                                         IExpression rangeMaxExpression = rangeClause.MaximumValue;
                                         if (rangeMinExpression?.ConstantValue != null &&
                                             rangeMinExpression.ResultType.SpecialType == SpecialType.System_Int32 &&
                                             rangeMaxExpression?.ConstantValue != null &&
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
                                         hasNonDefault = true;
                                         IRelationalCaseClause relationalClause = (IRelationalCaseClause)clause;
                                         IExpression relationalValueExpression = relationalClause.Value;
                                         if (relationalValueExpression?.ConstantValue != null &&
                                             relationalValueExpression.ResultType.SpecialType == SpecialType.System_Int32)
                                         {
                                             int rangeMinValue = int.MaxValue;
                                             int rangeMaxValue = int.MinValue;
                                             int relationalValue = (int)relationalValueExpression.ConstantValue;
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

        public static readonly DiagnosticDescriptor BigParamarrayArgumentsDescriptor = new DiagnosticDescriptor(
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
            get { return ImmutableArray.Create(BigParamarrayArgumentsDescriptor, 
                OutOfNumericalOrderArgumentsDescriptor, 
                UseDefaultArgumentDescriptor); }
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
                             if (arrayArgument != null && arrayArgument.ElementValues.ArrayInitializerKind == ArrayInitializerKind.Dimension)
                             {
                                 IDimensionArrayInitializer dimension = arrayArgument.ElementValues as IDimensionArrayInitializer;
                                 if (dimension != null)
                                 {
                                     if (dimension.ElementValues.Length > 10)
                                     {
                                         Report(operationContext, invocation.Syntax, BigParamarrayArgumentsDescriptor);
                                     }

                                     foreach (IArrayInitializer dimensionValues in dimension.ElementValues)
                                     {
                                         if (dimensionValues.ArrayInitializerKind == ArrayInitializerKind.Expression)
                                         {
                                             IExpressionArrayInitializer expressionInitializer = dimensionValues as IExpressionArrayInitializer;
                                             if (expressionInitializer != null)
                                             {
                                                 TestAscendingArgument(operationContext, expressionInitializer.ElementValue, ref priorArgumentValue);
                                             }
                                         }
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
            object argumentValue = argument.ConstantValue;
            if (argumentValue != null && argument.ResultType.SpecialType == SpecialType.System_Int32)
            {
                int integerArgument = (int)argumentValue;
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
                         literal.ConstantValue != null &&
                         (int)literal.ConstantValue == 17)
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
                     if (argument.Value.ConstantValue == null)
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

        public static readonly DiagnosticDescriptor DoNotUseFieldInitiliazerDescriptor = new DiagnosticDescriptor(
            "DoNotUseFieldInitializer",
            "Do Not Use Field Initializer",
            "a filed initializer is used for object creation",
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
            get { return ImmutableArray.Create(DoNotUseFieldInitiliazerDescriptor, DoNotUsePropertyInitializerDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var initializer = (IMemberInitializer)operationContext.Operation;
                     switch (initializer.MemberInitializerKind)
                     {
                         case MemberInitializerKind.Field:
                             Report(operationContext, initializer.Syntax, DoNotUseFieldInitiliazerDescriptor);
                             break;
                         case MemberInitializerKind.Property:
                             Report(operationContext, initializer.Syntax, DoNotUsePropertyInitializerDescriptor);
                             break;
                     }
                 },
                 OperationKind.FieldInitializer,
                 OperationKind.PropertyInitializer);
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
        }
    }
}