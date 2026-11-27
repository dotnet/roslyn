// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    public partial class RazorSourceGenerator
    {
        private const int MaxSyntaxTreeDepthForTagHelperDiscovery = 256;

        private static bool AreCompilationsEquivalentForTagHelperDiscovery(
            Compilation oldCompilation,
            Compilation newCompilation,
            ImmutableArray<SyntaxTree> oldSyntaxTrees,
            ImmutableArray<SyntaxTree> newSyntaxTrees)
        {
            if (ReferenceEquals(oldCompilation, newCompilation))
            {
                return true;
            }

            // A body-only edit is safe to skip only if the rest of the compilation still binds the same way.
            if (oldCompilation.ScriptCompilationInfo is not null || newCompilation.ScriptCompilationInfo is not null ||
                oldCompilation.AssemblyName != newCompilation.AssemblyName ||
                !oldCompilation.Options.Equals(newCompilation.Options) ||
                !AreMetadataReferencesEquivalentForTagHelperDiscovery(oldCompilation, newCompilation))
            {
                return false;
            }

            // The tree stage keeps the previous instance when an edit doesn't affect discovery.
            return oldSyntaxTrees.SequenceEqual(newSyntaxTrees);
        }

        private static bool AreSyntaxTreesEquivalentForTagHelperDiscovery(SyntaxTree? oldTree, SyntaxTree? newTree)
        {
            if (ReferenceEquals(oldTree, newTree))
            {
                return true;
            }

            if (oldTree is null || newTree is null ||
                oldTree.FilePath != newTree.FilePath ||
                !oldTree.Options.Equals(newTree.Options))
            {
                return false;
            }

            return AreNodesEquivalentForTagHelperDiscovery(oldTree.GetRoot(), newTree.GetRoot(), oldTree.GetText(), newTree.GetText(), depth: 0);
        }

        private static bool AreNodesEquivalentForTagHelperDiscovery(
            SyntaxNode oldNode,
            SyntaxNode newNode,
            SourceText oldText,
            SourceText newText,
            int depth)
        {
            // Treat unusually deep syntax as a cache miss instead of risking a stack overflow.
            if (depth >= MaxSyntaxTreeDepthForTagHelperDiscovery)
            {
                return false;
            }

            if (oldNode.RawKind != newNode.RawKind)
            {
                return false;
            }

            // Method bodies are the one place edits are allowed, since discovery never looks inside them.
            // Directives are the exception because things like #nullable leak out past the end of the body,
            // and errors are too because a broken body could be swallowing things that come after it.
            if (IsMethodBody(oldNode))
            {
                if (oldNode.ContainsDirectives || newNode.ContainsDirectives)
                {
                    return oldText.GetSubText(oldNode.FullSpan).ContentEquals(newText.GetSubText(newNode.FullSpan));
                }

                return !oldNode.ContainsDiagnostics && !newNode.ContainsDiagnostics;
            }

            // Everywhere else has to match exactly, so walk the children in lock step.
            var oldChildren = oldNode.ChildNodesAndTokens();
            var newChildren = newNode.ChildNodesAndTokens();
            if (oldChildren.Count != newChildren.Count)
            {
                return false;
            }

            for (var i = 0; i < oldChildren.Count; i++)
            {
                var oldChild = oldChildren[i];
                var newChild = newChildren[i];

                if (oldChild.AsNode() is { } oldChildNode && newChild.AsNode() is { } newChildNode)
                {
                    if (!AreNodesEquivalentForTagHelperDiscovery(oldChildNode, newChildNode, oldText, newText, depth + 1))
                    {
                        return false;
                    }
                }
                else if (oldChild.IsToken && newChild.IsToken)
                {
                    if (!AreTokensEquivalentForTagHelperDiscovery(oldChild.AsToken(), newChild.AsToken(), oldText, newText))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsMethodBody(SyntaxNode node)
            => node.Parent switch
            {
                BaseMethodDeclarationSyntax method => node == method.Body || node == method.ExpressionBody,
                AccessorDeclarationSyntax accessor => node == accessor.Body || node == accessor.ExpressionBody,
                LocalFunctionStatementSyntax localFunction => node == localFunction.Body || node == localFunction.ExpressionBody,
                PropertyDeclarationSyntax property => node == property.ExpressionBody,
                IndexerDeclarationSyntax indexer => node == indexer.ExpressionBody,
                _ => false,
            };

        private static bool AreTokensEquivalentForTagHelperDiscovery(SyntaxToken oldToken, SyntaxToken newToken, SourceText oldText, SourceText newText)
            => oldToken.RawKind == newToken.RawKind &&
                oldToken.Text == newToken.Text &&
                AreTriviaListsEquivalentForTagHelperDiscovery(oldToken.LeadingTrivia, newToken.LeadingTrivia, oldText, newText) &&
                AreTriviaListsEquivalentForTagHelperDiscovery(oldToken.TrailingTrivia, newToken.TrailingTrivia, oldText, newText);

        private static bool AreTriviaListsEquivalentForTagHelperDiscovery(SyntaxTriviaList oldTrivia, SyntaxTriviaList newTrivia, SourceText oldText, SourceText newText)
        {
            // Trivia matters because doc comments and directives like #nullable change what discovery sees.
            // The generated #line directives don't though, and they shift around whenever a line is added
            // or removed anywhere in the Razor file, so we skip over them on both sides.
            var oldIndex = 0;
            var newIndex = 0;
            while (true)
            {
                while (oldIndex < oldTrivia.Count && IsLineMappingTrivia(oldTrivia[oldIndex]))
                {
                    oldIndex++;
                }

                while (newIndex < newTrivia.Count && IsLineMappingTrivia(newTrivia[newIndex]))
                {
                    newIndex++;
                }

                if (oldIndex == oldTrivia.Count || newIndex == newTrivia.Count)
                {
                    return oldIndex == oldTrivia.Count && newIndex == newTrivia.Count;
                }

                var oldItem = oldTrivia[oldIndex];
                var newItem = newTrivia[newIndex];
                if (oldItem.RawKind != newItem.RawKind ||
                    !oldText.GetSubText(oldItem.FullSpan).ContentEquals(newText.GetSubText(newItem.FullSpan)))
                {
                    return false;
                }

                oldIndex++;
                newIndex++;
            }
        }

        private static bool AreMetadataReferencesEquivalentForTagHelperDiscovery(Compilation oldCompilation, Compilation newCompilation)
            => oldCompilation.References.SequenceEqual(
                newCompilation.References,
                new LambdaComparer<MetadataReference>((oldReference, newReference) =>
                    AreMetadataReferencesEquivalentForTagHelperDiscovery(oldCompilation, oldReference, newCompilation, newReference)));

        private static bool AreMetadataReferencesEquivalentForTagHelperDiscovery(
            Compilation oldCompilation,
            MetadataReference? oldReference,
            Compilation newCompilation,
            MetadataReference? newReference)
        {
            if (ReferenceEquals(oldReference, newReference))
            {
                return true;
            }

            if (oldReference is null || newReference is null ||
                !oldReference.Properties.Equals(newReference.Properties))
            {
                return false;
            }

            var oldSymbol = oldCompilation.GetAssemblyOrModuleSymbol(oldReference);
            var newSymbol = newCompilation.GetAssemblyOrModuleSymbol(newReference);
            if (oldSymbol is null || newSymbol is null)
            {
                // References skipped by the reference manager don't contribute symbols to discovery.
                return oldSymbol is null && newSymbol is null;
            }

            // Discovery reads XML docs too, so matching PE identity alone isn't enough.
            return SymbolEqualityComparer.Default.Equals(oldSymbol, newSymbol);
        }

        private static bool IsLineMappingTrivia(SyntaxTrivia trivia)
            => trivia.IsKind(CSharpSyntaxKind.LineDirectiveTrivia) ||
                trivia.IsKind(CSharpSyntaxKind.LineSpanDirectiveTrivia) ||
                trivia.IsKind(CSharpSyntaxKind.PragmaChecksumDirectiveTrivia);

        internal static string GetIdentifierFromPath(ReadOnlySpan<char> filePath)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            BuildIdentifierFromPath(builder, filePath);

            builder.Append(".g.cs");

            return builder.ToString();
        }

        /// <summary>
        /// Returns the hint name for the decl half of a Razor component generated source given
        /// the impl half's hint name. The decl file substitutes <c>.decl.g.cs</c> for the
        /// trailing <c>.g.cs</c> -- e.g. <c>Component1_razor.g.cs</c> →
        /// <c>Component1_razor.decl.g.cs</c> -- so both halves keep the <c>.g.cs</c> suffix
        /// (which the editor and MSBuild use to identify generated files) without stacking it.
        /// </summary>
        internal static string GetDeclIdentifierFromHintName(string implHintName)
        {
            const string ImplSuffix = ".g.cs";
            const string DeclSuffix = ".decl.g.cs";

            return implHintName.EndsWith(ImplSuffix, StringComparison.Ordinal)
                ? implHintName.Substring(0, implHintName.Length - ImplSuffix.Length) + DeclSuffix
                : implHintName + DeclSuffix;
        }

        internal static void BuildIdentifierFromPath(StringBuilder builder, ReadOnlySpan<char> filePath)
        {
            for (var i = 0; i < filePath.Length; i++)
            {
                switch (filePath[i])
                {
                    case '\\' or '/' when i + 1 < filePath.Length && filePath[i + 1] is '\\' or '/':
                        // Roslyn will throw on '//', but some weird Uri's have them, so sanitize to '_/'
                        builder.Append('_');
                        break;
                    case '\\' or '/' when i > 0:
                        builder.Append('/');
                        break;
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(filePath[i]);
                        break;
                }
            }
        }

        private static RazorCSharpDocument GetFallbackDiscoveryDeclDocument(
            RazorCodeDocument codeDocument,
            SourceGeneratorProjectItem item,
            ImmutableArray<SourceGeneratorProjectItem> imports,
            RazorSourceGenerationOptions razorSourceGeneratorOptions,
            CancellationToken cancellationToken)
        {
            // Reuse the discoverable decl the split phase built from the already-parsed document and lower
            // it here, so a fallback component's descriptor comes from the initial parse instead of
            // re-parsing the source through a separate declaration engine. The generator processes each
            // document before running discovery over the set, so this lowers ahead of the tag-helper
            // rewrite that mutates the shared IR.
            if (codeDocument.GetRequiredDocumentNode().FallbackDiscoveryDeclDocumentNode is { } declDocumentNode)
            {
                // Mirror DefaultRazorDeclCSharpLoweringPhase: the decl is lowered before the rewrite phase,
                // so seed the rewritten-tree back-reference with the canonical (markup-free) syntax tree.
                var declCodeDocument = codeDocument.GetTagHelperRewrittenSyntaxTree() is null
                    ? codeDocument.WithTagHelperRewrittenSyntaxTree(codeDocument.GetRequiredSyntaxTree())
                    : codeDocument;

                return RazorCSharpDocumentWriter.Write(
                    declDocumentNode,
                    declCodeDocument,
                    reportDiagnostics: false,
                    isDeclarationDocument: true,
                    isStubDocument: false,
                    cancellationToken);
            }

            // Rare fallback shapes (no render method or namespace) leave no discovery decl; re-parse those.
            var declEngine = GetDeclarationProjectEngine(item, imports, razorSourceGeneratorOptions);
            return declEngine.Process(item, cancellationToken).GetRequiredCSharpDocument(declarationDocument: false);
        }

        private static RazorProjectEngine GetDeclarationProjectEngine(
            SourceGeneratorProjectItem item,
            ImmutableArray<SourceGeneratorProjectItem> imports,
            RazorSourceGenerationOptions razorSourceGeneratorOptions)
        {
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(item);
            foreach (var import in imports)
            {
                fileSystem.Add(import);
            }

            var discoveryProjectEngine = RazorProjectEngine.Create(razorSourceGeneratorOptions.Configuration, fileSystem, b =>
            {
                b.ConfigureCodeGenerationOptions(builder =>
                {
                    builder.SuppressPrimaryMethodBody = true;
                    builder.SuppressChecksum = true;
                    builder.SupportLocalizedComponentNames = razorSourceGeneratorOptions.SupportLocalizedComponentNames;
                });

                b.ConfigureParserOptions(builder =>
                {
                    builder.UseRoslynTokenizer = razorSourceGeneratorOptions.UseRoslynTokenizer;
                    builder.CSharpParseOptions = razorSourceGeneratorOptions.CSharpParseOptions;
                });

                b.SetRootNamespace(razorSourceGeneratorOptions.RootNamespace);

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);

                b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpParseOptions.LanguageVersion);
            });

            return discoveryProjectEngine;
        }

        private static StaticCompilationTagHelperFeature GetStaticTagHelperFeature(Compilation compilation)
        {
            var tagHelperFeature = new StaticCompilationTagHelperFeature(compilation);

            // the tagHelperFeature will have its Engine property set as part of adding it to the engine,
            // which is used later when doing the actual discovery
            var discoveryProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, new VirtualRazorProjectFileSystem(), b =>
            {
                b.Features.Add(tagHelperFeature);

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);
            });

            return tagHelperFeature;
        }

        private static SourceGeneratorProjectEngine GetGenerationProjectEngine(
            SourceGeneratorProjectItem item,
            ImmutableArray<SourceGeneratorProjectItem> imports,
            RazorSourceGenerationOptions razorSourceGeneratorOptions)
        {
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(item);
            foreach (var import in imports)
            {
                fileSystem.Add(import);
            }

            var projectEngine = RazorProjectEngine.Create(razorSourceGeneratorOptions.Configuration, fileSystem, b =>
            {
                b.SetRootNamespace(razorSourceGeneratorOptions.RootNamespace);

                b.ConfigureCodeGenerationOptions(builder =>
                {
                    builder.SuppressMetadataSourceChecksumAttributes = !razorSourceGeneratorOptions.GenerateMetadataSourceChecksumAttributes;
                    builder.SupportLocalizedComponentNames = razorSourceGeneratorOptions.SupportLocalizedComponentNames;
                    builder.SuppressUniqueIds = razorSourceGeneratorOptions.TestSuppressUniqueIds;
                    builder.SuppressAddComponentParameter = razorSourceGeneratorOptions.Configuration.SuppressAddComponentParameter;

                    // The generator emits both halves of a split component (the impl `.g.cs` and the
                    // decl `.decl.g.cs`), so it is the one host that opts into the markup split.
                    builder.EnableMarkupSplit = true;
                });

                b.ConfigureParserOptions(builder =>
                {
                    builder.UseRoslynTokenizer = razorSourceGeneratorOptions.UseRoslynTokenizer;
                    builder.CSharpParseOptions = razorSourceGeneratorOptions.CSharpParseOptions;
                });

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);

                b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpParseOptions.LanguageVersion);
            });

            return new SourceGeneratorProjectEngine(projectEngine);
        }
    }
}
