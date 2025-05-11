// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp,
    Name = PredefinedCodeFixProviderNames.PopulateSwitchExpression), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpPopulateSwitchExpressionCodeFixProvider()
    : AbstractPopulateSwitchExpressionCodeFixProvider<
        ExpressionSyntax,
        SwitchExpressionSyntax,
        SwitchExpressionArmSyntax,
        MemberAccessExpressionSyntax>
{
    protected override SwitchExpressionArmSyntax CreateDefaultSwitchArm(SyntaxGenerator generator, Compilation compilation)
        => SwitchExpressionArm(DiscardPattern(), Exception(generator, compilation));

    protected override SwitchExpressionArmSyntax CreateSwitchArm(SyntaxGenerator generator, Compilation compilation, MemberAccessExpressionSyntax caseLabel)
        => SwitchExpressionArm(ConstantPattern(caseLabel), Exception(generator, compilation));

    protected override SwitchExpressionArmSyntax CreateNullSwitchArm(SyntaxGenerator generator, Compilation compilation)
        => SwitchExpressionArm(ConstantPattern((LiteralExpressionSyntax)generator.NullLiteralExpression()), Exception(generator, compilation));

    protected override SwitchExpressionSyntax InsertSwitchArms(SyntaxGenerator generator, SwitchExpressionSyntax switchNode, int insertLocation, List<SwitchExpressionArmSyntax> newArms)
    {
        // If the existing switch expression ends with a comma, then ensure that we preserve
        // that.  Also do this for an empty switch statement.
        if (switchNode.Arms.Count == 0 ||
            !switchNode.Arms.GetWithSeparators().LastOrDefault().IsNode)
        {
            return switchNode.WithArms(switchNode.Arms.InsertRangeWithTrailingSeparator(
                insertLocation, newArms, SyntaxKind.CommaToken));
        }

        return switchNode.WithArms(switchNode.Arms.InsertRange(insertLocation, newArms));
    }
}
