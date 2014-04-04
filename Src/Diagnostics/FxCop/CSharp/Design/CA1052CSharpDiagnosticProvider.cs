// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopRules.DiagnosticProviders.Design;

namespace Microsoft.CodeAnalysis.CSharp.FxCopRules.DiagnosticProviders.Design
{
    /// <summary>
    /// CA1052: Static holder types should be sealed
    /// </summary>
    [ExportDiagnosticProvider(RuleName, LanguageNames.CSharp)]
    public class CA1052CSharpDiagnosticProvider : CA1052DiagnosticProviderBase
    {
        protected override IEnumerable<SyntaxNode> GetNodes(SyntaxNode root)
        {
            return root.DescendantNodes().OfType<TypeDeclarationSyntax>();
        }
    }
}