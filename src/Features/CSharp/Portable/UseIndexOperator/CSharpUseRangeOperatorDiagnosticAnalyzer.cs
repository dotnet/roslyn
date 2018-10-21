// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    using static Helpers;

    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public const string StartFromEnd = nameof(StartFromEnd);
        public const string EndFromEnd = nameof(EndFromEnd);
        public const string OmitStart = nameof(OmitStart);
        public const string OmitEnd = nameof(OmitEnd);

        public CSharpUseRangeOperatorDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseRangeOperatorDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_range_operator), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources._0_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        /// <summary>
        /// Look for methods like "ContainingType Slice(int start, int length)"
        /// </summary>
        private static bool IsSliceLikeMethod(IMethodSymbol method)
            => IsPublicInstance(method) &&
               method.Parameters.Length == 2 &&
               (method.Parameters[0].Name == "start" || method.Parameters[0].Name == "startIndex") &&
               (method.Parameters[1].Name == "count" || method.Parameters[1].Name == "length") &&
               method.ContainingType.Equals(method.ReturnType);

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var typeChecker = new TypeChecker(compilationContext.Compilation);
                compilationContext.RegisterOperationAction(
                    c => AnalyzeInvocation(c, typeChecker),
                    OperationKind.Invocation);
            });
        }

        private void AnalyzeInvocation(
            OperationAnalysisContext context, TypeChecker typeChecker)
        {
            var cancellationToken = context.CancellationToken;
            var invocation = (IInvocationOperation)context.Operation;

            var invocationSyntax = invocation.Syntax;
            if (invocationSyntax is null || invocationSyntax.Kind() != SyntaxKind.InvocationExpression)
            {
                return;
            }

            var targetMethod = invocation.TargetMethod;
            if (!IsSliceLikeMethod(invocation.TargetMethod))
            {
                return;
            }

            if (!typeChecker.TryGetMemberInfo(targetMethod.ContainingType, out var memberInfo) ||
                !targetMethod.Equals(memberInfo.SliceLikeMethod))
            {
                return;
            }

            var syntaxTree = invocationSyntax.SyntaxTree;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            //if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
            //{
            //    return;
            //}

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferRangeOperator);
            if (!option.Value)
            {
                return;
            }

            // look for `s.SliceMethod(start, end - start)` and convert to `s[Range]`

            // Needs to have the two args for `start` and `end - start`
            if (invocation.Instance is null ||
                invocation.Instance.Syntax is null ||
                invocation.Arguments.Length != 2)
            {
                return;
            }

            // Arg2 needs to be a subtraction for: `end - start`
            var arg2 = invocation.Arguments[1];
            if (!(arg2.Value is IBinaryOperation binaryOperation) ||
                binaryOperation.OperatorKind != BinaryOperatorKind.Subtract)
            {
                return;
            }

            var arg1 = invocation.Arguments[0];
            var arg1Syntax = arg1.Value.Syntax;

            var subtractRightSyntax = binaryOperation.RightOperand.Syntax;

            var syntaxFacts = CSharpSyntaxFactsService.Instance;
            if (!syntaxFacts.AreEquivalent(arg1Syntax, subtractRightSyntax))
            {
                return;
            }

            var startOperation = arg1.Value;
            var endOperation = binaryOperation.LeftOperand;

            // var start = range.Start.FromEnd ? array.Length - range.Start.Value : range.Start.Value;
            // var end = range.End.FromEnd ? array.Length - range.End.Value : range.End.Value;

            var properties = ImmutableDictionary<string, string>.Empty;

            var lengthOrCountProp = memberInfo.LengthOrCountProperty;
            if (IsFromEnd(lengthOrCountProp, invocation.Instance, ref startOperation))
            {
                properties = properties.Add(StartFromEnd, StartFromEnd);
            }

            if (IsFromEnd(lengthOrCountProp, invocation.Instance, ref endOperation))
            {
                properties = properties.Add(EndFromEnd, EndFromEnd);
            }

            // If the range operation goes to 'instance.Length' then we can just leave off the end
            // part of the range.  i.e. `start..`
            if (IsInstanceLengthCheck(lengthOrCountProp, invocation.Instance, endOperation))
            {
                properties = properties.Add(OmitEnd, OmitEnd);
            }

            // If we're starting the range operation from 0, then we can just leave off the start of
            // the range. i.e. `..end`
            if (startOperation.ConstantValue.HasValue &&
                startOperation.ConstantValue.Value is 0)
            {
                properties = properties.Add(OmitStart, OmitStart);
            }

            var additionalLocations = ImmutableArray.Create(
                startOperation.Syntax.GetLocation(),
                endOperation.Syntax.GetLocation());

            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    invocationSyntax.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations,
                    properties,
                    memberInfo.SliceLikeMethod.Name));
        }

        private bool IsFromEnd(
            IPropertySymbol lengthOrCountProp, IOperation instance, ref IOperation rangeOperation)
        {
            // check if its the form: `stringExpr.Length - value`
            if (rangeOperation is IBinaryOperation binaryOperation &&
                binaryOperation.OperatorKind == BinaryOperatorKind.Subtract &&
                IsInstanceLengthCheck(lengthOrCountProp, instance, binaryOperation.LeftOperand))
            {
                rangeOperation = binaryOperation.RightOperand;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Checks if this is an expression `expr.Length` where `expr` is equivalent to
        /// the instance we were calling .Substring off of.
        /// </summary>
        private bool IsInstanceLengthCheck(
            IPropertySymbol lengthOrCountProp, IOperation instance, IOperation operation)
        {
            var syntaxFacts = CSharpSyntaxFactsService.Instance;
            return
                operation is IPropertyReferenceOperation propertyRef &&
                lengthOrCountProp.Equals(propertyRef.Property) &&
                propertyRef.Instance != null &&
                syntaxFacts.AreEquivalent(instance.Syntax, propertyRef.Instance.Syntax);
        }
    }
}
