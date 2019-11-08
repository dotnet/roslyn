// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
                     var invalidOperation = (IInvalidOperation)operationContext.Operation;
                     if (invalidOperation.Type == null)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(InvalidStatementDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     }
                     else
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(InvalidExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.Invalid);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     if (operationContext.Operation.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(IsInvalidDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.Invocation,
                 OperationKind.Invalid);
        }
    }

    /// <summary>Analyzer used to test for operations within symbols of certain names.</summary>
    public class OwningSymbolTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor ExpressionDescriptor = new DiagnosticDescriptor(
            "Expression",
            "Expression",
            "Expression found.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(ExpressionDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationBlockStartAction(
                (operationBlockContext) =>
                {
                    if (operationBlockContext.Compilation.Language != "Stumble")
                    {
                        operationBlockContext.RegisterOperationAction(
                             (operationContext) =>
                             {
                                 if (operationContext.ContainingSymbol.Name.StartsWith("Funky") && operationContext.Compilation.Language != "Mumble")
                                 {
                                     operationContext.ReportDiagnostic(Diagnostic.Create(ExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                                 }
                             },
                             OperationKind.LocalReference,
                             OperationKind.Literal);
                    }
                });
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
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Loop);
        }

        private void AnalyzeOperation(OperationAnalysisContext operationContext)
        {
            ILoopOperation loop = (ILoopOperation)operationContext.Operation;
            if (loop.LoopKind == LoopKind.For)
            {
                IForLoopOperation forLoop = (IForLoopOperation)loop;
                IOperation forCondition = forLoop.Condition;

                if (forCondition.Kind == OperationKind.Binary)
                {
                    IBinaryOperation condition = (IBinaryOperation)forCondition;
                    IOperation conditionLeft = condition.LeftOperand;
                    IOperation conditionRight = condition.RightOperand;

                    if (conditionRight is { ConstantValue: { HasValue: true }, Type: { SpecialType: SpecialType.System_Int32 } } && conditionLeft is { Kind: OperationKind.LocalReference })
                    {
                        // Test is known to be a comparison of a local against a constant.

                        int testValue = (int)conditionRight.ConstantValue.Value;
                        ILocalSymbol testVariable = ((ILocalReferenceOperation)conditionLeft).Local;

                        if (forLoop.Before.Length == 1)
                        {
                            IOperation setup = forLoop.Before[0];
                            if (setup.Kind == OperationKind.ExpressionStatement && ((IExpressionStatementOperation)setup).Operation.Kind == OperationKind.SimpleAssignment)
                            {
                                ISimpleAssignmentOperation setupAssignment = (ISimpleAssignmentOperation)((IExpressionStatementOperation)setup).Operation;
                                if (setupAssignment.Target.Kind == OperationKind.LocalReference &&
                                    ((ILocalReferenceOperation)setupAssignment.Target).Local == testVariable &&
                                    setupAssignment.Value.ConstantValue.HasValue &&
                                    setupAssignment.Value.Type.SpecialType == SpecialType.System_Int32)
                                {
                                    // Setup is known to be an assignment of a constant to the local used in the test.

                                    int initialValue = (int)setupAssignment.Value.ConstantValue.Value;

                                    if (forLoop.AtLoopBottom.Length == 1)
                                    {
                                        IOperation advance = forLoop.AtLoopBottom[0];
                                        if (advance.Kind == OperationKind.ExpressionStatement)
                                        {
                                            IOperation advanceExpression = ((IExpressionStatementOperation)advance).Operation;

                                            Optional<object> advanceIncrementOpt;
                                            BinaryOperatorKind? advanceOperationCode;
                                            GetOperationKindAndValue(testVariable, advanceExpression, out advanceOperationCode, out advanceIncrementOpt);

                                            if (advanceIncrementOpt.HasValue && advanceOperationCode.HasValue)
                                            {
                                                var incrementValue = (int)advanceIncrementOpt.Value;
                                                if (advanceOperationCode.Value == BinaryOperatorKind.Subtract)
                                                {
                                                    advanceOperationCode = BinaryOperatorKind.Add;
                                                    incrementValue = -incrementValue;
                                                }

                                                if (advanceOperationCode.Value == BinaryOperatorKind.Add &&
                                                    incrementValue != 0 &&
                                                    (condition.OperatorKind == BinaryOperatorKind.LessThan ||
                                                     condition.OperatorKind == BinaryOperatorKind.LessThanOrEqual ||
                                                     condition.OperatorKind == BinaryOperatorKind.NotEquals ||
                                                     condition.OperatorKind == BinaryOperatorKind.GreaterThan ||
                                                     condition.OperatorKind == BinaryOperatorKind.GreaterThanOrEqual))
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
        }

        private void GetOperationKindAndValue(
            ILocalSymbol testVariable, IOperation advanceExpression,
            out BinaryOperatorKind? advanceOperationCode, out Optional<object> advanceIncrementOpt)
        {
            advanceIncrementOpt = null;
            advanceOperationCode = null;

            if (advanceExpression.Kind == OperationKind.SimpleAssignment)
            {
                ISimpleAssignmentOperation advanceAssignment = (ISimpleAssignmentOperation)advanceExpression;

                if (advanceAssignment.Target.Kind == OperationKind.LocalReference &&
                    ((ILocalReferenceOperation)advanceAssignment.Target).Local == testVariable &&
                    advanceAssignment.Value.Kind == OperationKind.Binary &&
                    advanceAssignment.Value.Type.SpecialType == SpecialType.System_Int32)
                {
                    // Advance is known to be an assignment of a binary operation to the local used in the test.

                    IBinaryOperation advanceOperation = (IBinaryOperation)advanceAssignment.Value;
                    if (advanceOperation.OperatorMethod == null &&
                        advanceOperation.LeftOperand.Kind == OperationKind.LocalReference &&
                        ((ILocalReferenceOperation)advanceOperation.LeftOperand).Local == testVariable &&
                        advanceOperation.RightOperand.ConstantValue.HasValue &&
                        advanceOperation.RightOperand.Type.SpecialType == SpecialType.System_Int32)
                    {
                        // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                        advanceIncrementOpt = advanceOperation.RightOperand.ConstantValue;
                        advanceOperationCode = advanceOperation.OperatorKind;
                    }
                }
            }
            else if (advanceExpression.Kind == OperationKind.CompoundAssignment)
            {
                ICompoundAssignmentOperation advanceAssignment = (ICompoundAssignmentOperation)advanceExpression;

                if (advanceAssignment.Target.Kind == OperationKind.LocalReference &&
                    ((ILocalReferenceOperation)advanceAssignment.Target).Local == testVariable &&
                    advanceAssignment.Value.ConstantValue.HasValue &&
                    advanceAssignment.Value.Type.SpecialType == SpecialType.System_Int32)
                {
                    // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                    advanceIncrementOpt = advanceAssignment.Value.ConstantValue;
                    advanceOperationCode = advanceAssignment.OperatorKind;
                }
            }
            else if (advanceExpression.Kind == OperationKind.Increment)
            {
                IIncrementOrDecrementOperation advanceAssignment = (IIncrementOrDecrementOperation)advanceExpression;

                if (advanceAssignment.Target.Kind == OperationKind.LocalReference &&
                    ((ILocalReferenceOperation)advanceAssignment.Target).Local == testVariable)
                {
                    // Advance binary operation is known to involve a reference to the local used in the test and a constant.
                    advanceIncrementOpt = new Optional<object>(1);
                    advanceOperationCode = BinaryOperatorKind.Add;
                }
            }
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
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
                     ISwitchOperation switchOperation = (ISwitchOperation)operationContext.Operation;
                     long minCaseValue = long.MaxValue;
                     long maxCaseValue = long.MinValue;
                     long caseValueCount = 0;
                     bool hasDefault = false;
                     bool hasNonDefault = false;
                     foreach (ISwitchCaseOperation switchCase in switchOperation.Cases)
                     {
                         foreach (ICaseClauseOperation clause in switchCase.Clauses)
                         {
                             switch (clause.CaseKind)
                             {
                                 case CaseKind.SingleValue:
                                     {
                                         hasNonDefault = true;
                                         ISingleValueCaseClauseOperation singleValueClause = (ISingleValueCaseClauseOperation)clause;
                                         IOperation singleValueExpression = singleValueClause.Value;
                                         if (singleValueExpression != null &&
                                             singleValueExpression.ConstantValue.HasValue &&
                                             singleValueExpression.Type.SpecialType == SpecialType.System_Int32)
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
                                         IRangeCaseClauseOperation rangeClause = (IRangeCaseClauseOperation)clause;
                                         IOperation rangeMinExpression = rangeClause.MinimumValue;
                                         IOperation rangeMaxExpression = rangeClause.MaximumValue;
                                         if (rangeMinExpression != null &&
                                             rangeMinExpression.ConstantValue.HasValue &&
                                             rangeMinExpression.Type.SpecialType == SpecialType.System_Int32 &&
                                             rangeMaxExpression != null &&
                                             rangeMaxExpression.ConstantValue.HasValue &&
                                             rangeMaxExpression.Type.SpecialType == SpecialType.System_Int32)
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
                                         IRelationalCaseClauseOperation relationalClause = (IRelationalCaseClauseOperation)clause;
                                         IOperation relationalValueExpression = relationalClause.Value;
                                         if (relationalValueExpression != null &&
                                             relationalValueExpression.ConstantValue.HasValue &&
                                             relationalValueExpression.Type.SpecialType == SpecialType.System_Int32)
                                         {
                                             int rangeMinValue = int.MaxValue;
                                             int rangeMaxValue = int.MinValue;
                                             int relationalValue = (int)relationalValueExpression.ConstantValue.Value;
                                             switch (relationalClause.Relation)
                                             {
                                                 case BinaryOperatorKind.Equals:
                                                     rangeMinValue = relationalValue;
                                                     rangeMaxValue = relationalValue;
                                                     break;
                                                 case BinaryOperatorKind.NotEquals:
                                                     return;
                                                 case BinaryOperatorKind.LessThan:
                                                     rangeMinValue = int.MinValue;
                                                     rangeMaxValue = relationalValue - 1;
                                                     break;
                                                 case BinaryOperatorKind.LessThanOrEqual:
                                                     rangeMinValue = int.MinValue;
                                                     rangeMaxValue = relationalValue;
                                                     break;
                                                 case BinaryOperatorKind.GreaterThanOrEqual:
                                                     rangeMinValue = relationalValue;
                                                     rangeMaxValue = int.MaxValue;
                                                     break;
                                                 case BinaryOperatorKind.GreaterThan:
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
                 OperationKind.Switch);
        }

        private static int IncludeClause(int clauseMinValue, int clauseMaxValue, ref long minCaseValue, ref long maxCaseValue)
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

        private void Report(OperationAnalysisContext context, SyntaxNode syntax, DiagnosticDescriptor descriptor)
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

        public static readonly DiagnosticDescriptor InvalidArgumentDescriptor = new DiagnosticDescriptor(
            "InvalidArgument",
            "Invalid argument",
            "Invocation has invalid argument",
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
                                             UseDefaultArgumentDescriptor,
                                             InvalidArgumentDescriptor);
            }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IInvocationOperation invocation = (IInvocationOperation)operationContext.Operation;
                     long priorArgumentValue = long.MinValue;
                     foreach (IArgumentOperation argument in invocation.Arguments)
                     {
                         if (argument.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                         {
                             operationContext.ReportDiagnostic(Diagnostic.Create(InvalidArgumentDescriptor, argument.Syntax.GetLocation()));
                             return;
                         }

                         if (argument.ArgumentKind == ArgumentKind.DefaultValue)
                         {
                             operationContext.ReportDiagnostic(Diagnostic.Create(UseDefaultArgumentDescriptor, invocation.Syntax.GetLocation(), argument.Parameter.Name));
                         }

                         TestAscendingArgument(operationContext, argument.Value, ref priorArgumentValue);

                         if (argument.ArgumentKind == ArgumentKind.ParamArray)
                         {
                             if (argument.Value is IArrayCreationOperation arrayArgument)
                             {
                                 var initializer = arrayArgument.Initializer;
                                 if (initializer != null)
                                 {
                                     if (initializer.ElementValues.Length > 10)
                                     {
                                         Report(operationContext, invocation.Syntax, BigParamArrayArgumentsDescriptor);
                                     }

                                     foreach (IOperation element in initializer.ElementValues)
                                     {
                                         TestAscendingArgument(operationContext, element, ref priorArgumentValue);
                                     }
                                 }
                             }
                         }
                     }
                 },
                 OperationKind.Invocation);
        }

        private static void TestAscendingArgument(OperationAnalysisContext operationContext, IOperation argument, ref long priorArgumentValue)
        {
            Optional<object> argumentValue = argument.ConstantValue;
            if (argumentValue.HasValue && argument.Type.SpecialType == SpecialType.System_Int32)
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
                     ILiteralOperation literal = (ILiteralOperation)operationContext.Operation;
                     if (literal.Type.SpecialType == SpecialType.System_Int32 &&
                         literal.ConstantValue.HasValue &&
                         (int)literal.ConstantValue.Value == 17)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(SeventeenDescriptor, literal.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.Literal);
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
                     var argument = (IArgumentOperation)operationContext.Operation;
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
                     var initializer = operationContext.Operation;
                     Report(operationContext, initializer.Syntax, initializer.Kind == OperationKind.FieldReference ? DoNotUseFieldInitializerDescriptor : DoNotUsePropertyInitializerDescriptor);
                 },
                 OperationKind.FieldReference,
                 OperationKind.PropertyReference);
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
                     var assignment = (ISimpleAssignmentOperation)operationContext.Operation;
                     var kind = assignment.Target.Kind;
                     if (kind == OperationKind.FieldReference ||
                         kind == OperationKind.PropertyReference)
                     {
                         Report(operationContext, assignment.Syntax, DoNotUseMemberAssignmentDescriptor);
                     }
                 },
                 OperationKind.SimpleAssignment);
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
        private const string Maintainability = nameof(Maintainability);

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
                     var initializer = (IArrayInitializerOperation)operationContext.Operation;
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
        private const string Maintainability = nameof(Maintainability);

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
                     var declarationStatement = (IVariableDeclarationGroupOperation)operationContext.Operation;
                     if (declarationStatement.GetDeclaredVariables().Count() > 3)
                     {
                         Report(operationContext, declarationStatement.Syntax, TooManyLocalVarDeclarationsDescriptor);
                     }

                     foreach (var decl in declarationStatement.Declarations.SelectMany(multiDecl => multiDecl.Declarators))
                     {
                         var initializer = decl.GetVariableInitializer();
                         if (initializer != null && !initializer.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                         {
                             Report(operationContext, decl.Symbol.DeclaringSyntaxReferences.Single().GetSyntax(), LocalVarInitializedDeclarationDescriptor);
                         }
                     }
                 },
                 OperationKind.VariableDeclarationGroup);
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
        private const string Maintainability = nameof(Maintainability);

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
                         case OperationKind.CaseClause:
                             var caseClause = (ICaseClauseOperation)operationContext.Operation;
                             if (caseClause.CaseKind == CaseKind.Default)
                             {
                                 Report(operationContext, caseClause.Syntax, HasDefaultCaseDescriptor);
                             }
                             break;
                         case OperationKind.SwitchCase:
                             var switchSection = (ISwitchCaseOperation)operationContext.Operation;
                             if (!switchSection.HasErrors(operationContext.Compilation, operationContext.CancellationToken) && switchSection.Clauses.Length > 1)
                             {
                                 Report(operationContext, switchSection.Syntax, MultipleCaseClausesDescriptor);
                             }
                             break;
                     }
                 },
                 OperationKind.SwitchCase,
                 OperationKind.CaseClause);
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
                     IInstanceReferenceOperation instanceReference = (IInstanceReferenceOperation)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(instanceReference.IsImplicit ? ImplicitInstanceDescriptor : ExplicitInstanceDescriptor,
                                                                         instanceReference.Syntax.GetLocation()));
                 },
                 OperationKind.InstanceReference);
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

        public static readonly DiagnosticDescriptor InvalidEventDescriptor = new DiagnosticDescriptor(
            "InvalidEvent",
            "Invalid Event",
            "A EventAssignmentExpression with invalid event found.",
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

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(EventReferenceDescriptor,
                HandlerAddedDescriptor,
                HandlerRemovedDescriptor,
                PropertyReferenceDescriptor,
                FieldReferenceDescriptor,
                MethodBindingDescriptor,
                InvalidEventDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(EventReferenceDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.EventReference);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IEventAssignmentOperation eventAssignment = (IEventAssignmentOperation)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(eventAssignment.Adds ? HandlerAddedDescriptor : HandlerRemovedDescriptor, operationContext.Operation.Syntax.GetLocation()));

                     if (eventAssignment.EventReference.Kind == OperationKind.Invalid || eventAssignment.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(InvalidEventDescriptor, eventAssignment.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.EventAssignment);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(PropertyReferenceDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.PropertyReference);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(FieldReferenceDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.FieldReference);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(MethodBindingDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.MethodReference);
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

        public static readonly DiagnosticDescriptor InvalidConstructorDescriptor = new DiagnosticDescriptor(
            "InvalidConstructor",
            "Invalid Constructor",
            "Invalid Constructor.",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(LongParamsDescriptor, InvalidConstructorDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IInvocationOperation invocation = (IInvocationOperation)operationContext.Operation;

                     foreach (IArgumentOperation argument in invocation.Arguments)
                     {
                         if (argument.Parameter.IsParams)
                         {
                             if (argument.Value is IArrayCreationOperation arrayValue)
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
                 OperationKind.Invocation);

            context.RegisterOperationAction(
                (operationContext) =>
                {
                    IObjectCreationOperation creation = (IObjectCreationOperation)operationContext.Operation;

                    if (creation.Constructor == null)
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(InvalidConstructorDescriptor, creation.Syntax.GetLocation()));
                    }

                    foreach (IArgumentOperation argument in creation.Arguments)
                    {
                        if (argument.Parameter.IsParams)
                        {
                            if (argument.Value is IArrayCreationOperation arrayValue)
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
                OperationKind.ObjectCreation);
        }

        private static long IntegralValue(object value)
        {
            if (value is long v)
            {
                return v;
            }

            if (value is int i)
            {
                return i;
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
                     IFieldInitializerOperation equalsValue = (IFieldInitializerOperation)operationContext.Operation;
                     if (equalsValue.InitializedFields[0].Name.StartsWith("F"))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(EqualsValueDescriptor, equalsValue.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.FieldInitializer);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IParameterInitializerOperation equalsValue = (IParameterInitializerOperation)operationContext.Operation;
                     if (equalsValue.Parameter.Name.StartsWith("F"))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(EqualsValueDescriptor, equalsValue.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.ParameterInitializer);
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

    public class AddressOfTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor AddressOfDescriptor = new DiagnosticDescriptor(
            "AddressOfOperation",
            "AddressOf operation found",
            "An AddressOf operation found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidAddressOfReferenceDescriptor = new DiagnosticDescriptor(
            "InvalidAddressOfReference",
            "Invalid AddressOf reference found",
            "An invalid AddressOf reference found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(AddressOfDescriptor, InvalidAddressOfReferenceDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var addressOfOperation = (IAddressOfOperation)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(AddressOfDescriptor, addressOfOperation.Syntax.GetLocation()));

                     if (addressOfOperation.Reference.Kind == OperationKind.Invalid && addressOfOperation.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(InvalidAddressOfReferenceDescriptor, addressOfOperation.Reference.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.AddressOf);
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
                     var lambdaExpression = (IAnonymousFunctionOperation)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(LambdaExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     var block = lambdaExpression.Body;
                     // TODO: Can this possibly be null? Remove check if not.
                     if (block == null)
                     {
                         return;
                     }
                     if (block.Operations.Length > 3)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(TooManyStatementsInLambdaExpressionDescriptor, operationContext.Operation.Syntax.GetLocation()));
                     }
                     bool flag = false;
                     foreach (var statement in block.Operations)
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
                 OperationKind.AnonymousFunction);
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
            get
            {
                return ImmutableArray.Create(StaticMemberDescriptor,
                                             StaticMemberWithInstanceDescriptor);
            }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var operation = operationContext.Operation;
                     ISymbol memberSymbol;
                     IOperation receiver;
                     switch (operation.Kind)
                     {
                         case OperationKind.FieldReference:
                             memberSymbol = ((IFieldReferenceOperation)operation).Field;
                             receiver = ((IFieldReferenceOperation)operation).Instance;
                             break;
                         case OperationKind.PropertyReference:
                             memberSymbol = ((IPropertyReferenceOperation)operation).Property;
                             receiver = ((IPropertyReferenceOperation)operation).Instance;
                             break;
                         case OperationKind.EventReference:
                             memberSymbol = ((IEventReferenceOperation)operation).Event;
                             receiver = ((IEventReferenceOperation)operation).Instance;
                             break;
                         case OperationKind.MethodReference:
                             memberSymbol = ((IMethodReferenceOperation)operation).Method;
                             receiver = ((IMethodReferenceOperation)operation).Instance;
                             break;
                         case OperationKind.Invocation:
                             memberSymbol = ((IInvocationOperation)operation).TargetMethod;
                             receiver = ((IInvocationOperation)operation).Instance;
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
                 OperationKind.FieldReference,
                 OperationKind.PropertyReference,
                 OperationKind.EventReference,
                 OperationKind.MethodReference,
                 OperationKind.Invocation);
        }
    }

    public class LabelOperationsTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor LabelDescriptor = new DiagnosticDescriptor(
           "Label",
           "Label found",
           "A label was was found",
           "Testing",
           DiagnosticSeverity.Warning,
           isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GotoDescriptor = new DiagnosticDescriptor(
          "Goto",
          "Goto found",
          "A goto was was found",
          "Testing",
          DiagnosticSeverity.Warning,
          isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(LabelDescriptor, GotoDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                (operationContext) =>
                {
                    ILabelSymbol label = ((ILabeledOperation)operationContext.Operation).Label;
                    if (label.Name == "Wilma" || label.Name == "Betty")
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(LabelDescriptor, operationContext.Operation.Syntax.GetLocation()));
                    }
                },
                OperationKind.Labeled);

            context.RegisterOperationAction(
                (operationContext) =>
                {
                    IBranchOperation branch = (IBranchOperation)operationContext.Operation;
                    if (branch.BranchKind == BranchKind.GoTo)
                    {
                        ILabelSymbol label = branch.Target;
                        if (label.Name == "Wilma" || label.Name == "Betty")
                        {
                            operationContext.ReportDiagnostic(Diagnostic.Create(GotoDescriptor, branch.Syntax.GetLocation()));
                        }
                    }
                },
                OperationKind.Branch);
        }
    }

    public class UnaryAndBinaryOperationsTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor OperatorAddMethodDescriptor = new DiagnosticDescriptor(
            "OperatorAddMethod",
            "Operator Add method found",
            "An operator Add method was found",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor OperatorMinusMethodDescriptor = new DiagnosticDescriptor(
            "OperatorMinusMethod",
            "Operator Minus method found",
            "An operator Minus method was found",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DoubleMultiplyDescriptor = new DiagnosticDescriptor(
            "DoubleMultiply",
            "Double multiply found",
            "A double multiply was found",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor BooleanNotDescriptor = new DiagnosticDescriptor(
            "BooleanNot",
            "Boolean not found",
            "A boolean not was found",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(OperatorAddMethodDescriptor, OperatorMinusMethodDescriptor, DoubleMultiplyDescriptor, BooleanNotDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                (operationContext) =>
                {
                    IBinaryOperation binary = (IBinaryOperation)operationContext.Operation;
                    if (binary.OperatorKind == BinaryOperatorKind.Add && binary.OperatorMethod != null && binary.OperatorMethod.Name.Contains("Addition"))
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(OperatorAddMethodDescriptor, binary.Syntax.GetLocation()));
                    }

                    if (binary.OperatorKind == BinaryOperatorKind.Multiply && binary.Type.SpecialType == SpecialType.System_Double)
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(DoubleMultiplyDescriptor, binary.Syntax.GetLocation()));
                    }
                },
                OperationKind.Binary);

            context.RegisterOperationAction(
                (operationContext) =>
                {
                    IUnaryOperation unary = (IUnaryOperation)operationContext.Operation;
                    if (unary.OperatorKind == UnaryOperatorKind.Minus && unary.OperatorMethod != null && unary.OperatorMethod.Name.Contains("UnaryNegation"))
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(OperatorMinusMethodDescriptor, unary.Syntax.GetLocation()));
                    }

                    if (unary.OperatorKind == UnaryOperatorKind.Not)
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(BooleanNotDescriptor, unary.Syntax.GetLocation()));
                    }

                    if (unary.OperatorKind == UnaryOperatorKind.BitwiseNegation)
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(BooleanNotDescriptor, unary.Syntax.GetLocation()));
                    }
                },
                OperationKind.Unary);
        }
    }

    public class BinaryOperatorVBTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor BinaryUserDefinedOperatorDescriptor = new DiagnosticDescriptor(
            "BinaryUserDefinedOperator",
            "Binary user defined operator found",
            "A Binary user defined operator {0} is found",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(BinaryUserDefinedOperatorDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                (operationContext) =>
                {
                    var binary = (IBinaryOperation)operationContext.Operation;
                    if (binary.OperatorMethod != null)
                    {
                        operationContext.ReportDiagnostic(
                            Diagnostic.Create(BinaryUserDefinedOperatorDescriptor,
                                binary.Syntax.GetLocation(),
                                binary.OperatorKind.ToString()));
                    }
                },
                OperationKind.Binary);
        }
    }

    public class OperatorPropertyPullerTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor BinaryOperatorDescriptor = new DiagnosticDescriptor(
            "BinaryOperator",
            "Binary operator found",
            "A Binary operator {0} was found",
            "Testing",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnaryOperatorDescriptor = new DiagnosticDescriptor(
           "UnaryOperator",
           "Unary operator found",
           "A Unary operator {0} was found",
           "Testing",
           DiagnosticSeverity.Warning,
           isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(BinaryOperatorDescriptor, UnaryOperatorDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                (operationContext) =>
                {
                    var binary = (IBinaryOperation)operationContext.Operation;
                    var left = binary.LeftOperand;
                    var right = binary.RightOperand;
                    if (!left.HasErrors(operationContext.Compilation, operationContext.CancellationToken) &&
                        !right.HasErrors(operationContext.Compilation, operationContext.CancellationToken) &&
                        binary.OperatorMethod == null)
                    {
                        if (left.Kind == OperationKind.LocalReference)
                        {
                            var leftLocal = ((ILocalReferenceOperation)left).Local;
                            if (leftLocal.Name == "x")
                            {
                                if (right.Kind == OperationKind.Literal)
                                {
                                    var rightValue = right.ConstantValue;
                                    if (rightValue.HasValue && rightValue.Value is int && (int)rightValue.Value == 10)
                                    {
                                        operationContext.ReportDiagnostic(
                                            Diagnostic.Create(BinaryOperatorDescriptor,
                                            binary.Syntax.GetLocation(),
                                            binary.OperatorKind.ToString()));
                                    }
                                }
                            }
                        }
                    }
                },
                OperationKind.Binary);

            context.RegisterOperationAction(
                (operationContext) =>
                {
                    var unary = (IUnaryOperation)operationContext.Operation;
                    var operand = unary.Operand;
                    if (operand.Kind == OperationKind.LocalReference)
                    {
                        var operandLocal = ((ILocalReferenceOperation)operand).Local;
                        if (operandLocal.Name == "x")
                        {
                            if (!operand.HasErrors(operationContext.Compilation, operationContext.CancellationToken) && unary.OperatorMethod == null)
                            {
                                operationContext.ReportDiagnostic(
                                    Diagnostic.Create(UnaryOperatorDescriptor,
                                        unary.Syntax.GetLocation(),
                                        unary.OperatorKind.ToString()));
                            }
                        }
                    }
                },
                OperationKind.Unary);
        }
    }

    public class NullOperationSyntaxTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        // We should not see this warning triggered by any code
        public static readonly DiagnosticDescriptor NullOperationSyntaxDescriptor = new DiagnosticDescriptor(
            "NullOperationSyntax",
            "null operation Syntax found",
            "An IOperation with Syntax property of value null is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // since we don't expect to see the first diagnostic, we created this one to make sure
        // the test didn't pass because the analyzer crashed.
        public static readonly DiagnosticDescriptor ParamsArrayOperationDescriptor = new DiagnosticDescriptor(
            "ParamsArray",
            "Params array argument found",
            "A params array argument is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(NullOperationSyntaxDescriptor, ParamsArrayOperationDescriptor); }
        }
        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                (operationContext) =>
                {
                    var nullList = new List<IOperation>();
                    var paramsList = new List<IOperation>();
                    var collector = new Walker(nullList, paramsList);
                    collector.Visit(operationContext.Operation);

                    foreach (var nullSyntaxOperation in nullList)
                    {
                        operationContext.ReportDiagnostic(
                            Diagnostic.Create(NullOperationSyntaxDescriptor, null));
                    }
                    foreach (var paramsarrayArgumentOperation in paramsList)
                    {
                        operationContext.ReportDiagnostic(
                            Diagnostic.Create(ParamsArrayOperationDescriptor,
                                              paramsarrayArgumentOperation.Syntax.GetLocation()));
                    }
                },
                OperationKind.Invocation);
        }

        // this OperationWalker collect:
        // 1. all the operation with null Syntax property
        // 2. all the params array argument operations
        private sealed class Walker : OperationWalker
        {
            private readonly List<IOperation> _nullList;
            private readonly List<IOperation> _paramsList;

            public Walker(List<IOperation> nullList, List<IOperation> paramsList)
            {
                _nullList = nullList;
                _paramsList = paramsList;
            }

            public override void Visit(IOperation operation)
            {
                if (operation != null)
                {
                    if (operation.Syntax == null)
                    {
                        _nullList.Add(operation);
                    }
                    if (operation.Kind == OperationKind.Argument)
                    {
                        if (((IArgumentOperation)operation).ArgumentKind == ArgumentKind.ParamArray)
                        {
                            _paramsList.Add(operation);
                        }
                    }
                }
                base.Visit(operation);
            }
        }
    }

    public class InvalidOperatorExpressionTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor InvalidBinaryDescriptor = new DiagnosticDescriptor(
            "InvalidBinary",
            "Invalid binary expression operation with BinaryOperationKind.Invalid",
            "An Invalid binary expression operation with BinaryOperationKind.Invalid is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidUnaryDescriptor = new DiagnosticDescriptor(
            "InvalidUnary",
            "Invalid unary expression operation with UnaryOperationKind.Invalid",
            "An Invalid unary expression operation with UnaryOperationKind.Invalid is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidIncrementDescriptor = new DiagnosticDescriptor(
            "InvalidIncrement",
            "Invalid increment expression operation with ICompoundAssignmentExpression.BinaryOperationKind == BinaryOperationKind.Invalid",
            "An Invalid increment expression operation with ICompoundAssignmentExpression.BinaryOperationKind == BinaryOperationKind.Invalid is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(InvalidBinaryDescriptor,
                                                                                                                  InvalidUnaryDescriptor,
                                                                                                                  InvalidIncrementDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var operation = operationContext.Operation;
                     if (operation.Kind == OperationKind.Binary)
                     {
                         var binary = (IBinaryOperation)operation;
                         if (binary.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                         {
                             operationContext.ReportDiagnostic(Diagnostic.Create(InvalidBinaryDescriptor, binary.Syntax.GetLocation()));
                         }
                     }
                     else if (operation.Kind == OperationKind.Unary)
                     {
                         var unary = (IUnaryOperation)operation;
                         if (unary.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                         {
                             operationContext.ReportDiagnostic(Diagnostic.Create(InvalidUnaryDescriptor, unary.Syntax.GetLocation()));
                         }
                     }
                     else if (operation.Kind == OperationKind.Increment)
                     {
                         var inc = (IIncrementOrDecrementOperation)operation;
                         if (inc.HasErrors(operationContext.Compilation))
                         {
                             operationContext.ReportDiagnostic(Diagnostic.Create(InvalidIncrementDescriptor, inc.Syntax.GetLocation()));
                         }
                     }
                 },
                 OperationKind.Binary,
                 OperationKind.Unary,
                 OperationKind.Increment);
        }
    }

    public class ConditionalAccessOperationTestAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor ConditionalAccessOperationDescriptor = new DiagnosticDescriptor(
           "ConditionalAccessOperation",
           "Conditional access operation found",
           "Conditional access operation was found",
           "Testing",
           DiagnosticSeverity.Warning,
           isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ConditionalAccessInstanceOperationDescriptor = new DiagnosticDescriptor(
           "ConditionalAccessInstanceOperation",
           "Conditional access instance operation found",
           "Conditional access instance operation was found",
           "Testing",
           DiagnosticSeverity.Warning,
           isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(ConditionalAccessOperationDescriptor, ConditionalAccessInstanceOperationDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IConditionalAccessOperation conditionalAccess = (IConditionalAccessOperation)operationContext.Operation;
                     if (conditionalAccess.WhenNotNull != null && conditionalAccess.Operation != null)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(ConditionalAccessOperationDescriptor, conditionalAccess.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.ConditionalAccess);

            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IConditionalAccessInstanceOperation conditionalAccessInstance = (IConditionalAccessInstanceOperation)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(ConditionalAccessInstanceOperationDescriptor, conditionalAccessInstance.Syntax.GetLocation()));
                 },
                 OperationKind.ConditionalAccessInstance);

            // https://github.com/dotnet/roslyn/issues/21294
            //context.RegisterOperationAction(
            //    (operationContext) =>
            //    {
            //        IPlaceholderExpression placeholder = (IPlaceholderExpression)operationContext.Operation;
            //        operationContext.ReportDiagnostic(Diagnostic.Create(ConditionalAccessInstanceOperationDescriptor, placeholder.Syntax.GetLocation()));
            //    },
            //    OperationKind.PlaceholderExpression);
        }
    }

    public class ConversionExpressionCSharpTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor InvalidConversionExpressionDescriptor = new DiagnosticDescriptor(
            "InvalidConversionExpression",
            "Invalid conversion expression",
            "Invalid conversion expression.",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(InvalidConversionExpressionDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var conversion = (IConversionOperation)operationContext.Operation;
                     if (conversion.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(InvalidConversionExpressionDescriptor, conversion.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.Conversion);
        }
    }

    public class ForLoopConditionCrashVBTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor ForLoopConditionCrashDescriptor = new DiagnosticDescriptor(
            "ForLoopConditionCrash",
            "Ensure ForLoopCondition property doesn't crash",
            "Ensure ForLoopCondition property doesn't crash",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(ForLoopConditionCrashDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     ILoopOperation loop = (ILoopOperation)operationContext.Operation;
                     if (loop.LoopKind == LoopKind.ForTo)
                     {
                         var forLoop = (IForToLoopOperation)loop;
                         var forCondition = forLoop.LimitValue;

                         if (forCondition.HasErrors(operationContext.Compilation, operationContext.CancellationToken))
                         {
                             // Generate a warning to prove we didn't crash
                             operationContext.ReportDiagnostic(Diagnostic.Create(ForLoopConditionCrashDescriptor, forLoop.LimitValue.Syntax.GetLocation()));
                         }
                     }
                 },
                 OperationKind.Loop);
        }
    }

    public class TrueFalseUnaryOperationTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor UnaryTrueDescriptor = new DiagnosticDescriptor(
            "UnaryTrue",
            "An unary True operation is found",
            "A unary True operation is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnaryFalseDescriptor = new DiagnosticDescriptor(
            "UnaryFalse",
            "An unary False operation is found",
            "A unary False operation is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(UnaryTrueDescriptor, UnaryFalseDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var unary = (IUnaryOperation)operationContext.Operation;
                     if (unary.OperatorKind == UnaryOperatorKind.True)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(UnaryTrueDescriptor, unary.Syntax.GetLocation()));
                     }
                     else if (unary.OperatorKind == UnaryOperatorKind.False)
                     {
                         operationContext.ReportDiagnostic(Diagnostic.Create(UnaryFalseDescriptor, unary.Syntax.GetLocation()));
                     }
                 },
                 OperationKind.Unary);
        }
    }

    public class AssignmentOperationSyntaxTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor AssignmentOperationDescriptor = new DiagnosticDescriptor(
            "AssignmentOperation",
            "An assignment operation is found",
            "An assignment operation is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AssignmentSyntaxDescriptor = new DiagnosticDescriptor(
            "AssignmentSyntax",
            "An assignment syntax is found",
            "An assignment syntax is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(AssignmentOperationDescriptor, AssignmentSyntaxDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     operationContext.ReportDiagnostic(Diagnostic.Create(AssignmentOperationDescriptor, operationContext.Operation.Syntax.GetLocation()));
                 },
                 OperationKind.SimpleAssignment);

            context.RegisterSyntaxNodeAction(
                 (syntaxContext) =>
                 {

                     syntaxContext.ReportDiagnostic(Diagnostic.Create(AssignmentSyntaxDescriptor, syntaxContext.Node.GetLocation()));
                 },
                 CSharp.SyntaxKind.SimpleAssignmentExpression);
        }
    }

    public class LiteralTestAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor LiteralDescriptor = new DiagnosticDescriptor(
            "Literal",
            "A literal is found",
            "A literal of value {0} is found",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(LiteralDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     var literal = (ILiteralOperation)operationContext.Operation;
                     operationContext.ReportDiagnostic(Diagnostic.Create(LiteralDescriptor, literal.Syntax.GetLocation(), literal.Syntax.ToString()));
                 },
                 OperationKind.Literal);
        }
    }

    // This analyzer is to test operation action registration method in AnalysisContext
    public class AnalysisContextAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor OperationActionDescriptor = new DiagnosticDescriptor(
            "AnalysisContext",
            "An operation related action is invoked",
            "An {0} action is invoked in {1} context.",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(OperationActionDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                (operationContext) =>
                {
                    operationContext.ReportDiagnostic(
                        Diagnostic.Create(OperationActionDescriptor, operationContext.Operation.Syntax.GetLocation(), "Operation", "Analysis"));
                },
                OperationKind.Literal);
        }
    }

    // This analyzer is to test operation action registration method in CompilationStartAnalysisContext
    public class CompilationStartAnalysisContextAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor OperationActionDescriptor = new DiagnosticDescriptor(
            "CompilationStartAnalysisContext",
            "An operation related action is invoked",
            "An {0} action is invoked in {1} context.",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(OperationActionDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(
                (compilationStartContext) =>
                {
                    compilationStartContext.RegisterOperationAction(
                        (operationContext) =>
                        {
                            operationContext.ReportDiagnostic(
                                Diagnostic.Create(OperationActionDescriptor, operationContext.Operation.Syntax.GetLocation(), "Operation", "CompilationStart within Analysis"));
                        },
                        OperationKind.Literal);
                });
        }
    }

    // This analyzer is to test GetOperation method in SemanticModel
    public class SemanticModelAnalyzer : DiagnosticAnalyzer
    {
        private const string ReliabilityCategory = "Reliability";

        public static readonly DiagnosticDescriptor GetOperationDescriptor = new DiagnosticDescriptor(
            "GetOperation",
            "An IOperation is returned by SemanticModel",
            "An IOperation is returned by SemanticModel.",
            ReliabilityCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(GetOperationDescriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                (syntaxContext) =>
                {
                    var node = syntaxContext.Node;
                    var model = syntaxContext.SemanticModel;
                    if (model.GetOperation(node) != null)
                    {
                        syntaxContext.ReportDiagnostic(Diagnostic.Create(GetOperationDescriptor, node.GetLocation()));
                    }
                },
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression);

            context.RegisterSyntaxNodeAction(
                (syntaxContext) =>
                {
                    var node = syntaxContext.Node;
                    var model = syntaxContext.SemanticModel;
                    if (model.GetOperation(node) != null)
                    {
                        syntaxContext.ReportDiagnostic(Diagnostic.Create(GetOperationDescriptor, node.GetLocation()));
                    }
                },
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NumericLiteralExpression);
        }
    }
}
