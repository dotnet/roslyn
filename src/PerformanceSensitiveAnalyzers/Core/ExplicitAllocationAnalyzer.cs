// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class ExplicitAllocationAnalyzer : AbstractAllocationAnalyzer
    {
        public const string ArrayCreationRuleId = "HAA0501";
        public const string ObjectCreationRuleId = "HAA0502";
        public const string AnonymousObjectCreationRuleId = "HAA0503";
        // HAA0504 is retired and should not be reused
        // HAA0505 is retired and should not be reused
        public const string LetCauseRuleId = "HAA0506";

        private static readonly LocalizableString s_localizableArrayCreationRuleTitleAndMessage = new LocalizableResourceString(nameof(PerformanceSensitiveAnalyzersResources.NewArrayRuleTitleAndMessage), PerformanceSensitiveAnalyzersResources.ResourceManager, typeof(PerformanceSensitiveAnalyzersResources));
        private static readonly LocalizableString s_localizableObjectCreationRuleTitleAndMessage = new LocalizableResourceString(nameof(PerformanceSensitiveAnalyzersResources.NewObjectRuleTitleAndMessage), PerformanceSensitiveAnalyzersResources.ResourceManager, typeof(PerformanceSensitiveAnalyzersResources));
        private static readonly LocalizableString s_localizablAnonymousObjectCreationRuleTitleAndMessage = new LocalizableResourceString(nameof(PerformanceSensitiveAnalyzersResources.AnonymousNewObjectRuleTitleAndMessage), PerformanceSensitiveAnalyzersResources.ResourceManager, typeof(PerformanceSensitiveAnalyzersResources));
        private static readonly LocalizableString s_localizableLetCauseRuleTitleAndMessage = new LocalizableResourceString(nameof(PerformanceSensitiveAnalyzersResources.LetCauseRuleTitleAndMessage), PerformanceSensitiveAnalyzersResources.ResourceManager, typeof(PerformanceSensitiveAnalyzersResources));

        internal static DiagnosticDescriptor ArrayCreationRule = new(
            ArrayCreationRuleId,
            s_localizableArrayCreationRuleTitleAndMessage,
            s_localizableArrayCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor ObjectCreationRule = new(
            ObjectCreationRuleId,
            s_localizableObjectCreationRuleTitleAndMessage,
            s_localizableObjectCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor AnonymousObjectCreationRule = new(
            AnonymousObjectCreationRuleId,
            s_localizablAnonymousObjectCreationRuleTitleAndMessage,
            s_localizablAnonymousObjectCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/en-us/library/bb397696.aspx");

        internal static DiagnosticDescriptor LetCauseRule = new(
            LetCauseRuleId,
            s_localizableLetCauseRuleTitleAndMessage,
            s_localizableLetCauseRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            ArrayCreationRule,
            ObjectCreationRule,
            AnonymousObjectCreationRule,
            LetCauseRule);

        protected override ImmutableArray<OperationKind> Operations => ImmutableArray.Create(
            OperationKind.ArrayCreation,
            OperationKind.ObjectCreation,
            OperationKind.AnonymousObjectCreation,
            OperationKind.DelegateCreation,
            OperationKind.TypeParameterObjectCreation);

        protected override void AnalyzeNode(OperationAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            if (context.Operation is IArrayCreationOperation arrayCreation)
            {
                // The implicit case is handled by HAA0101
                if (!arrayCreation.IsImplicit)
                {
                    context.ReportDiagnostic(context.Operation.CreateDiagnostic(ArrayCreationRule, EmptyMessageArgs));
                }

                return;
            }

            if (context.Operation is IObjectCreationOperation or ITypeParameterObjectCreationOperation)
            {
                if (context.Operation.Type.IsReferenceType)
                {
                    context.ReportDiagnostic(context.Operation.CreateDiagnostic(ObjectCreationRule, EmptyMessageArgs));
                    return;
                }

                if (context.Operation.Parent is IConversionOperation conversion)
                {
                    if (conversion.Type.IsReferenceType && conversion.Operand.Type.IsValueType)
                    {
                        context.ReportDiagnostic(context.Operation.CreateDiagnostic(ObjectCreationRule, EmptyMessageArgs));
                    }

                    return;
                }
            }

            if (context.Operation is IAnonymousObjectCreationOperation)
            {
                if (context.Operation.IsImplicit)
                {
                    context.ReportDiagnostic(context.Operation.CreateDiagnostic(LetCauseRule, EmptyMessageArgs));
                }
                else
                {
                    context.ReportDiagnostic(context.Operation.CreateDiagnostic(AnonymousObjectCreationRule, EmptyMessageArgs));
                }

                return;
            }

            if (context.Operation is IDelegateCreationOperation delegateCreationOperation)
            {
                // The implicit case is handled by HAA0603
                if (!delegateCreationOperation.IsImplicit)
                {
                    context.ReportDiagnostic(context.Operation.CreateDiagnostic(ObjectCreationRule, EmptyMessageArgs));
                }

                return;
            }
        }
    }
}