// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using static Helpers;

    /// <summary>
    /// Analyzer that looks for code like: 
    /// 
    /// 1) `s[s.Length - n]` and offers to change that to `s[^n]`. and.
    /// 2) `s.Get(s.Length - n)` and offers to change that to `s.Get(^n)`
    ///
    /// In order to do convert between indexers, the type must look 'indexable'.  Meaning, it must
    /// have an int-returning property called 'Length' or 'Count', and it must have both an
    /// int-indexer, and a System.Index-indexer.  In order to convert between methods, the type
    /// must have identical overloads except that one takes an int, and the other a System.Index.
    ///
    /// It is assumed that if the type follows this shape that it is well behaved and that this
    /// transformation will preserve semantics.  If this assumption is not good in practice, we
    /// could always limit the feature to only work on a whitelist of known safe types.
    /// 
    /// Note that this feature only works if the code literally has `expr1.Length - expr2`.  If
    /// code has this, and is calling into a method that takes either an int or a System.Index,
    /// it feels very safe to assume this is well behaved and switching to `^expr2` is going to
    /// preserve semantics.
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
                // We're going to be checking every property-reference and invocation in the
                // compilation. Cache information we compute in this object so we don't have to
                // continually recompute it.
                var infoCache = new InfoCache(startContext.Compilation);
                context.RegisterOperationAction(
                    c => AnalyzePropertyReference(c, infoCache),
                    OperationKind.PropertyReference);

                context.RegisterOperationAction(
                    c => AnalyzeInvocation(c, infoCache),
                    OperationKind.Invocation);
            });
        }

        private void AnalyzeInvocation(OperationAnalysisContext context, InfoCache infoCache)
        {
            var cancellationToken = context.CancellationToken;
            var invocationOperation = (IInvocationOperation)context.Operation;

            // Make sure we're actually on an invocation something like `s.Get(...)`.
            var invocationSyntax = invocationOperation.Syntax as InvocationExpressionSyntax;
            if (invocationSyntax is null)
            {
                return;
            }

            var instance = invocationOperation.Instance;
            var targetMethod = invocationOperation.TargetMethod;
            var arguments = invocationOperation.Arguments;

            AnalyzeInvokedMember(
                context, infoCache, invocationOperation,
                instance, targetMethod, arguments, cancellationToken);
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

            var instance = propertyReference.Instance;
            var targetMethod = property.GetMethod;
            var arguments = propertyReference.Arguments;

            AnalyzeInvokedMember(
                context, infoCache, propertyReference, 
                instance, targetMethod, arguments, cancellationToken);
        }

        private void AnalyzeInvokedMember(
            OperationAnalysisContext context, InfoCache infoCache, IOperation invocation,
            IOperation instance, IMethodSymbol targetMethod, ImmutableArray<IArgumentOperation> arguments, 
            CancellationToken cancellationToken)
        {
            // Only supported on C# 8 and above.
            var syntaxTree = invocation.Syntax.SyntaxTree;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
            {
                return;
            }

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

            // look for `s[s.Length - value]` or `s.Get(s.Length- value)`.

            // Needs to have the one arg for `s.Length - value`, and that arg needs to be
            // a subtraction.
            if (instance is null ||
                arguments.Length != 1 ||
                !IsSubtraction(arguments[0].Value, out var subtraction))
            {
                return;
            }

            // Ok, looks promising.  We're indexing in with some subtraction expression. Examine the
            // type this indexer is in to see if there's another member that takes a System.Index
            // that we can convert to.
            //
            // Also ensure that the left side of the subtraction : `s.Length - value` is actually
            // getting the length off the same instance we're indexing into.
            if (!infoCache.TryGetMemberInfo(targetMethod, out var memberInfo) ||
                !IsInstanceLengthCheck(memberInfo.LengthLikeProperty, instance, subtraction.LeftOperand))
            {
                return;
            }

            // Everything looks good.  We can update this to use the System.Index member instead.
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
