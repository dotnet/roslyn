// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertIfToSwitch
{
    partial class CSharpConvertIfToSwitchCodeRefactoringProvider
    {
        public abstract class Pattern
        {
            public sealed class ByValue : Pattern
            {
                private readonly ExpressionSyntax _expression;

                public ByValue(ExpressionSyntax expression)
                {
                    _expression = expression;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => SyntaxFactory.CasePatternSwitchLabel(
                        SyntaxFactory.ConstantPattern(_expression),
                        SyntaxFactory.Token(SyntaxKind.ColonToken));
            }

            public sealed class ByType : Pattern
            {
                private readonly TypeSyntax _type;
                private readonly SyntaxToken _identifier;

                public ByType(TypeSyntax type, SyntaxToken identifier)
                {
                    _type = type;
                    _identifier = identifier;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => CasePatternSwitchLabel(_type, SyntaxFactory.SingleVariableDesignation(_identifier));
            }

            public sealed class Discarded : Pattern
            {
                private readonly TypeSyntax _type;

                public Discarded(TypeSyntax type)
                {
                    _type = type;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => CasePatternSwitchLabel(_type, SyntaxFactory.DiscardDesignation());
            }

            public sealed class Guarded : Pattern
            {
                private readonly Pattern _pattern;
                private readonly ExpressionSyntax _expression;

                public Guarded(Pattern pattern, ExpressionSyntax expression)
                {
                    _pattern = pattern;
                    _expression = expression;
                }

                public override CasePatternSwitchLabelSyntax CreateSwitchLabel()
                    => _pattern.CreateSwitchLabel().WithWhenClause(SyntaxFactory.WhenClause(_expression));
            }

            public abstract CasePatternSwitchLabelSyntax CreateSwitchLabel();

            private static CasePatternSwitchLabelSyntax CasePatternSwitchLabel(TypeSyntax type, VariableDesignationSyntax designation)
                => SyntaxFactory.CasePatternSwitchLabel(
                    SyntaxFactory.DeclarationPattern(type, designation),
                    SyntaxFactory.Token(SyntaxKind.ColonToken));
        }
    }
}
