// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class DiagnosticDescriptorAccessAnalyzer<TSyntaxKind, TMemberAccessExpressionSyntax> : AbstractSyntaxNodeAnalyzer<TSyntaxKind>
        where TSyntaxKind : struct
        where TMemberAccessExpressionSyntax : SyntaxNode
    {
        private static readonly string DiagnosticTypeFullName = typeof(Diagnostic).FullName;

        internal static readonly DiagnosticDescriptor DoNotRealizeDiagnosticDescriptorRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DoNotAccessDiagnosticDescriptorRuleId,
            RoslynDiagnosticsResources.DiagnosticDescriptorAccessTitle,
            RoslynDiagnosticsResources.DiagnosticDescriptorAccessMessage,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: RoslynDiagnosticsResources.DiagnosticDescriptorAccessDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        protected sealed override DiagnosticDescriptor Descriptor
        {
            get
            {
                return DoNotRealizeDiagnosticDescriptorRule;
            }
        }

        protected sealed override TSyntaxKind[] SyntaxKindsOfInterest
        {
            get
            {
                return new[] { SimpleMemberAccessExpressionKind };
            }
        }

        protected abstract TSyntaxKind SimpleMemberAccessExpressionKind { get; }

        protected abstract SyntaxNode GetLeftOfMemberAccess(TMemberAccessExpressionSyntax memberAccess);
        protected abstract SyntaxNode GetRightOfMemberAccess(TMemberAccessExpressionSyntax memberAccess);

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (TMemberAccessExpressionSyntax)context.Node;
            var right = GetRightOfMemberAccess(memberAccess);
            if (right.ToString() != nameof(Diagnostic.Descriptor))
            {
                return;
            }

            var left = GetLeftOfMemberAccess(memberAccess);
            var leftType = context.SemanticModel.GetTypeInfo(left).Type;
            if (leftType != null && leftType.ToDisplayString() == DiagnosticTypeFullName)
            {
                var nameOfMember = string.Empty;
                var parentMemberAccess = memberAccess.Parent as TMemberAccessExpressionSyntax;
                if (parentMemberAccess != null)
                {
                    var member = GetRightOfMemberAccess(parentMemberAccess);
                    nameOfMember = " '" + member.ToString() + "'";
                }

                ReportDiagnostic(context, memberAccess, nameof(Diagnostic.Descriptor), nameof(Diagnostic), nameOfMember);
            }
        }
    }
}
