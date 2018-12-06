// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using FormattingRangeHelper = Microsoft.CodeAnalysis.CSharp.Utilities.FormattingRangeHelper;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAddBracesDiagnosticAnalyzer :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpAddBracesDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddBracesDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Add_braces), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(WorkspacesResources.Add_braces_to_0_statement), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)))
        {
        }

        public override bool OpenFileOnly(Workspace workspace) => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode,
                SyntaxKind.IfStatement,
                SyntaxKind.ElseClause,
                SyntaxKind.ForStatement,
                SyntaxKind.ForEachStatement,
                SyntaxKind.ForEachVariableStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.UsingStatement,
                SyntaxKind.LockStatement,
                SyntaxKind.FixedStatement);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var statement = context.Node;
            var cancellationToken = context.CancellationToken;

            var optionSet = context.Options.GetDocumentOptionSetAsync(statement.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferBraces);
            if (option.Value == PreferBracesPreference.None)
            {
                return;
            }

            var embeddedStatement = statement.GetEmbeddedStatement();
            switch (embeddedStatement.Kind())
            {
                case SyntaxKind.Block:
                    // The embedded statement already has braces, which is always allowed.
                    return;

                case SyntaxKind.IfStatement when statement.Kind() == SyntaxKind.ElseClause:
                    // Constructs like the following are always allowed:
                    //
                    //   if (something)
                    //   {
                    //   }
                    //   else if (somethingElse) // <-- 'if' nested in an 'else' clause
                    //   {
                    //   }
                    return;

                case SyntaxKind.LockStatement:
                case SyntaxKind.UsingStatement:
                case SyntaxKind.FixedStatement:
                    // If we have something like this:
                    //     
                    //    using (...)
                    //    using (...)
                    //    {
                    //    }
                    //
                    // The first statement needs no block as it formatted with the same indentation.
                    if (statement.Kind() == embeddedStatement.Kind())
                    {
                        return;
                    }

                    break;
            }

            if (option.Value == PreferBracesPreference.WhenMultiline
                && !IsConsideredMultiLine(statement, embeddedStatement)
                && !RequiresBracesToMatchContext(statement, embeddedStatement))
            {
                return;
            }

            var firstToken = statement.GetFirstToken();
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                firstToken.GetLocation(),
                option.Notification.Severity,
                additionalLocations: null,
                properties: null,
                SyntaxFacts.GetText(firstToken.Kind())));
        }

        private static bool IsConsideredMultiLine(SyntaxNode statement, SyntaxNode embeddedStatement)
        {
            if (FormattingRangeHelper.AreTwoTokensOnSameLine(statement.GetFirstToken(), statement.GetLastToken()))
            {
                // The entire statement fits on one line. Examples:
                //
                //   if (something) return;
                //
                //   while (true) something();
                return false;
            }

            if (!FormattingRangeHelper.AreTwoTokensOnSameLine(embeddedStatement.GetFirstToken(), embeddedStatement.GetLastToken()))
            {
                // The embedded statement does not fit on one line. Examples:
                //
                //   if (something)
                //     obj.Method(   // <-- This embedded statement spans two lines.
                //       arg);
                return true;
            }

            var lastTokenBeforeEmbeddedStatement = embeddedStatement.GetFirstToken().GetPreviousToken();
            if (!FormattingRangeHelper.AreTwoTokensOnSameLine(statement.GetFirstToken(), lastTokenBeforeEmbeddedStatement))
            {
                // The part of the statement preceding the embedded statement does not fit on one line. Examples:
                //
                //   for (int i = 0; // <-- The initializer/condition/increment are on separate lines
                //        i < 10;
                //        i++)
                //     SomeMethod();
                return true;
            }

            if (statement.GetLastToken() != embeddedStatement.GetLastToken())
            {
                var firstTokenAfterEmbeddedStatement = embeddedStatement.GetLastToken().GetNextToken();
                if (!FormattingRangeHelper.AreTwoTokensOnSameLine(firstTokenAfterEmbeddedStatement, statement.GetLastToken()))
                {
                    // The part of the statement following the embedded statement does not fit on one line. Examples:
                    //
                    //   do
                    //     SomeMethod();
                    //   while (x < 0 ||    // <-- This condition is split across multiple lines.
                    //          x > 10);
                    return true;
                }
            }

            return false;
        }

        private static bool RequiresBracesToMatchContext(SyntaxNode statement, StatementSyntax embeddedStatement)
        {
            if (!statement.IsKind(SyntaxKind.IfStatement, SyntaxKind.ElseClause))
            {
                return false;
            }

            var topLevelContext = GetTopLevelContext(statement);
            if (AnyPartUsesBraces(topLevelContext))
            {
                return true;
            }

            return false;
        }

        private static IfStatementSyntax GetTopLevelContext(SyntaxNode statement)
        {
            IfStatementSyntax result;
            if (statement.IsKind(SyntaxKind.ElseClause))
            {
                result = (IfStatementSyntax)statement.Parent;
            }
            else
            {
                Debug.Assert(statement.IsKind(SyntaxKind.IfStatement));
                result = (IfStatementSyntax)statement;
            }

            while (result != null)
            {
                if (!result.IsParentKind(SyntaxKind.ElseClause))
                {
                    break;
                }

                result = (IfStatementSyntax)result.Parent.Parent;
            }

            return result;
        }

        private static bool AnyPartUsesBraces(IfStatementSyntax statement)
        {
            // Iterative instead of recursive to avoid stack depth problems
            while (statement != null)
            {
                if (statement.Statement.IsKind(SyntaxKind.Block))
                {
                    return true;
                }

                var elseStatement = statement.Else?.Statement;
                if (elseStatement.IsKind(SyntaxKind.Block))
                {
                    return true;
                }

                statement = elseStatement as IfStatementSyntax;
            }

            return false;
        }
    }
}
