// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers
{
    internal abstract class AbstractAllocationAnalyzer : DiagnosticAnalyzer
    {
        protected abstract ImmutableArray<OperationKind> Operations { get; }

        protected abstract void AnalyzeNode(OperationAnalysisContext context, in PerformanceSensitiveInfo info);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // This analyzer is triggered by an attribute, even if it appears in generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            if (Operations.IsEmpty)
            {
                return;
            }

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var compilation = compilationStartContext.Compilation;
                var attributeSymbol = compilation.GetTypeByMetadataName(AllocationRules.PerformanceSensitiveAttributeName);

                // Bail if PerformanceSensitiveAttribute is not delcared in the compilation.
                if (attributeSymbol == null)
                {
                    return;
                }

                compilationStartContext.RegisterOperationBlockStartAction(blockStartContext =>
                {
                    var checker = new AttributeChecker(attributeSymbol);
                    RegisterOperationAnalysis(blockStartContext, checker);
                });
            });
        }

        private void RegisterOperationAnalysis(OperationBlockStartAnalysisContext operationBlockStartAnalysisContext, AttributeChecker performanceSensitiveAttributeChecker)
        {
            var owningSymbol = operationBlockStartAnalysisContext.OwningSymbol;
            if (!performanceSensitiveAttributeChecker.TryGetContainsPerformanceSensitiveInfo(owningSymbol, out var info))
            {
                return;
            }

            operationBlockStartAnalysisContext.RegisterOperationAction(
                syntaxNodeContext =>
                {
                    AnalyzeNode(syntaxNodeContext, in info);
                },
                Operations);
        }

        protected sealed class AttributeChecker
        {
            private INamedTypeSymbol PerfSensitiveAttributeSymbol { get; }

            public AttributeChecker(INamedTypeSymbol perfSensitiveAttributeSymbol)
            {
                PerfSensitiveAttributeSymbol = perfSensitiveAttributeSymbol;
            }

            public bool TryGetContainsPerformanceSensitiveInfo(ISymbol symbol, out PerformanceSensitiveInfo info)
            {
                if (TryGet(symbol, out info))
                {
                    return true;
                }

                // The attribute might be applied to a property declaration, instead of its accessor declaration.
                if (symbol is IMethodSymbol methodSymbol &&
                    (methodSymbol.MethodKind == MethodKind.PropertyGet || methodSymbol.MethodKind == MethodKind.PropertySet) &&
                    TryGet(methodSymbol.AssociatedSymbol, out info))
                {
                    return true;
                }

                info = default;
                return false;

                bool TryGet(ISymbol s, out PerformanceSensitiveInfo i)
                {
                    var attributes = s.GetAttributes();
                    foreach (var attribute in attributes)
                    {
                        if (attribute.AttributeClass.Equals(PerfSensitiveAttributeSymbol))
                        {
                            i = CreatePerformanceSensitiveInfo(attribute);
                            return true;
                        }
                    }

                    i = default;
                    return false;
                }
            }

            private static PerformanceSensitiveInfo CreatePerformanceSensitiveInfo(AttributeData data)
            {
                var allowCaptures = true;
                var allowGenericEnumeration = true;
                var allowLocks = true;

                foreach (var namedArgument in data.NamedArguments)
                {
                    switch (namedArgument.Key)
                    {
                        case "AllowCaptures":
                            allowCaptures = (bool)namedArgument.Value.Value;
                            break;
                        case "AllowGenericEnumeration":
                            allowGenericEnumeration = (bool)namedArgument.Value.Value;
                            break;
                        case "AllowLocks":
                            allowLocks = (bool)namedArgument.Value.Value;
                            break;
                    }
                }

                return new PerformanceSensitiveInfo(allowCaptures, allowGenericEnumeration, allowLocks);
            }
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types. This type is never used for comparison
        protected readonly struct PerformanceSensitiveInfo
#pragma warning restore CA1815
        {
            public bool AllowCaptures { get; }
            public bool AllowGenericEnumeration { get; }
            public bool AllowLocks { get; }

            public PerformanceSensitiveInfo(
                bool allowCaptures = true,
                bool allowGenericEnumeration = true,
                bool allowLocks = true)
            {
                AllowCaptures = allowCaptures;
                AllowGenericEnumeration = allowGenericEnumeration;
                AllowLocks = allowLocks;
            }
        }
    }
}
