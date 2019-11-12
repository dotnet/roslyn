// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using FormattingRangeHelper = Microsoft.CodeAnalysis.CSharp.Utilities.FormattingRangeHelper;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAddBracesDiagnosticAnalyzer :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpAddBracesDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddBracesDiagnosticId,
                   CSharpCodeStyleOptions.PreferBraces,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(FeaturesResources.Add_braces), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(WorkspacesResources.Add_braces_to_0_statement), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)))
        {
        }

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

            var optionSet = context.Options.GetAnalyzerOptionSetAsync(statement.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
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
                && !RequiresBracesToMatchContext(statement))
            {
                return;
            }

            if (ContainsInterleavedDirective(statement, embeddedStatement, cancellationToken))
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

        /// <summary>
        /// Check if there are interleaved directives on the statement.
        /// Handles special case with if/else.
        /// </summary>
        private static bool ContainsInterleavedDirective(SyntaxNode statement, StatementSyntax embeddedStatement, CancellationToken cancellationToken)
        {
            if (statement.Kind() == SyntaxKind.IfStatement)
            {
                var ifStatementNode = (IfStatementSyntax)statement;
                var elseNode = ifStatementNode.Else;
                if (elseNode != null && !embeddedStatement.IsMissing)
                {
                    // For IF/ELSE statements, only the IF part should be checked for interleaved directives when the diagnostic is triggered on the IF.
                    // A separate diagnostic will be triggered to handle the ELSE part.
                    var ifStatementSpanWithoutElse = TextSpan.FromBounds(statement.Span.Start, embeddedStatement.Span.End);
                    return statement.ContainsInterleavedDirective(ifStatementSpanWithoutElse, cancellationToken);
                }
            }

            return statement.ContainsInterleavedDirective(cancellationToken);
        }

        /// <summary>
        /// <para>In general, statements are considered multiline if any of the following span more than one line:</para>
        /// <list type="bullet">
        /// <item><description>The part of the statement preceding the embedded statement</description></item>
        /// <item><description>The embedded statement itself</description></item>
        /// <item><description>The part of the statement following the embedded statement, for example the
        /// <c>while (...);</c> portion of a <c>do ... while (...);</c> statement</description></item>
        /// </list>
        /// <para>The third condition is not checked for <c>else</c> clauses because they are only considered multiline
        /// when their embedded statement is multiline.</para>
        /// </summary>
        private static bool IsConsideredMultiLine(SyntaxNode statement, SyntaxNode embeddedStatement)
        {
            // Early return if syntax errors prevent analysis
            if (embeddedStatement.IsMissing)
            {
                // The embedded statement was added by the compiler during recovery from a syntax error
                return false;
            }

            // Early return if the entire statement fits on one line
            if (FormattingRangeHelper.AreTwoTokensOnSameLine(statement.GetFirstToken(), statement.GetLastToken()))
            {
                // The entire statement fits on one line. Examples:
                //
                //   if (something) return;
                //
                //   while (true) something();
                return false;
            }

            // Check the part of the statement preceding the embedded statement (bullet 1)
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

            // Check the embedded statement itself (bullet 2)
            if (!FormattingRangeHelper.AreTwoTokensOnSameLine(embeddedStatement.GetFirstToken(), embeddedStatement.GetLastToken()))
            {
                // The embedded statement does not fit on one line. Examples:
                //
                //   if (something)
                //     obj.Method(   // <-- This embedded statement spans two lines.
                //       arg);
                return true;
            }

            // Check the part of the statement following the embedded statement, but only if it exists and is not an
            // 'else' clause (bullet 3)
            if (statement.GetLastToken() != embeddedStatement.GetLastToken())
            {
                if (statement is IfStatementSyntax ifStatement && ifStatement.Statement == embeddedStatement)
                {
                    // The embedded statement is followed by an 'else' clause, which may span multiple lines without
                    // triggering a braces requirement, such as this:
                    //
                    //   if (true)
                    //     return;
                    //   else          // <-- this else clause is two lines, but is not considered a multiline context
                    //     return;
                    //
                    // ---
                    // INTENTIONAL FALLTHROUGH
                }
                else
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
            }

            return false;
        }

        /// <summary>
        /// Determines whether <paramref name="statement"/> should use braces under a
        /// <see cref="PreferBracesPreference.WhenMultiline"/> preference due to the presence of braces on one or more
        /// sibling statements (the "context").
        /// </summary>
        private static bool RequiresBracesToMatchContext(SyntaxNode statement)
        {
            if (!statement.IsKind(SyntaxKind.IfStatement, SyntaxKind.ElseClause))
            {
                // 'if' statements are the only statements that can have multiple embedded statements which are
                // considered relative to each other.
                return false;
            }

            var outermostIfStatement = GetOutermostIfStatementOfSequence(statement);
            if (AnyPartOfIfSequenceUsesBraces(outermostIfStatement))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the top-most <see cref="IfStatementSyntax"/> for which <paramref name="ifStatementOrElseClause"/> is
        /// part of the <c>if</c>/<c>else if</c>/<c>else</c> sequence.
        /// </summary>
        /// <remarks>
        /// <para>For the purpose of brace usage analysis, the embedded statements of an <c>if</c>/<c>else if</c>/<c>else</c>
        /// sequence are considered sibling statements, even though they don't appear as immediate siblings in the
        /// syntax tree. This method walks up the syntax tree to find the <c>if</c> statement that starts the
        /// sequence.</para>
        /// </remarks>
        private static IfStatementSyntax GetOutermostIfStatementOfSequence(SyntaxNode ifStatementOrElseClause)
        {
            IfStatementSyntax result;
            if (ifStatementOrElseClause.IsKind(SyntaxKind.ElseClause))
            {
                result = (IfStatementSyntax)ifStatementOrElseClause.Parent;
            }
            else
            {
                Debug.Assert(ifStatementOrElseClause.IsKind(SyntaxKind.IfStatement));
                result = (IfStatementSyntax)ifStatementOrElseClause;
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

        /// <summary>
        /// Determines if any embedded statement of an <c>if</c>/<c>else if</c>/<c>else</c> sequence uses braces. Only
        /// the embedded statements falling <em>immediately</em> under one of these nodes are checked.
        /// </summary>
        private static bool AnyPartOfIfSequenceUsesBraces(IfStatementSyntax statement)
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
