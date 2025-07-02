// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.IO;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpMoveToResxCodeFixProvider)), Shared]
    internal class CSharpMoveToResxCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        public CSharpMoveToResxCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(MoveToResx.DiagnosticId);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root?.FindNode(diagnostic.Location.SourceSpan) as LiteralExpressionSyntax;
            if (node == null)
                return;

            var resxFile = GetResxAdditionalFiles(context.Document.Project).FirstOrDefault();
            if (resxFile != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Move string to .resx resource (in-memory)",
                        createChangedSolution: c => MoveStringToResxAndReplaceLiteralAsync(context.Document, node, resxFile, c),
                        equivalenceKey: "MoveStringToResxInMemory"),
                    diagnostic);
            }
            else
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "No resx file found in AdditionalFiles. Please add a .resx file to your project.",
                        createChangedDocument: c => Task.FromResult(context.Document), // No-op
                        equivalenceKey: "NoResxFileFound"),
                    diagnostic);
            }
        }

        private static IEnumerable<TextDocument> GetResxAdditionalFiles(Project project)
        {
            foreach (var file in project.AdditionalDocuments)
            {
                if (file.FilePath != null &&
                    string.Equals(".resx", Path.GetExtension(file.FilePath), StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }

        // This method parses, updates, and saves the resx file in memory, and replaces the string literal with a resource reference
        private static async Task<Solution> MoveStringToResxAndReplaceLiteralAsync(
            Document document,
            LiteralExpressionSyntax stringLiteral,
            TextDocument resxFile,
            CancellationToken cancellationToken)
        {
            // 1. Get the SourceText from the resx AdditionalDocument
            var resxSourceText = await resxFile.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // 2. Parse the SourceText with XDocument
            var xdoc = XDocument.Parse(resxSourceText.ToString());

            // 3. Add or update the resource entry
            var value = stringLiteral.Token.ValueText;
            var name = ToDeterministicResourceKey(value);

            var dataElement = xdoc.Root.Elements("data")
                .FirstOrDefault(e => (string)e.Attribute("name") == name);

            if (dataElement == null)
            {
                dataElement = new XElement("data",
                    new XAttribute("name", name),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value));
                xdoc.Root.Add(dataElement);
            }
            else
            {
                dataElement.Element("value")!.Value = value;
            }

            // 4. Serialize the updated XDocument
            string updatedResxText;
            using (var sw = new StringWriter())
            {
                xdoc.Save(sw);
                updatedResxText = sw.ToString();
            }

            // 5. Create new SourceText
            var newResxSourceText = SourceText.From(updatedResxText, resxSourceText.Encoding);

            // 6. Update the AdditionalDocument in the Solution (in memory)
            var newSolution = document.Project.Solution.WithAdditionalDocumentText(resxFile.Id, newResxSourceText);

            // 7. Replace the string literal in the code with a resource reference, including namespace if available
            var resourceClass = Path.GetFileNameWithoutExtension(resxFile.Name);
            var ns = document.Project.DefaultNamespace;
            string resourceAccessString = !string.IsNullOrEmpty(ns)
                ? $"{ns}.{resourceClass}.{name}"
                : $"{resourceClass}.{name}";

            // After replacing the string literal:
            var resourceAccess = SyntaxFactory.ParseExpression(resourceAccessString).WithTriviaFrom(stringLiteral);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(stringLiteral, resourceAccess);
            newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, newRoot);

            // 8. Return the updated Solution
            return newSolution;
        }

        private static string ToDeterministicResourceKey(string value, int maxWords = 6)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "emptyString";

            // Split the string into words using non-alphanumeric as delimiters
            var words = new List<string>();
            var sb = new StringBuilder();
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0)
                {
                    words.Add(sb.ToString().ToLowerInvariant());
                    sb.Clear();
                }
            }
            if (sb.Length > 0)
                words.Add(sb.ToString().ToLowerInvariant());

            // Limit to maxWords
            if (words.Count > maxWords)
                words = words.Take(maxWords).ToList();

            // Convert to lowerCamelCase
            var keyBuilder = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                var word = words[i];
                if (i == 0)
                    keyBuilder.Append(word);
                else if (word.Length > 0)
                    keyBuilder.Append(char.ToUpperInvariant(word[0]) + word.Substring(1));
            }

            var key = keyBuilder.ToString();
            if (key.Length == 0)
                key = "emptyString";
            else if (char.IsDigit(key[0]))
                key = "_" + key;

            return key;
        }
    }
}
