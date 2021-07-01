// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotCallGetTestAccessor : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotCallGetTestAccessorTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotCallGetTestAccessorMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotCallGetTestAccessorDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static readonly DiagnosticDescriptor DoNotCallGetTestAccessorRule = new(
            RoslynDiagnosticIds.DoNotCallGetTestAccessorRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DoNotCallGetTestAccessorRule);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterOperationBlockStartAction(context =>
            {
                if (!string.Equals(context.OwningSymbol.Name, TestAccessorHelper.GetTestAccessorMethodName, StringComparison.Ordinal)
                    && !string.Equals(context.OwningSymbol.ContainingType?.Name, TestAccessorHelper.TestAccessorTypeName, StringComparison.Ordinal))
                {
                    context.RegisterOperationAction(HandleMemberReference, OperationKinds.MemberReference);
                    context.RegisterOperationAction(HandleInvocation, OperationKind.Invocation);
                    context.RegisterOperationAction(HandleObjectCreation, OperationKind.ObjectCreation);
                }
            });
        }

        private void HandleMemberReference(OperationAnalysisContext context)
        {
            var memberReference = (IMemberReferenceOperation)context.Operation;
            if (string.Equals(memberReference.Member.ContainingType?.Name, TestAccessorHelper.TestAccessorTypeName, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(memberReference.Syntax.CreateDiagnostic(DoNotCallGetTestAccessorRule));
            }
        }

        private void HandleInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (invocation.TargetMethod.Name.Equals(TestAccessorHelper.GetTestAccessorMethodName, StringComparison.Ordinal))
            {
                // Calling a type's GetTestAccessor method
                context.ReportDiagnostic(invocation.Syntax.CreateDiagnostic(DoNotCallGetTestAccessorRule));
            }
            else if (string.Equals(invocation.TargetMethod.ContainingType?.Name, TestAccessorHelper.TestAccessorTypeName, StringComparison.Ordinal))
            {
                // Calling a static method of a TestAccessor type
                context.ReportDiagnostic(invocation.Syntax.CreateDiagnostic(DoNotCallGetTestAccessorRule));
            }
        }

        private void HandleObjectCreation(OperationAnalysisContext context)
        {
            var objectCreation = (IObjectCreationOperation)context.Operation;
            if (objectCreation.Type.Name.Equals(TestAccessorHelper.TestAccessorTypeName, StringComparison.Ordinal))
            {
                // Directly constructing a TestAccessor instance
                context.ReportDiagnostic(objectCreation.Syntax.CreateDiagnostic(DoNotCallGetTestAccessorRule));
            }
        }
    }
}
