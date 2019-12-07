// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PopulateSwitch;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class PopulateSwitchExpressionCodeFixProvider
        : AbstractPopulateSwitchCodeFixProvider<ISwitchExpressionOperation, SwitchExpressionSyntax>
    {
        [ImportingConstructor]
        public PopulateSwitchExpressionCodeFixProvider()
            : base(IDEDiagnosticIds.PopulateSwitchExpressionDiagnosticId)
        {
        }

        protected override void FixOneDiagnostic(
            Document document, SyntaxEditor editor, SemanticModel model,
            bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
            bool hasMissingCases, bool hasMissingDefaultCase,
            SwitchExpressionSyntax switchNode, ISwitchExpressionOperation switchExpression)
        {
            var generator = editor.Generator;
            var compilation = model.Compilation;

            var enumType = switchExpression.Value.Type;
            var exception = (ExpressionSyntax)generator.CreateThrowNotImplementedExpression(compilation);
            var newArms = new List<SwitchExpressionArmSyntax>();

            if (hasMissingCases && addCases)
            {
                var missingEnumMembers = PopulateSwitchHelpers.GetMissingEnumMembers(switchExpression);
                var missingArms =
                    from e in missingEnumMembers
                    let armPattern = generator.MemberAccessExpression(generator.TypeExpression(enumType), e.Name).WithAdditionalAnnotations(Simplifier.Annotation)
                    select SwitchExpressionArm(ConstantPattern((ExpressionSyntax)armPattern), exception);

                newArms.AddRange(missingArms);
            }

            if (hasMissingDefaultCase && addDefaultCase)
            {
                // Always add the default clause at the end.
                newArms.Add(SwitchExpressionArm(DiscardPattern(), exception));
            }

            var insertLocation = InsertPosition(switchExpression);

            var newSwitchNode = switchNode.WithArms(switchNode.Arms.InsertRange(insertLocation, newArms))
                .WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(switchNode, newSwitchNode);
        }

        private int InsertPosition(ISwitchExpressionOperation switchExpression)
        {
            // If the last section has a default label, then we want to be above that.
            // Otherwise, we just get inserted at the end.

            var arms = switchExpression.Arms;
            for (var i = 0; i < arms.Length; i++)
            {
                var arm = arms[i];
                if (PopulateSwitchHelpers.IsDefault(arm))
                {
                    return i - 1;
                }
            }

            return arms.Length;
        }
    }
}
