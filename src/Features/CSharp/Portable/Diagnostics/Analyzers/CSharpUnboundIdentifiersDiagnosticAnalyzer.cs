// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.AddImport;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUnboundIdentifiersDiagnosticAnalyzer : UnboundIdentifiersDiagnosticAnalyzerBase<SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IncompleteMemberSyntax>
    {
        private readonly LocalizableString _nameNotInContextMessageFormat =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.The_name_0_does_not_exist_in_the_current_context), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.IncompleteMember);

        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => s_kindsOfInterest;

        protected override DiagnosticDescriptor DiagnosticDescriptor
            => GetDiagnosticDescriptor(IDEDiagnosticIds.UnboundIdentifierId, _nameNotInContextMessageFormat);

        protected override bool IsNameOf(SyntaxNode node)
            => node.Parent is InvocationExpressionSyntax invocation && invocation.IsNameOfInvocation();
    }
}
