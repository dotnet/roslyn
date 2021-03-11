// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class CodeActionTest<TVerifier> : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        /// <summary>
        /// Gets or sets the index of the code action to apply.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CodeActionIndex"/> and <see cref="CodeActionEquivalenceKey"/> are both specified, the
        /// test will further verify that the two properties refer to the same code action.</para>
        /// </remarks>
        /// <seealso cref="CodeActionEquivalenceKey"/>
        public int? CodeActionIndex { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CodeAction.EquivalenceKey"/> of the code action to apply.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CodeActionIndex"/> and <see cref="CodeActionEquivalenceKey"/> are both specified, the
        /// test will further verify that the two properties refer to the same code action.</para>
        /// </remarks>
        /// <seealso cref="CodeActionIndex"/>
        public string? CodeActionEquivalenceKey { get; set; }

        /// <summary>
        /// Gets or sets an additional verifier for a <see cref="CodeAction"/>. After the code action is selected, it is
        /// passed to this verification method to test any other properties of the code action.
        /// </summary>
        /// <remarks>
        /// <para>For a successful test, the verification action is expected to complete without throwing an
        /// exception.</para>
        /// </remarks>
        public Action<CodeAction, IVerifier>? CodeActionVerifier { get; set; }

        /// <summary>
        /// Gets or sets the validation mode for code actions. The default is
        /// <see cref="CodeActionValidationMode.SemanticStructure"/>.
        /// </summary>
        public CodeActionValidationMode CodeActionValidationMode { get; set; } = CodeActionValidationMode.SemanticStructure;

        /// <summary>
        /// Gets the syntax kind enumeration type for the current code action test.
        /// </summary>
        public abstract Type SyntaxKindType { get; }

        protected static bool CodeActionExpected(SolutionState state)
        {
            return state.InheritanceMode != null
                || state.MarkupHandling != null
                || state.Sources.Any()
                || state.GeneratedSources.Any()
                || state.AdditionalFiles.Any()
                || state.AnalyzerConfigFiles.Any()
                || state.AdditionalFilesFactories.Any();
        }

        protected static bool HasAnyChange(SolutionState oldState, SolutionState newState)
        {
            return !oldState.Sources.SequenceEqual(newState.Sources, SourceFileEqualityComparer.Instance)
                || !oldState.GeneratedSources.SequenceEqual(newState.GeneratedSources, SourceFileEqualityComparer.Instance)
                || !oldState.AdditionalFiles.SequenceEqual(newState.AdditionalFiles, SourceFileEqualityComparer.Instance)
                || !oldState.AnalyzerConfigFiles.SequenceEqual(newState.AnalyzerConfigFiles, SourceFileEqualityComparer.Instance);
        }

        protected static CodeAction? TryGetCodeActionToApply(ImmutableArray<CodeAction> actions, int? codeActionIndex, string? codeActionEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, IVerifier verifier)
        {
            CodeAction? result;
            if (codeActionIndex.HasValue && codeActionEquivalenceKey != null)
            {
                if (actions.Length <= codeActionIndex)
                {
                    return null;
                }

                verifier.Equal(
                    codeActionEquivalenceKey,
                    actions[codeActionIndex.Value].EquivalenceKey,
                    "The code action equivalence key and index must be consistent when both are specified.");

                result = actions[codeActionIndex.Value];
            }
            else if (codeActionEquivalenceKey != null)
            {
                result = actions.FirstOrDefault(x => x.EquivalenceKey == codeActionEquivalenceKey);
            }
            else if (actions.Length > (codeActionIndex ?? 0))
            {
                result = actions[codeActionIndex ?? 0];
            }
            else
            {
                return null;
            }

            if (result is object)
            {
                codeActionVerifier?.Invoke(result, verifier);
            }

            return result;
        }

        protected virtual ImmutableArray<CodeAction> FilterCodeActions(ImmutableArray<CodeAction> actions)
        {
            var builder = actions.ToBuilder();
            while (true)
            {
                var changesMade = false;
                for (var i = builder.Count - 1; i >= 0; i--)
                {
                    var action = builder[i];
                    var nestedActions = action.GetNestedActions();
                    if (!nestedActions.IsEmpty)
                    {
                        builder.RemoveAt(i);
                        for (var j = nestedActions.Length - 1; j >= 0; j--)
                        {
                            builder.Insert(i, nestedActions[j]);
                        }

                        changesMade = true;
                    }
                }

                if (!changesMade)
                {
                    break;
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Apply the inputted <see cref="CodeAction"/> to the inputted document.
        /// Meant to be used to apply code fixes.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to apply the code action on.</param>
        /// <param name="codeAction">A <see cref="CodeAction"/> that will be applied to the
        /// <paramref name="project"/>.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Project"/> with the changes from the <see cref="CodeAction"/>.</returns>
        protected async Task<Project> ApplyCodeActionAsync(Project project, CodeAction codeAction, IVerifier verifier, CancellationToken cancellationToken)
        {
            var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            var changedProject = solution.GetProject(project.Id);
            if (changedProject != project)
            {
                project = await RecreateProjectDocumentsAsync(changedProject, verifier, cancellationToken).ConfigureAwait(false);
            }

            return project;
        }

        /// <summary>
        /// Implements a workaround for issue #936, force re-parsing to get the same sort of syntax tree as the original document.
        /// </summary>
        /// <param name="project">The project to update.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="Project"/>.</returns>
        private async Task<Project> RecreateProjectDocumentsAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                var initialTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                document = await RecreateDocumentAsync(document, cancellationToken).ConfigureAwait(false);
                var recreatedTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (CodeActionValidationMode != CodeActionValidationMode.None)
                {
                    try
                    {
                        // We expect the tree produced by the code fix (initialTree) to match the form of the tree produced
                        // by the compiler for the same text (recreatedTree).
                        TreeEqualityVisitor.AssertNodesEqual(
                            verifier,
                            SyntaxKindType,
                            await recreatedTree.GetRootAsync(cancellationToken).ConfigureAwait(false),
                            await initialTree.GetRootAsync(cancellationToken).ConfigureAwait(false),
                            checkTrivia: CodeActionValidationMode == CodeActionValidationMode.Full);
                    }
                    catch
                    {
                        // Try to revalidate the tree with a better message
                        var renderedInitialTree = TreeToString(await initialTree.GetRootAsync(cancellationToken).ConfigureAwait(false), CodeActionValidationMode);
                        var renderedRecreatedTree = TreeToString(await recreatedTree.GetRootAsync(cancellationToken).ConfigureAwait(false), CodeActionValidationMode);
                        verifier.EqualOrDiff(renderedRecreatedTree, renderedInitialTree);

                        // This is not expected to be hit, but it will be hit if the validation failure occurred in a
                        // portion of the tree not captured by the rendered form from TreeToString.
                        throw;
                    }
                }

                project = document.Project;
            }

            return project;
        }

        private static async Task<Document> RecreateDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var newText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return document.WithText(SourceText.From(newText.ToString(), newText.Encoding, newText.ChecksumAlgorithm));
        }

        private string TreeToString(SyntaxNodeOrToken syntaxNodeOrToken, CodeActionValidationMode validationMode)
        {
            var result = new StringBuilder();
            TreeToString(syntaxNodeOrToken, string.Empty, validationMode, result);
            return result.ToString();
        }

        private void TreeToString(SyntaxNodeOrToken syntaxNodeOrToken, string indent, CodeActionValidationMode validationMode, StringBuilder result)
        {
            if (syntaxNodeOrToken.IsNode)
            {
                result.AppendLine($"{indent}Node({Kind(syntaxNodeOrToken.RawKind)}):");

                var childIndent = indent + "  ";
                foreach (var child in syntaxNodeOrToken.ChildNodesAndTokens())
                {
                    TreeToString(child, childIndent, validationMode, result);
                }
            }
            else
            {
                var syntaxToken = syntaxNodeOrToken.AsToken();
                result.AppendLine($"{indent}Token({Kind(syntaxToken.RawKind)}): {Escape(syntaxToken.Text)}");

                if (validationMode == CodeActionValidationMode.Full)
                {
                    var childIndent = indent + "  ";
                    foreach (var trivia in syntaxToken.LeadingTrivia)
                    {
                        if (trivia.HasStructure)
                        {
                            result.AppendLine($"{childIndent}Leading({Kind(trivia.RawKind)}):");
                            TreeToString(trivia.GetStructure(), childIndent + "  ", validationMode, result);
                        }
                        else
                        {
                            result.AppendLine($"{childIndent}Leading({Kind(trivia.RawKind)}): {Escape(trivia.ToString())}");
                        }
                    }

                    foreach (var trivia in syntaxToken.TrailingTrivia)
                    {
                        if (trivia.HasStructure)
                        {
                            result.AppendLine($"{childIndent}Trailing({Kind(trivia.RawKind)}):");
                            TreeToString(trivia.GetStructure(), childIndent + "  ", validationMode, result);
                        }
                        else
                        {
                            result.AppendLine($"{childIndent}Trailing({Kind(trivia.RawKind)}): {Escape(trivia.ToString())}");
                        }
                    }
                }
            }

            // Local functions
            string Escape(string text)
            {
                return text
                    .Replace("\\", "\\\\")
                    .Replace("\t", "\\t")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");
            }

            string Kind(int syntaxKind)
            {
                if (SyntaxKindType.GetTypeInfo()?.IsEnum ?? false)
                {
                    return Enum.Format(SyntaxKindType, (ushort)syntaxKind, "G");
                }
                else
                {
                    return syntaxKind.ToString();
                }
            }
        }

        private sealed class SourceFileEqualityComparer : IEqualityComparer<(string filename, SourceText content)>
        {
            private SourceFileEqualityComparer()
            {
            }

            public static SourceFileEqualityComparer Instance { get; } = new SourceFileEqualityComparer();

            public bool Equals((string filename, SourceText content) x, (string filename, SourceText content) y)
            {
                if (x.filename != y.filename)
                {
                    return false;
                }

                if (x.content is null || y.content is null)
                {
                    return ReferenceEquals(x, y);
                }

                return x.content.Encoding == y.content.Encoding
                    && x.content.ChecksumAlgorithm == y.content.ChecksumAlgorithm
                    && x.content.ContentEquals(y.content);
            }

            public int GetHashCode((string filename, SourceText content) obj)
            {
                return obj.filename.GetHashCode()
                    ^ (obj.content?.ToString().GetHashCode() ?? 0);
            }
        }

        private class TreeEqualityVisitor
        {
            private readonly IVerifier _verifier;
            private readonly Type _syntaxKindType;
            private readonly SyntaxNode _expected;
            private readonly bool _checkTrivia;

            private TreeEqualityVisitor(IVerifier verifier, Type syntaxKindType, SyntaxNode expected, bool checkTrivia)
            {
                _verifier = verifier;
                _syntaxKindType = syntaxKindType;
                _expected = expected ?? throw new ArgumentNullException(nameof(expected));
                _checkTrivia = checkTrivia;
            }

            public void Visit(SyntaxNode node)
            {
                AssertSyntaxKindEqual(_expected.RawKind, node.RawKind);
                AssertChildSyntaxListEqual(_expected.ChildNodesAndTokens(), node.ChildNodesAndTokens(), _checkTrivia);
            }

            internal static void AssertNodesEqual(IVerifier verifier, Type syntaxKindType, SyntaxNode expected, SyntaxNode actual, bool checkTrivia)
            {
                new TreeEqualityVisitor(verifier, syntaxKindType, expected, checkTrivia).Visit(actual);
            }

            private void AssertNodesEqual(SyntaxNode expected, SyntaxNode actual, bool checkTrivia)
            {
                AssertNodesEqual(_verifier, _syntaxKindType, expected, actual, checkTrivia);
            }

            private void AssertChildSyntaxListEqual(ChildSyntaxList expected, ChildSyntaxList actual, bool checkTrivia)
            {
                _verifier.Equal(expected.Count, actual.Count);
                foreach (var (expectedChild, actualChild) in expected.Zip(actual, (first, second) => (first, second)))
                {
                    if (expectedChild.IsToken)
                    {
                        _verifier.True(actualChild.IsToken);
                        AssertTokensEqual(expectedChild.AsToken(), actualChild.AsToken(), checkTrivia);
                    }
                    else
                    {
                        _verifier.True(actualChild.IsNode);
                        AssertNodesEqual(expectedChild.AsNode(), actualChild.AsNode(), checkTrivia);
                    }
                }
            }

            private void AssertTokensEqual(SyntaxToken expected, SyntaxToken actual, bool checkTrivia)
            {
                AssertTriviaListEqual(expected.LeadingTrivia, actual.LeadingTrivia, checkTrivia);
                AssertSyntaxKindEqual(expected.RawKind, actual.RawKind);
                _verifier.Equal(expected.Value, actual.Value);
                _verifier.Equal(expected.Text, actual.Text);
                _verifier.Equal(expected.ValueText, actual.ValueText);
                AssertTriviaListEqual(expected.TrailingTrivia, actual.TrailingTrivia, checkTrivia);
            }

            private void AssertTriviaListEqual(SyntaxTriviaList expected, SyntaxTriviaList actual, bool checkTrivia)
            {
                if (!checkTrivia)
                {
                    return;
                }

                for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
                {
                    AssertTriviaEqual(expected[i], actual[i], checkTrivia);
                }

                _verifier.Equal(expected.Count, actual.Count);
            }

            private void AssertTriviaEqual(SyntaxTrivia expected, SyntaxTrivia actual, bool checkTrivia)
            {
                if (!checkTrivia)
                {
                    return;
                }

                AssertSyntaxKindEqual(expected.RawKind, actual.RawKind);
                _verifier.Equal(expected.HasStructure, actual.HasStructure);
                _verifier.Equal(expected.IsDirective, actual.IsDirective);
                _verifier.Equal(expected.GetAnnotations(), actual.GetAnnotations());
                if (expected.HasStructure)
                {
                    AssertNodesEqual(expected.GetStructure(), actual.GetStructure(), checkTrivia);
                }
            }

            private void AssertSyntaxKindEqual(int expected, int actual)
            {
                if (expected == actual)
                {
                    return;
                }

                if (_syntaxKindType.GetTypeInfo()?.IsEnum ?? false)
                {
                    _verifier.Equal(
                        Enum.Format(_syntaxKindType, (ushort)expected, "G"),
                        Enum.Format(_syntaxKindType, (ushort)actual, "G"));
                }
                else
                {
                    _verifier.Equal(expected, actual);
                }
            }
        }
    }
}
