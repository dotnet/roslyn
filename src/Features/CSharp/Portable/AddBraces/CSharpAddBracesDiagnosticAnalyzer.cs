// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAddBracesDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly LocalizableString s_addBracesLocalizableTitle =
            new LocalizableResourceString(nameof(FeaturesResources.Add_braces), FeaturesResources.ResourceManager,
                typeof(FeaturesResources));

        private static readonly LocalizableString s_addBracesLocalizableMessage =
            new LocalizableResourceString(nameof(WorkspacesResources.Add_braces_to_0_statement), WorkspacesResources.ResourceManager,
                typeof(WorkspacesResources));

        private static readonly LocalizableString s_removeBracesLocalizableTitle =
            new LocalizableResourceString(nameof(FeaturesResources.Remove_braces), FeaturesResources.ResourceManager,
                typeof(FeaturesResources));

        private static readonly LocalizableString s_removeBracesLocalizableMessage =
            new LocalizableResourceString(nameof(WorkspacesResources.Remove_braces_from_0_statement), WorkspacesResources.ResourceManager,
                typeof(WorkspacesResources));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(_noneAddBracesDiagnosticDescriptor,
                                  _infoAddBracesDiagnosticDescriptor,
                                  _warningAddBracesDiagnosticDescriptor,
                                  _errorAddBracesDiagnosticDescriptor,
                                  _noneRemoveBracesDiagnosticDescriptor,
                                  _infoRemoveBracesDiagnosticDescriptor,
                                  _warningRemoveBracesDiagnosticDescriptor,
                                  _errorRemoveBracesDiagnosticDescriptor);

        private readonly DiagnosticDescriptor _noneAddBracesDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _infoAddBracesDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _warningAddBracesDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _errorAddBracesDiagnosticDescriptor;
        private readonly Dictionary<DiagnosticSeverity, DiagnosticDescriptor> _severityToAddBracesDescriptorMap;

        private readonly DiagnosticDescriptor _noneRemoveBracesDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _infoRemoveBracesDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _warningRemoveBracesDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _errorRemoveBracesDiagnosticDescriptor;
        private readonly Dictionary<DiagnosticSeverity, DiagnosticDescriptor> _severityToRemoveBracesDescriptorMap;

        public CSharpAddBracesDiagnosticAnalyzer()
        {
            _noneAddBracesDiagnosticDescriptor = CreateAddBracesDiagnosticDescriptor(DiagnosticSeverity.Hidden);
            _infoAddBracesDiagnosticDescriptor = CreateAddBracesDiagnosticDescriptor(DiagnosticSeverity.Info);
            _warningAddBracesDiagnosticDescriptor = CreateAddBracesDiagnosticDescriptor(DiagnosticSeverity.Warning);
            _errorAddBracesDiagnosticDescriptor = CreateAddBracesDiagnosticDescriptor(DiagnosticSeverity.Error);

            _severityToAddBracesDescriptorMap =
                new Dictionary<DiagnosticSeverity, DiagnosticDescriptor>
                {
                    {DiagnosticSeverity.Hidden, _noneAddBracesDiagnosticDescriptor },
                    {DiagnosticSeverity.Info, _infoAddBracesDiagnosticDescriptor },
                    {DiagnosticSeverity.Warning, _warningAddBracesDiagnosticDescriptor },
                    {DiagnosticSeverity.Error, _errorAddBracesDiagnosticDescriptor },
                };

            _noneRemoveBracesDiagnosticDescriptor = CreateRemoveBracesDiagnosticDescriptor(DiagnosticSeverity.Hidden);
            _infoRemoveBracesDiagnosticDescriptor = CreateRemoveBracesDiagnosticDescriptor(DiagnosticSeverity.Info);
            _warningRemoveBracesDiagnosticDescriptor = CreateRemoveBracesDiagnosticDescriptor(DiagnosticSeverity.Warning);
            _errorRemoveBracesDiagnosticDescriptor = CreateRemoveBracesDiagnosticDescriptor(DiagnosticSeverity.Error);

            _severityToRemoveBracesDescriptorMap =
                new Dictionary<DiagnosticSeverity, DiagnosticDescriptor>
                {
                    {DiagnosticSeverity.Hidden, _noneRemoveBracesDiagnosticDescriptor },
                    {DiagnosticSeverity.Info, _infoRemoveBracesDiagnosticDescriptor },
                    {DiagnosticSeverity.Warning, _warningRemoveBracesDiagnosticDescriptor },
                    {DiagnosticSeverity.Error, _errorRemoveBracesDiagnosticDescriptor },
                };
        }

        private DiagnosticDescriptor CreateAddBracesDiagnosticDescriptor(DiagnosticSeverity severity) =>
            new DiagnosticDescriptor(
                IDEDiagnosticIds.AddBracesDiagnosticId,
                s_addBracesLocalizableTitle,
                s_addBracesLocalizableMessage,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true);


        private DiagnosticDescriptor CreateRemoveBracesDiagnosticDescriptor(DiagnosticSeverity severity) =>
            new DiagnosticDescriptor(
                IDEDiagnosticIds.RemoveBracesDiagnosticId,
                s_removeBracesLocalizableTitle,
                s_removeBracesLocalizableMessage,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true);

        public bool RunInProcess => false;

        public override void Initialize(AnalysisContext context)
        {
            var syntaxKindsOfInterest =
            ImmutableArray.Create(SyntaxKind.IfStatement,
                SyntaxKind.ElseClause,
                SyntaxKind.ForStatement,
                SyntaxKind.ForEachStatement,
                SyntaxKind.ForEachComponentStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.UsingStatement,
                SyntaxKind.LockStatement,
                SyntaxKind.FixedStatement);

            context.RegisterSyntaxNodeAction(AnalyzeNode, syntaxKindsOfInterest);
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var optionSet = context.Options.GetOptionSet();
            var useBracesOption = optionSet.GetOption(CSharpCodeStyleOptions.UseBracesWherePossible);
            var severity = useBracesOption.Notification.Value;

            if (useBracesOption.Value)
            {
                var diagnostic = GetAddBracesDiagnostic(context.Node, severity);
                if (diagnostic != null)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else
            {
                var diagnostic = GetRemoveBracesDiagnostic(context.Node, severity);
                if (diagnostic != null)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private Diagnostic GetAddBracesDiagnostic(SyntaxNode node, DiagnosticSeverity severity)
        {
            if (node.IsKind(SyntaxKind.IfStatement))
            {
                var ifStatement = (IfStatementSyntax)node;
                if (AnalyzeIfStatement(ifStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        ifStatement.IfKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.IfKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.ElseClause))
            {
                var elseClause = (ElseClauseSyntax)node;
                if (AnalyzeElseClause(elseClause))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        elseClause.ElseKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ElseKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.ForStatement))
            {
                var forStatement = (ForStatementSyntax)node;
                if (AnalyzeForStatement(forStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        forStatement.ForKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ForKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.ForEachStatement) || node.IsKind(SyntaxKind.ForEachComponentStatement))
            {
                var forEachStatement = (CommonForEachStatementSyntax)node;
                if (AnalyzeForEachStatement(forEachStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        forEachStatement.ForEachKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ForEachKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.WhileStatement))
            {
                var whileStatement = (WhileStatementSyntax)node;
                if (AnalyzeWhileStatement(whileStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        whileStatement.WhileKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.WhileKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.DoStatement))
            {
                var doStatement = (DoStatementSyntax)node;
                if (AnalyzeDoStatement(doStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        doStatement.DoKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.DoKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.UsingStatement))
            {
                var usingStatement = (UsingStatementSyntax)node;
                if (AnalyzeUsingStatement(usingStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        usingStatement.UsingKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.UsingKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.LockStatement))
            {
                var lockStatement = (LockStatementSyntax)node;
                if (AnalyzeLockStatement(lockStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        lockStatement.LockKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.LockKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.FixedStatement))
            {
                var fixedStatement = (FixedStatementSyntax)node;
                if (AnalyzeFixedStatement(fixedStatement))
                {
                    return Diagnostic.Create(_severityToAddBracesDescriptorMap[severity],
                        fixedStatement.FixedKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.LockKeyword));
                }
            }

            return null;
        }

        private Diagnostic GetRemoveBracesDiagnostic(SyntaxNode node, DiagnosticSeverity severity)
        {
            if (node.IsKind(SyntaxKind.IfStatement))
            {
                var ifStatement = (IfStatementSyntax)node;
                if (AnalyzeBlockedIfStatement(ifStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        ifStatement.IfKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.IfKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.ElseClause))
            {
                var elseClause = (ElseClauseSyntax)node;
                if (AnalyzeBlockedElseClause(elseClause))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        elseClause.ElseKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ElseKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.ForStatement))
            {
                var forStatement = (ForStatementSyntax)node;
                if (AnalyzeBlockedForStatement(forStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        forStatement.ForKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ForKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.ForEachStatement) || node.IsKind(SyntaxKind.ForEachComponentStatement))
            {
                var forEachStatement = (CommonForEachStatementSyntax)node;
                if (AnalyzeBlockedForEachStatement(forEachStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        forEachStatement.ForEachKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ForEachKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.WhileStatement))
            {
                var whileStatement = (WhileStatementSyntax)node;
                if (AnalyzeBlockedWhileStatement(whileStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        whileStatement.WhileKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.WhileKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.DoStatement))
            {
                var doStatement = (DoStatementSyntax)node;
                if (AnalyzeBlockedDoStatement(doStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        doStatement.DoKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.DoKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.UsingStatement))
            {
                var usingStatement = (UsingStatementSyntax)node;
                if (AnalyzeBlockedUsingStatement(usingStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        usingStatement.UsingKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.UsingKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.LockStatement))
            {
                var lockStatement = (LockStatementSyntax)node;
                if (AnalyzeBlockedLockStatement(lockStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        lockStatement.LockKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.LockKeyword));
                }
            }

            if (node.IsKind(SyntaxKind.FixedStatement))
            {
                var fixedStatement = (FixedStatementSyntax)node;
                if (AnalyzeBlockedFixedStatement(fixedStatement))
                {
                    return Diagnostic.Create(_severityToRemoveBracesDescriptorMap[severity],
                        fixedStatement.FixedKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.LockKeyword));
                }
            }

            return null;
        }

        private bool AnalyzeIfStatement(IfStatementSyntax ifStatement) =>
            !ifStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeElseClause(ElseClauseSyntax elseClause) =>
            !elseClause.Statement.IsKind(SyntaxKind.Block) &&
            !elseClause.Statement.IsKind(SyntaxKind.IfStatement);

        private bool AnalyzeForStatement(ForStatementSyntax forStatement) =>
            !forStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeForEachStatement(CommonForEachStatementSyntax forEachStatement) =>
            !forEachStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeWhileStatement(WhileStatementSyntax whileStatement) =>
            !whileStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeDoStatement(DoStatementSyntax doStatement) =>
            !doStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeUsingStatement(UsingStatementSyntax usingStatement) =>
            !usingStatement.Statement.IsKind(SyntaxKind.Block) &&
            !usingStatement.Statement.IsKind(SyntaxKind.UsingStatement);

        private bool AnalyzeLockStatement(LockStatementSyntax lockStatement) =>
            !lockStatement.Statement.IsKind(SyntaxKind.Block) &&
            !lockStatement.Statement.IsKind(SyntaxKind.LockStatement);

        private bool AnalyzeFixedStatement(FixedStatementSyntax fixedStatement) =>
            !fixedStatement.Statement.IsKind(SyntaxKind.Block) &&
            !fixedStatement.Statement.IsKind(SyntaxKind.FixedStatement);

        private bool AnalyzeBlockedIfStatement(IfStatementSyntax ifStatement) =>
            ifStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)ifStatement.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedElseClause(ElseClauseSyntax elseClause) =>
            elseClause.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)elseClause.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedForStatement(ForStatementSyntax forStatement) =>
            forStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)forStatement.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedForEachStatement(CommonForEachStatementSyntax forEachStatement) =>
            forEachStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)forEachStatement.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedWhileStatement(WhileStatementSyntax whileStatement) =>
            whileStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)whileStatement.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedDoStatement(DoStatementSyntax doStatement) =>
            doStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)doStatement.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedUsingStatement(UsingStatementSyntax usingStatement) =>
            usingStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)usingStatement.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedLockStatement(LockStatementSyntax lockStatement) =>
            lockStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)lockStatement.Statement).Statements.Count == 1;

        private bool AnalyzeBlockedFixedStatement(FixedStatementSyntax fixedStatement) =>
            fixedStatement.Statement.IsKind(SyntaxKind.Block) &&
            ((BlockSyntax)fixedStatement.Statement).Statements.Count == 1;
    }
}