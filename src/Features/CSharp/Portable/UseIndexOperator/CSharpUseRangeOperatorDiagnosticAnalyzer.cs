// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal class CSharpUseRangeOperatorDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public const string StartFromEnd = nameof(StartFromEnd);
        public const string EndFromEnd = nameof(EndFromEnd);
        public const string OmitStart = nameof(OmitStart);
        public const string OmitEnd = nameof(OmitEnd);

        public CSharpUseRangeOperatorDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseRangeOperatorDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_range_operator), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Substring_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var compilation = compilationContext.Compilation;
                var stringType = compilation.GetSpecialType(SpecialType.System_String);

                var substringMethod =
                    stringType.GetMembers(nameof(string.Substring))
                              .OfType<IMethodSymbol>()
                              .Where(m => m.Parameters.Length == 2 && m.Parameters.All(p => p.Type.SpecialType == SpecialType.System_Int32))
                              .FirstOrDefault();

                var stringLength =
                    stringType.GetMembers(nameof(string.Length))
                              .OfType<IPropertySymbol>()
                              .Where(p => p.Parameters.IsEmpty)
                              .FirstOrDefault();

                if (substringMethod != null && stringLength != null)
                {
                    compilationContext.RegisterOperationAction(
                        c => AnalyzeInvocation(c, substringMethod, stringLength),
                        OperationKind.Invocation);
                }
            });
        }

        private void AnalyzeInvocation(
            OperationAnalysisContext context,
            IMethodSymbol substringMethod, IPropertySymbol stringLength)
        {
            var cancellationToken = context.CancellationToken;
            var invocation = (IInvocationOperation)context.Operation;

            if (!substringMethod.Equals(invocation.TargetMethod))
            {
                return;
            }

            var invocationSyntax = invocation.Syntax;
            if (invocationSyntax is null || invocationSyntax.Kind() != SyntaxKind.InvocationExpression)
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

            // look for `s.Substring(start, end - start)` and convert to `s[Range]`

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

            if (IsFromEnd(stringLength, invocation.Instance, ref startOperation))
            {
                properties = properties.Add(StartFromEnd, StartFromEnd);
            }

            if (IsFromEnd(stringLength, invocation.Instance, ref endOperation))
            {
                properties = properties.Add(EndFromEnd, EndFromEnd);
            }

            if (IsInstanceLengthCheck(stringLength, invocation.Instance, endOperation))
            {
                properties = properties.Add(OmitEnd, OmitEnd);
            }

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
                    properties));
        }

        private bool IsFromEnd(
            IPropertySymbol stringLength, IOperation stringInstance, ref IOperation rangeOperation)
        {
            // check if its the form: `stringExpr.Length - value`
            if (rangeOperation is IBinaryOperation binaryOperation &&
                binaryOperation.OperatorKind == BinaryOperatorKind.Subtract &&
                IsInstanceLengthCheck(stringLength, stringInstance, binaryOperation.LeftOperand))
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
        private bool IsInstanceLengthCheck(IPropertySymbol stringLength, IOperation stringInstance, IOperation operation)
        {
            var syntaxFacts = CSharpSyntaxFactsService.Instance;
            return
                operation is IPropertyReferenceOperation propertyRef &&
                stringLength.Equals(propertyRef.Property) &&
                propertyRef.Instance != null &&
                syntaxFacts.AreEquivalent(stringInstance.Syntax, propertyRef.Instance.Syntax);
        }

        private static bool IsStringIndexer(IPropertySymbol property)
            => property.IsIndexer && property.Parameters.Length == 1 && property.Parameters[0].Type.SpecialType == SpecialType.System_Int32;
    }
}
