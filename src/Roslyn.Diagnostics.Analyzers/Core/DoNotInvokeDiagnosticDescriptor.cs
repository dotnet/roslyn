// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class DiagnosticDescriptorAccessAnalyzer<TSyntaxKind, TMemberAccessExpressionSyntax> : DiagnosticAnalyzer
        where TSyntaxKind : struct
        where TMemberAccessExpressionSyntax : SyntaxNode
    {
        private const string DiagnosticTypeFullName = "Microsoft.CodeAnalysis.Diagnostic";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotInvokeDiagnosticDescriptorTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotInvokeDiagnosticDescriptorMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotInvokeDiagnosticDescriptorDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static readonly DiagnosticDescriptor DoNotRealizeDiagnosticDescriptorRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DoNotAccessDiagnosticDescriptorRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsPerformance,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DoNotRealizeDiagnosticDescriptorRule);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeNode, SimpleMemberAccessExpressionKind);
        }

        protected abstract TSyntaxKind SimpleMemberAccessExpressionKind { get; }

        protected abstract SyntaxNode GetLeftOfMemberAccess(TMemberAccessExpressionSyntax memberAccess);
        protected abstract SyntaxNode GetRightOfMemberAccess(TMemberAccessExpressionSyntax memberAccess);
        protected abstract bool IsThisOrBaseOrMeOrMyBaseExpression(SyntaxNode node);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (TMemberAccessExpressionSyntax)context.Node;
            SyntaxNode right = GetRightOfMemberAccess(memberAccess);
            if (right.ToString() != nameof(Diagnostic.Descriptor))
            {
                return;
            }

            SyntaxNode left = GetLeftOfMemberAccess(memberAccess);
            ITypeSymbol leftType = context.SemanticModel.GetTypeInfo(left).Type;
            if (leftType != null && leftType.ToDisplayString() == DiagnosticTypeFullName && !IsThisOrBaseOrMeOrMyBaseExpression(left))
            {
                string nameOfMember = string.Empty;
                if (memberAccess.Parent is TMemberAccessExpressionSyntax parentMemberAccess)
                {
                    SyntaxNode member = GetRightOfMemberAccess(parentMemberAccess);
                    nameOfMember = " '" + member.ToString() + "'";
                }

                memberAccess.CreateDiagnostic(DoNotRealizeDiagnosticDescriptorRule, nameof(Diagnostic.Descriptor), nameof(Diagnostic), nameOfMember);
            }
        }
    }
}
