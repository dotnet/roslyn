// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch
{
    partial class CSharpConvertIfToSwitchCodeRefactoringProvider
    {
        private abstract class Pattern : IPattern<CasePatternSwitchLabelSyntax>
        {
            public abstract CasePatternSwitchLabelSyntax CreateSwitchLabel();

            private static CasePatternSwitchLabelSyntax CasePatternSwitchLabel(TypeSyntax type, VariableDesignationSyntax designation)
                => SyntaxFactory.CasePatternSwitchLabel(
                    SyntaxFactory.DeclarationPattern(type, designation),
                    SyntaxFactory.Token(SyntaxKind.ColonToken));

            internal sealed class ByValue : Pattern
            {
                private readonly ExpressionSyntax _expression;

                internal ByValue(ExpressionSyntax expression)
                {
                    _expression = expression;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => SyntaxFactory.CasePatternSwitchLabel(
                        SyntaxFactory.ConstantPattern(_expression),
                        SyntaxFactory.Token(SyntaxKind.ColonToken));
            }

            internal sealed class ByType : Pattern
            {
                private readonly TypeSyntax _type;
                private readonly SyntaxToken _identifier;

                internal ByType(TypeSyntax type, SyntaxToken identifier)
                {
                    _type = type;
                    _identifier = identifier;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => CasePatternSwitchLabel(_type, SyntaxFactory.SingleVariableDesignation(_identifier));
            }

            internal sealed class Discarded : Pattern
            {
                private readonly TypeSyntax _type;

                internal Discarded(TypeSyntax type)
                {
                    _type = type;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => CasePatternSwitchLabel(_type, SyntaxFactory.DiscardDesignation());
            }

            internal sealed class Guarded : Pattern
            {
                private readonly IPattern<CasePatternSwitchLabelSyntax> _pattern;
                private readonly ExpressionSyntax _expression;

                internal Guarded(IPattern<CasePatternSwitchLabelSyntax> pattern, ExpressionSyntax expression)
                {
                    _pattern = pattern;
                    _expression = expression;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => _pattern.CreateSwitchLabel().WithWhenClause(SyntaxFactory.WhenClause(_expression));
            }
        }
    }
}
