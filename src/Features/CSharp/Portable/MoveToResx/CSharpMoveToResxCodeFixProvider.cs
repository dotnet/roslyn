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
            => new MoveToResxFixAllProvider();

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root?.FindNode(diagnostic.Location.SourceSpan) as LiteralExpressionSyntax;
            if (node == null)
                return;

            var allResx = GetAllResxFiles(context.Document.Project).ToList();
            if (allResx.Count == 0)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "No .resx file found. Please create a .resx file in your project or folder.",
                        createChangedDocument: c => Task.FromResult(context.Document), // No-op
                        equivalenceKey: "NoResxFileFound"),
                    diagnostic);
                return;
            }

            var nestedActions = allResx
                .Select(resx =>
                {
                    var resxName = Path.GetFileName(resx.FilePath);
                    return CodeAction.Create(
                        title: resxName,
                        createChangedSolution: c => MoveStringToResxAndReplaceLiteralAsync(context.Document, node, resx, c),
                        equivalenceKey: $"MoveStringTo_{resxName}");
                })
                .ToImmutableArray();

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Move string to .resx resource",
                    nestedActions,
                    isInlinable: false),
                diagnostic);
        }

        private static IEnumerable<TextDocument> GetAllResxFiles(Project project)
        {
            return project.AdditionalDocuments
                .Where(f => f.FilePath != null && string.Equals(".resx", Path.GetExtension(f.FilePath), StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<Solution> MoveStringToResxAndReplaceLiteralAsync(
            Document document,
            LiteralExpressionSyntax stringLiteral,
            TextDocument resxFile,
            CancellationToken cancellationToken)
        {
            var resxSourceText = await resxFile.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var xdoc = XDocument.Parse(resxSourceText.ToString());

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

            string updatedResxText;
            using (var sw = new StringWriter())
            {
                xdoc.Save(sw);
                updatedResxText = sw.ToString();
            }

            var newResxSourceText = SourceText.From(updatedResxText, resxSourceText.Encoding);
            var newSolution = document.Project.Solution.WithAdditionalDocumentText(resxFile.Id, newResxSourceText);

            var resourceClass = Path.GetFileNameWithoutExtension(resxFile.Name);
            var ns = document.Project.DefaultNamespace;
            string resourceAccessString = !string.IsNullOrEmpty(ns)
                ? $"{ns}.{resourceClass}.{name}"
                : $"{resourceClass}.{name}";

            var resourceAccess = SyntaxFactory.ParseExpression(resourceAccessString).WithTriviaFrom(stringLiteral);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(stringLiteral, resourceAccess);
            newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, newRoot);

            return newSolution;
        }

        private static string ToDeterministicResourceKey(string value, int maxWords = 6)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "emptyString";

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

            if (words.Count > maxWords)
                words = words.Take(maxWords).ToList();

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

        private class MoveToResxFixAllProvider : FixAllProvider
        {
            public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
                => new[] { FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution };

            public override async Task<CodeAction?> GetFixAsync(FixAllContext context)
            {
                // Only support FixAll in Document for simplicity
                if (context.Document == null)
                    return null;

                var diagnostics = (await context.GetDocumentDiagnosticsToFixAsync()).Values.SelectMany(x => x).ToList();
                if (diagnostics.Count == 0)
                    return null;

                var document = context.Document;
                var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var project = document.Project;
                var allResx = GetAllResxFiles(project).ToList();
                if (allResx.Count == 0)
                    return null;
                var resx = allResx.First();
                var resxSourceText = await resx.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
                var xdoc = XDocument.Parse(resxSourceText.ToString());
                var newRoot = root;
                var updated = false;
                foreach (var diagnostic in diagnostics)
                {
                    var node = root.FindNode(diagnostic.Location.SourceSpan) as LiteralExpressionSyntax;
                    if (node == null)
                        continue;
                    var value = node.Token.ValueText;
                    var name = ToDeterministicResourceKey(value);
                    var dataElement = xdoc.Root.Elements("data").FirstOrDefault(e => (string)e.Attribute("name") == name);
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
                    var resourceClass = Path.GetFileNameWithoutExtension(resx.Name);
                    var ns = project.DefaultNamespace;
                    string resourceAccessString = !string.IsNullOrEmpty(ns)
                        ? $"{ns}.{resourceClass}.{name}"
                        : $"{resourceClass}.{name}";
                    var resourceAccess = SyntaxFactory.ParseExpression(resourceAccessString).WithTriviaFrom(node);
                    newRoot = newRoot.ReplaceNode(node, resourceAccess);
                    updated = true;
                }
                if (!updated)
                    return null;
                string updatedResxText;
                using (var sw = new StringWriter())
                {
                    xdoc.Save(sw);
                    updatedResxText = sw.ToString();
                }
                var newResxSourceText = SourceText.From(updatedResxText, resxSourceText.Encoding);
                var newSolution = document.Project.Solution.WithAdditionalDocumentText(resx.Id, newResxSourceText)
                    .WithDocumentSyntaxRoot(document.Id, newRoot);
                return CodeAction.Create(
                    "Move all strings to .resx resource",
                    ct => Task.FromResult(newSolution),
                    nameof(MoveToResxFixAllProvider));
            }
        }
    }
}
