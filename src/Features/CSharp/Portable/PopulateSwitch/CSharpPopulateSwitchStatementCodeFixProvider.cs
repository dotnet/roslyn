// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
