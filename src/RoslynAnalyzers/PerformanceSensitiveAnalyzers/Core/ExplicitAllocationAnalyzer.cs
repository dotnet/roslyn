// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers
{
    using static PerformanceSensitiveAnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class ExplicitAllocationAnalyzer : AbstractAllocationAnalyzer
    {
        public const string ArrayCreationRuleId = "HAA0501";
        public const string ObjectCreationRuleId = "HAA0502";
        public const string AnonymousObjectCreationRuleId = "HAA0503";
        // HAA0504 is retired and should not be reused
        // HAA0505 is retired and should not be reused
        public const string LetCauseRuleId = "HAA0506";

        private static readonly LocalizableString s_localizableArrayCreationRuleTitleAndMessage = CreateLocalizableResourceString(nameof(NewArrayRuleTitleAndMessage));
        private static readonly LocalizableString s_localizableObjectCreationRuleTitleAndMessage = CreateLocalizableResourceString(nameof(NewObjectRuleTitleAndMessage));
        private static readonly LocalizableString s_localizablAnonymousObjectCreationRuleTitleAndMessage = CreateLocalizableResourceString(nameof(AnonymousNewObjectRuleTitleAndMessage));
        private static readonly LocalizableString s_localizableLetCauseRuleTitleAndMessage = CreateLocalizableResourceString(nameof(LetCauseRuleTitleAndMessage));

        internal static readonly DiagnosticDescriptor ArrayCreationRule = new(
            ArrayCreationRuleId,
            s_localizableArrayCreationRuleTitleAndMessage,
            s_localizableArrayCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ObjectCreationRule = new(
            ObjectCreationRuleId,
            s_localizableObjectCreationRuleTitleAndMessage,
            s_localizableObjectCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor AnonymousObjectCreationRule = new(
            AnonymousObjectCreationRuleId,
            s_localizablAnonymousObjectCreationRuleTitleAndMessage,
            s_localizablAnonymousObjectCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/en-us/library/bb397696.aspx");

        internal static readonly DiagnosticDescriptor LetCauseRule = new(
            LetCauseRuleId,
            s_localizableLetCauseRuleTitleAndMessage,
            s_localizableLetCauseRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            ArrayCreationRule,
            ObjectCreationRule,
            AnonymousObjectCreationRule,
            LetCauseRule);

        protected override ImmutableArray<OperationKind> Operations { get; } = ImmutableArray.Create(
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
                if (context.Operation.Parent?.Kind == OperationKind.Attribute)
                {
                    // Don't report attribute usage as creating a new instance
                    return;
                }

                if (context.Operation.Type?.IsReferenceType == true)
                {
                    context.ReportDiagnostic(context.Operation.CreateDiagnostic(ObjectCreationRule, EmptyMessageArgs));
                    return;
                }

                if (context.Operation.Parent is IConversionOperation conversion)
                {
                    if (conversion.Type?.IsReferenceType == true && conversion.Operand.Type?.IsValueType == true)
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
