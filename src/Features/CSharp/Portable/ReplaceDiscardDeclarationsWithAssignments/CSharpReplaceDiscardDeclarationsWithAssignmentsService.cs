// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.ReplaceDiscardDeclarationsWithAssignments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceDiscardDeclarationsWithAssignments
{
    [ExportLanguageService(typeof(IReplaceDiscardDeclarationsWithAssignmentsService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpReplaceDiscardDeclarationsWithAssignmentsService : IReplaceDiscardDeclarationsWithAssignmentsService
    {
        [ImportingConstructor]
        public CSharpReplaceDiscardDeclarationsWithAssignmentsService()
        {
        }

        public Task<SyntaxNode> ReplaceAsync(SyntaxNode memberDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var editor = new SyntaxEditor(memberDeclaration, CSharpSyntaxGenerator.Instance);
            foreach (var child in memberDeclaration.DescendantNodes())
            {
                switch (child)
                {
                    case LocalDeclarationStatementSyntax localDeclarationStatement:
                        if (localDeclarationStatement.Declaration.Variables.Any(IsDiscardDeclaration))
                        {
                            RemoveDiscardHelper.ProcessDeclarationStatement(localDeclarationStatement, editor);
                        }

                        break;

                    case CatchDeclarationSyntax catchDeclaration:
                        if (IsDiscardDeclaration(catchDeclaration))
                        {
                            // "catch (Exception _)" => "catch (Exception)"
                            editor.ReplaceNode(catchDeclaration, catchDeclaration.WithIdentifier(default));
                        }

                        break;

                    case DeclarationExpressionSyntax declarationExpression:
                        if (declarationExpression.Designation is DiscardDesignationSyntax discardSyntax)
                        {
                            // "M(out var _)" => "M(out _)"
                            // "M(out int _)" => "M(out _)"

                            var discardToken = SyntaxFactory.Identifier(
                                leading: declarationExpression.GetLeadingTrivia(),
                                contextualKind: SyntaxKind.UnderscoreToken,
                                text: discardSyntax.UnderscoreToken.Text,
                                valueText: discardSyntax.UnderscoreToken.ValueText,
                                trailing: declarationExpression.GetTrailingTrivia());
                            var replacementNode = SyntaxFactory.IdentifierName(discardToken);

                            // Removing explicit type is possible only if there are no overloads of the method with same parameter.
                            // For example, if method "M" had overloads with signature "void M(int x)" and "void M(char x)",
                            // then the replacement "M(out int _)" => "M(out _)" will cause overload resolution error.
                            // Bail out if replacement changes semantics.
                            var speculationAnalyzer = new SpeculationAnalyzer(declarationExpression,
                                replacementNode, semanticModel, cancellationToken);
                            if (!speculationAnalyzer.ReplacementChangesSemantics())
                            {
                                editor.ReplaceNode(declarationExpression, replacementNode);
                            }
                        }

                        break;

                    case DeclarationPatternSyntax declarationPattern:
                        if (declarationPattern is
                        {
                            Designation: DiscardDesignationSyntax discardDesignationSyntax,
                            Parent: IsPatternExpressionSyntax isPatternExpression
                        })
                        {
                            // "x is int _" => "x is int"
                            var replacementNode = SyntaxFactory.BinaryExpression(
                                kind: SyntaxKind.IsExpression,
                                left: isPatternExpression.Expression,
                                operatorToken: isPatternExpression.IsKeyword,
                                right: declarationPattern.Type.WithTrailingTrivia(declarationPattern.GetTrailingTrivia()));
                            editor.ReplaceNode(isPatternExpression, replacementNode);
                        }

                        break;
                }
            }

            return Task.FromResult(editor.GetChangedRoot());
        }

        private static bool IsDiscardDeclaration(VariableDeclaratorSyntax variable)
            => variable.Identifier.Text == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName;
        private static bool IsDiscardDeclaration(CatchDeclarationSyntax catchDeclaration)
            => catchDeclaration.Identifier.Text == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName;

        private sealed class RemoveDiscardHelper : IDisposable
        {
            private readonly LocalDeclarationStatementSyntax _localDeclarationStatement;
            private readonly SyntaxEditor _editor;
            private readonly ArrayBuilder<StatementSyntax> _statementsBuilder;
            private SeparatedSyntaxList<VariableDeclaratorSyntax> _currentNonDiscardVariables;

            private RemoveDiscardHelper(LocalDeclarationStatementSyntax localDeclarationStatement, SyntaxEditor editor)
            {
                _localDeclarationStatement = localDeclarationStatement;
                _editor = editor;

                _statementsBuilder = ArrayBuilder<StatementSyntax>.GetInstance();
                _currentNonDiscardVariables = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            }

            public static void ProcessDeclarationStatement(
                LocalDeclarationStatementSyntax localDeclarationStatement,
                SyntaxEditor editor)
            {
                using var helper = new RemoveDiscardHelper(localDeclarationStatement, editor);
                helper.ProcessDeclarationStatement();
            }

            public void Dispose() => _statementsBuilder.Free();

            private void ProcessDeclarationStatement()
            {
                // We will replace all discard variable declarations in this method with discard assignments,
                // For example,
                //  1. "int _ = M();" is replaced with "_ = M();"
                //  2. "int x = 1, _ = M(), y = 2;" is replaced with following statements:
                //          int x = 1;
                //          _ = M();
                //          int y = 2;
                // This is done to prevent compiler errors where the existing method has a discard
                // variable declaration at a line following the one we added a discard assignment in our fix.

                // Process all the declared variables in the given local declaration statement,
                // tracking the currently encountered non-discard variables. 
                foreach (var variable in _localDeclarationStatement.Declaration.Variables)
                {
                    if (!IsDiscardDeclaration(variable))
                    {
                        // Add to the list of currently encountered non-discard variables
                        _currentNonDiscardVariables = _currentNonDiscardVariables.Add(variable);
                    }
                    else
                    {
                        // Process currently encountered non-discard variables to generate
                        // a local declaration statement with these variables.
                        GenerateDeclarationStatementForCurrentNonDiscardVariables();

                        // Process the discard variable declaration to replace it
                        // with an assignment to discard.
                        GenerateAssignmentForDiscardVariable(variable);
                    }
                }

                // Process all the remaining variable declarators to generate
                // a local declaration statement with these variables.
                GenerateDeclarationStatementForCurrentNonDiscardVariables();

                // Now replace the original local declaration statement with
                // the replacement statement list tracked in _statementsBuilder.

                if (_statementsBuilder.Count == 0)
                {
                    // Nothing to replace.
                    return;
                }

                // Move the leading trivia from original local declaration statement
                // to the first statement of the replacement statement list.
                var leadingTrivia = _localDeclarationStatement.Declaration.Type.GetLeadingTrivia()
                    .Concat(_localDeclarationStatement.Declaration.Type.GetTrailingTrivia());
                _statementsBuilder[0] = _statementsBuilder[0].WithLeadingTrivia(leadingTrivia);

                // Move the trailing trivia from original local declaration statement
                // to the last statement of the replacement statement list.
                var last = _statementsBuilder.Count - 1;
                var trailingTrivia = _localDeclarationStatement.SemicolonToken.GetAllTrivia();
                _statementsBuilder[last] = _statementsBuilder[last].WithTrailingTrivia(trailingTrivia);

                // Replace the original local declaration statement with new statement list
                // from _statementsBuilder.
                if (_localDeclarationStatement.Parent is BlockSyntax || _localDeclarationStatement.Parent is SwitchSectionSyntax)
                {
                    if (_statementsBuilder.Count > 1)
                    {
                        _editor.InsertAfter(_localDeclarationStatement, _statementsBuilder.Skip(1));
                    }

                    _editor.ReplaceNode(_localDeclarationStatement, _statementsBuilder[0]);
                }
                else
                {
                    _editor.ReplaceNode(_localDeclarationStatement, SyntaxFactory.Block(_statementsBuilder));
                }
            }

            private void GenerateDeclarationStatementForCurrentNonDiscardVariables()
            {
                // Generate a variable declaration with all the currently tracked non-discard declarators.
                // For example, for a declaration "int x = 1, y = 2, _ = M(), z = 3;", we generate two variable declarations:
                //   1. "int x = 1, y = 2;" and
                //   2. "int z = 3;",
                // which are split by a single assignment statement "_ = M();"
                if (_currentNonDiscardVariables.Count > 0)
                {
                    var statement = SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxFactory.VariableDeclaration(_localDeclarationStatement.Declaration.Type, _currentNonDiscardVariables))
                                    .WithAdditionalAnnotations(Formatter.Annotation);
                    _statementsBuilder.Add(statement);
                    _currentNonDiscardVariables = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
                }
            }

            private void GenerateAssignmentForDiscardVariable(VariableDeclaratorSyntax variable)
            {
                Debug.Assert(IsDiscardDeclaration(variable));

                // Convert a discard declaration with initializer of the form "int _ = M();" into
                // a discard assignment "_ = M();"
                if (variable.Initializer != null)
                {
                    _statementsBuilder.Add(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                kind: SyntaxKind.SimpleAssignmentExpression,
                                left: SyntaxFactory.IdentifierName(variable.Identifier),
                                operatorToken: variable.Initializer.EqualsToken,
                                right: variable.Initializer.Value))
                        .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
        }
    }
}
