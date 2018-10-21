// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal class CSharpUseIndexOperatorDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseIndexOperatorDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseIndexOperatorDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_index_operator), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Indexing_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var compilation = compilationContext.Compilation;
                var stringType = compilation.GetSpecialType(SpecialType.System_String);

                var stringIndexer =
                    stringType.GetMembers()
                              .OfType<IPropertySymbol>()
                              .Where(p => IsStringIndexer(p))
                              .FirstOrDefault();

                var stringLength =
                    stringType.GetMembers()
                              .OfType<IPropertySymbol>()
                              .Where(p => p.Name == nameof(string.Length))
                              .FirstOrDefault();


                if (stringIndexer != null && stringLength != null)
                {
                    compilationContext.RegisterOperationAction(
                        c => AnalyzePropertyReference(c, stringIndexer, stringLength),
                        OperationKind.PropertyReference);
                }
            });
        }

        private void AnalyzePropertyReference(
            OperationAnalysisContext context,
            IPropertySymbol stringIndexer, IPropertySymbol stringLength)
        {
            var cancellationToken = context.CancellationToken;
            var propertyReference = (IPropertyReferenceOperation)context.Operation;

            if (!stringIndexer.Equals(propertyReference.Property))
            {
                return;
            }

            var syntax = propertyReference.Syntax;
            if (syntax == null)
            {
                return;
            }

            var syntaxTree = syntax.SyntaxTree;
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

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferIndexOperator);
            if (!option.Value)
            {
                return;
            }

            // look for `s[s.Length - index.Value]` and convert to `s[^index]`

            // Needs to have the one arg for `[s.Length - index.Value]`
            if (propertyReference.Instance is null ||
                propertyReference.Arguments.Length != 1)
            {
                return;
            }

            // Arg needs to be a subtraction for: `s.Length - index.Value`
            var arg = propertyReference.Arguments[0];
            if (!(arg.Value is IBinaryOperation binaryOperation) ||
                binaryOperation.OperatorKind != BinaryOperatorKind.Subtract)
            {
                return;
            }

            // Left side of the subtraction needs to be `s.Length`.  First make
            // sure we're referencing String.Length.
            if (!(binaryOperation.LeftOperand is IPropertyReferenceOperation leftPropertyRef) ||
                leftPropertyRef.Instance is null ||
                !stringLength.Equals(leftPropertyRef.Property))
            {
                return;
            }

            // make sure that we're indexing and getting the length off hte same value:
            // `s[s.Length`
            var indexInstanceSyntax = propertyReference.Instance.Syntax;
            var lengthInstanceSyntax = leftPropertyRef.Instance.Syntax;

            var syntaxFacts = CSharpSyntaxFactsService.Instance;
            if (!syntaxFacts.AreEquivalent(indexInstanceSyntax, lengthInstanceSyntax))
            {
                return;
            }

            if (!(propertyReference.Syntax is ElementAccessExpressionSyntax elementAccess))
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(
                binaryOperation.RightOperand.Syntax.GetLocation());

            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    elementAccess.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations,
                    ImmutableDictionary<string, string>.Empty));
        }

        private static bool IsStringIndexer(IPropertySymbol property)
            => property.IsIndexer && property.Parameters.Length == 1 && property.Parameters[0].Type.SpecialType == SpecialType.System_Int32;
    }
}
