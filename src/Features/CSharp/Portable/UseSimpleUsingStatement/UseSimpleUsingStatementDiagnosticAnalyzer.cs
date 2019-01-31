// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseSimpleUsingStatementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public UseSimpleUsingStatementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_simple_using_statement), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.using_statement_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.UsingStatement);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax)context.Node;

            var syntaxTree = context.Node.SyntaxTree;
            var options = (CSharpParseOptions)syntaxTree.Options;
            if (options.LanguageVersion < LanguageVersion.CSharp8)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction);
            if (!option.Value)
            {
                return;
            }

            if (CanConvertUsingStatement(usingStatement))
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    usingStatement.UsingKeyword.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: ImmutableArray.Create(usingStatement.GetLocation()),
                    properties: null));
            }
        }

        public static bool CanConvertUsingStatement(UsingStatementSyntax usingStatement)
        {
            if (usingStatement.Declaration == null)
            {
                return false;
            }

            var parent = usingStatement.Parent;
            if (!(parent is BlockSyntax || parent is UsingStatementSyntax))
            {
                return false;
            }

            // Has to be one of the following forms:
            // 1. Using statement is the last statement in the parent.
            // 2. Using statement is not the last statement in parent, but is followed by 
            //    something that is unaffected by simplifying the using statement.  i.e.
            //    `return`/`break`/`continue`.  *Note*.  `return expr` would *not* be ok.
            //    In that case, `expr` would now be evaluated *before* the using disposed
            //    the resource, instead of afterwards.  Effectly, the statement following
            //    cannot actually execute any code that might depend on the .Dispose method
            //    being called or not.
            var statements = GetStatements(parent);

            var index = statements.IndexOf(usingStatement);
            if (index == statements.Count - 1)
            {
                // very last statement in the block.  Can be converted.
                return true;
            }

            // Not the last statement, get the next statement and examine that.
            var nextStatement = statements[index + 1];
            if (nextStatement is BreakStatementSyntax ||
                nextStatement is ContinueStatementSyntax)
            {
                // using statemnet followed by break/continue.  Can conver this as executing 
                // the break/continue will cause the code to exit the using scope, causing 
                // Dispose to be called at the same place as before.
                return true;
            }

            if (nextStatement is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression == null)
            {
                // using statemnet followed by `return`.  Can conver this as executing 
                // the `return` will cause the code to exit the using scope, causing 
                // Dispose to be called at the same place as before.
                //
                // Note: the expr has to be null.  If it was non-null, then the expr would
                // now execute before hte using called 'Dispose' instead of after, potentially
                // changing semantics.
                return true;
            }

            // Add any additional cases here in the future.
            return false;
        }

        private static SyntaxList<StatementSyntax> GetStatements(SyntaxNode parent)
        {
            switch (parent)
            {
                case BlockSyntax block: return block.Statements;
                case UsingStatementSyntax usingStatement: return new SyntaxList<StatementSyntax>(usingStatement.Statement);
                default: throw ExceptionUtilities.UnexpectedValue(parent);
            }
        }
    }
}
