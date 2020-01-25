﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.QualifyMemberAccess;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.QualifyMemberAccess), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal class CSharpQualifyMemberAccessCodeFixProvider : AbstractQualifyMemberAccessCodeFixprovider<SimpleNameSyntax, InvocationExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpQualifyMemberAccessCodeFixProvider()
        {
        }

        protected override SimpleNameSyntax GetNode(Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            switch (node)
            {
                case SimpleNameSyntax simpleNameSyntax:
                    return simpleNameSyntax;
                case InvocationExpressionSyntax invocationExpressionSyntax:
                    return invocationExpressionSyntax.Expression as SimpleNameSyntax;
                default:
                    return null;
            }
        }

        protected override string GetTitle() => CSharpFeaturesResources.Add_this;
    }
}
