// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
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
