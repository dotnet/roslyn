// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    public partial class RazorSourceGenerator
    {
        internal static string GetIdentifierFromPath(ReadOnlySpan<char> filePath)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            BuildIdentifierFromPath(builder, filePath);

            builder.Append(".g.cs");

            return builder.ToString();
        }

        /// <summary>
        /// A tag helper descriptor's <see cref="TagHelperDescriptor.TypeName"/> with any generic type
        /// arguments removed -- e.g. <c>MyApp.Counter&lt;T&gt;</c> becomes <c>MyApp.Counter</c> -- so it
        /// can be matched against the fallback components' type names, which carry no arity.
        /// </summary>
        internal static string StripGenericArity(string typeName)
        {
            var baseName = TypeNameHelper.GetNonGenericTypeName(typeName, out _);
            return baseName.Length == typeName.Length ? typeName : baseName.ToString();
        }

        /// <summary>
        /// Returns the name of the component type a descriptor belongs to. A component descriptor's owning
        /// type is itself, but descriptors derived from a component -- child content and bind, for example --
        /// carry the owning component's namespace and identifier while their own <see cref="TagHelperDescriptor.TypeName"/>
        /// is suffixed (e.g. <c>Ns.Card.Header</c> for the <c>Header</c> child content of <c>Ns.Card</c>).
        /// Reconstructing the owning name from the namespace and identifier lets ownership filtering keep or
        /// exclude a fallback component and all its derived descriptors together, rather than only the
        /// component descriptor whose <see cref="TagHelperDescriptor.TypeName"/> matches exactly.
        /// </summary>
        internal static string GetOwningTypeName(TagHelperDescriptor descriptor)
        {
            var identifier = descriptor.TypeNameIdentifier;
            if (identifier is null)
            {
                return descriptor.TypeName;
            }

            return descriptor.TypeNamespace is { Length: > 0 } typeNamespace
                ? typeNamespace + "." + identifier
                : identifier;
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

        /// <summary>
        ///  Resolves the fallback component type symbols so the slow discovery path can target just those
        ///  types instead of walking the whole augmented assembly. Uses the compilation's declaration table
        ///  (no semantic models): a fallback type name is namespace-qualified, so the fast predicate keys off
        ///  its final segment and over-selects; the caller's descriptor-name filter trims any collisions.
        /// </summary>
        private static ImmutableArray<INamedTypeSymbol> ResolveFallbackTypes(
            Compilation compilation,
            ImmutableHashSet<string> fallbackTypeNames,
            CancellationToken cancellationToken)
        {
            if (fallbackTypeNames.IsEmpty)
            {
                return [];
            }

            var shortNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in fallbackTypeNames)
            {
                var lastDot = name.LastIndexOf('.');
                shortNames.Add(lastDot >= 0 ? name.Substring(lastDot + 1) : name);
            }

            using var builder = new PooledArrayBuilder<INamedTypeSymbol>();

            foreach (var symbol in compilation.GetSymbolsWithName(shortNames.Contains, SymbolFilter.Type, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (symbol is INamedTypeSymbol typeSymbol)
                {
                    builder.Add(typeSymbol);
                }
            }

            return builder.ToImmutable();
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

                b.Features.Add(new DefaultUtf8WriteLiteralFeature());

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);

                b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpParseOptions.LanguageVersion);
            });

            return new SourceGeneratorProjectEngine(projectEngine);
        }
    }
}
