// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class DoNotCreateTasksWithoutTaskSchedulerAnalyzer<TSyntaxKind> : DiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DoNotCreateTasksWithoutTaskSchedulerMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DoNotCreateTasksWithoutTaskSchedulerTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DoNotCreateTasksWithoutTaskSchedulerDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        internal static readonly DiagnosticDescriptor DoNotCreateTasksWithoutTaskSchedulerAnalyzerDescriptor = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DoNotCreateTasksWithoutTaskSchedulerRuleId,
            s_localizableTitle,
            s_localizableMessage,
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(DoNotCreateTasksWithoutTaskSchedulerAnalyzerDescriptor);
            }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Check if TPL is available before actually doing the searches
                var taskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                var taskFactoryType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskFactory");
                var taskSchedulerType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskScheduler");
                if (taskType != null && taskFactoryType != null && taskSchedulerType != null)
                {
                    compilationContext.RegisterSyntaxNodeAction(syntaxNodeContext => AnalyzeNode(syntaxNodeContext, taskType, taskFactoryType, taskSchedulerType), ImmutableArray.Create(InvocationExpressionSyntaxKind));
                }
            });
        }

        protected abstract TSyntaxKind InvocationExpressionSyntaxKind { get; }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol taskType, INamedTypeSymbol taskFactoryType, INamedTypeSymbol taskSchedulerType)
        {
            var methodSymbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            if (!IsMethodOfInterest(methodSymbol, taskType, taskFactoryType))
            {
                return;
            }

            // We want to ensure that all overloads called are explicitly taking a task scheduler
            if (methodSymbol.Parameters.Any(p => p.Type.Equals(taskSchedulerType)))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(DoNotCreateTasksWithoutTaskSchedulerAnalyzerDescriptor, context.Node.GetLocation(), methodSymbol.Name));
        }

        private bool IsMethodOfInterest(IMethodSymbol methodSymbol, INamedTypeSymbol taskType, INamedTypeSymbol taskFactoryType)
        {
            // Check if it's a method of Task or a derived type (for Task<T>)
            if ((methodSymbol.ContainingType.Equals(taskType) ||
                 taskType.Equals(methodSymbol.ContainingType.BaseType)) &&
                methodSymbol.Name == "ContinueWith")
            {
                return true;
            }

            if (methodSymbol.ContainingType.Equals(taskFactoryType) &&
                methodSymbol.Name == "StartNew")
            {
                return true;
            }

            return false;
        }
    }
}
