// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    /// <summary>
    /// Analyzer that looks for code like `s[s.Length - n]` and offers to change that to `s[^n]`. In
    /// order to do this, the type must look 'indexable'.  Meaning, it must have an int-returning
    /// property called 'Length' or 'Count', and it must have both an int-indexer, and a
    /// System.Index indexer.
    ///
    /// It is assumed that if the type follows this shape that it is well behaved and that this
    /// transformation will preserve semantics.  If this assumption is not good in practice, we
    /// could always limit the feature to only work on a whitelist of known safe types.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseIndexOperatorDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseIndexOperatorDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseIndexOperatorDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_index_operator), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Indexing_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(startContext =>
            {
                // We're going to be checking every property-reference in the compilation. Cache
                // information we compute in this TypeChecker object so we don't have to continually
                // recompute it.
                var typeChecker = new TypeChecker(startContext.Compilation);
                context.RegisterOperationAction(
                    c => AnalyzePropertyReference(c, typeChecker),
                    OperationKind.PropertyReference);
            });
        }

        private void AnalyzePropertyReference(
            OperationAnalysisContext context, TypeChecker typeChecker)
        {
            var cancellationToken = context.CancellationToken;
            var propertyReference = (IPropertyReferenceOperation)context.Operation;
            var property = propertyReference.Property;

            // Only analyze indexer calls.
            if (!property.IsIndexer)
            {
                return;
            }

            // Make sure we're actually on something like `s[...]`.
            var elementAccess = propertyReference.Syntax as ElementAccessExpressionSyntax;
            if (elementAccess == null)
            {
                return;
            }

            // Only supported on C# 8 and above.
            var syntaxTree = elementAccess.SyntaxTree;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            //if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
            //{
            //    return;
            //}

            // Don't bother analyzing if the user doesn't like using Index/Range operators.
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

            // Needs to be an indexer with a single int argument.
            var indexer = property;
            if (indexer.Parameters.Length != 1 ||
                indexer.Parameters[0].Type.SpecialType != SpecialType.System_Int32)
            {
                return;
            }

            // Make sure this is a type that has both a Length/Count property, as well
            // as an indexer that takes a System.Index. 
            var lengthOrCountProp = typeChecker.GetLengthOrCountProperty(indexer.ContainingType);
            if (lengthOrCountProp == null)
            {
                return;
            }

            // look for `s[s.Length - value]` and convert to `s[^val]`

            // Needs to have the one arg for `[s.Length - value]`
            if (propertyReference.Instance is null ||
                propertyReference.Arguments.Length != 1)
            {
                return;
            }

            // Arg needs to be a subtraction for: `s.Length - value`
            var arg = propertyReference.Arguments[0];
            if (!(arg.Value is IBinaryOperation binaryOperation) ||
                binaryOperation.OperatorKind != BinaryOperatorKind.Subtract)
            {
                return;
            }

            // Left side of the subtraction needs to be `s.Length`
            if (!(binaryOperation.LeftOperand is IPropertyReferenceOperation leftPropertyRef) ||
                leftPropertyRef.Instance is null ||
                !lengthOrCountProp.Equals(leftPropertyRef.Property))
            {
                return;
            }

            // make sure that we're indexing and getting the length off the same value:
            // `s[s.Length`
            var indexInstanceSyntax = propertyReference.Instance.Syntax;
            var lengthInstanceSyntax = leftPropertyRef.Instance.Syntax;

            var syntaxFacts = CSharpSyntaxFactsService.Instance;
            if (!syntaxFacts.AreEquivalent(indexInstanceSyntax, lengthInstanceSyntax))
            {
                return;
            }

            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    binaryOperation.Syntax.GetLocation(),
                    option.Notification.Severity,
                    ImmutableArray<Location>.Empty,
                    ImmutableDictionary<string, string>.Empty));
        }
    }
}
