// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpPopulateSwitchExpressionCodeFixProvider
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
}
