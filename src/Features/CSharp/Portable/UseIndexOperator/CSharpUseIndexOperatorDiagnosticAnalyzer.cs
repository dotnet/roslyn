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
    using static Helpers;

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
                // information we compute in this object so we don't have to continually recompute
                // it.
                var infoCache = new InfoCache(startContext.Compilation);
                context.RegisterOperationAction(
                    c => AnalyzePropertyReference(c, infoCache),
                    OperationKind.PropertyReference);
            });
        }

        private void AnalyzePropertyReference(
            OperationAnalysisContext context, InfoCache infoCache)
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
            if (elementAccess is null)
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
            if (optionSet is null)
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
            var lengthLikeProperty = infoCache.GetLengthLikeProperty(indexer.ContainingType);
            if (lengthLikeProperty is null)
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
            if (!IsSubtraction(arg, out var subtraction))
            {
                return;
            }

            // Left side of the subtraction needs to be `s.Length`
            if (!IsInstanceLengthCheck(lengthLikeProperty, propertyReference.Instance, subtraction.LeftOperand))
            {
                return;
            }

            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    subtraction.Syntax.GetLocation(),
                    option.Notification.Severity,
                    ImmutableArray<Location>.Empty,
                    ImmutableDictionary<string, string>.Empty));
        }
    }
}
