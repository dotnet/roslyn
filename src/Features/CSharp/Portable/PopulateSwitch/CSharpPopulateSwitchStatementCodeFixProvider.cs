// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpPopulateSwitchStatementCodeFixProvider : AbstractPopulateSwitchStatementCodeFixProvider<
        SwitchStatementSyntax, SwitchSectionSyntax, MemberAccessExpressionSyntax>
    {
    }
}
