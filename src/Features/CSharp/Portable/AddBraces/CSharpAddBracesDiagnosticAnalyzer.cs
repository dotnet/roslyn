// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAddBracesDiagnosticAnalyzer :
        AbstractCodeStyleDiagnosticAnalyzer
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
            if (!option.Value)
            {
                return;
            }

            var embeddedStatement = statement.GetEmbeddedStatement();
            switch (embeddedStatement.Kind())
            {
                case SyntaxKind.Block:
                case SyntaxKind.IfStatement when statement.Kind() == SyntaxKind.ElseClause:
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

            var firstToken = statement.GetFirstToken();
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                firstToken.GetLocation(),
                option.Notification.Severity,
                additionalLocations: null,
                properties: null,
                SyntaxFacts.GetText(firstToken.Kind())));
        }
    }
}
