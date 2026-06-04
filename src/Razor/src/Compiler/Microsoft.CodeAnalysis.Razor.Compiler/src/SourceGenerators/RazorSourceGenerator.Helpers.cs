// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Text;
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

        /// <summary>
        /// Determines whether <paramref name="namespaceName"/> resolves to an actual namespace in
        /// <paramref name="compilation"/>. Used to identify <c>@using</c> directives that the C#
        /// compiler will fail to bind (CS0246 / CS0234) so the source generator can omit them from
        /// the impl half (the decl half always emits the full set, ensuring the diagnostic fires
        /// exactly once instead of duplicating across halves).
        /// </summary>
        /// <remarks>
        /// Walks <c>compilation.GlobalNamespace</c> through the dotted segments rather than using
        /// <see cref="Compilation.GetTypeByMetadataName"/> (which is types-only) or running a full
        /// semantic-model binding (which would require attaching to a syntax tree).
        /// </remarks>
        private static bool ResolvesAsNamespace(Compilation compilation, string namespaceName)
        {
            INamespaceSymbol? current = compilation.GlobalNamespace;
            var remaining = namespaceName.AsSpan();

            while (!remaining.IsEmpty)
            {
                var dotIndex = remaining.IndexOf('.');
                ReadOnlySpan<char> segment;
                if (dotIndex < 0)
                {
                    segment = remaining;
                    remaining = default;
                }
                else
                {
                    segment = remaining[..dotIndex];
                    remaining = remaining[(dotIndex + 1)..];
                }

                if (segment.IsEmpty)
                {
                    // Malformed (`A..B`, leading/trailing dot, etc.): treat as unresolvable.
                    return false;
                }

                var segmentString = segment.ToString();
                INamespaceSymbol? match = null;
                foreach (var child in current!.GetNamespaceMembers())
                {
                    if (string.Equals(child.Name, segmentString, StringComparison.Ordinal))
                    {
                        match = child;
                        break;
                    }
                }

                if (match is null)
                {
                    return false;
                }

                current = match;
            }

            return current is not null;
        }

        /// <summary>
        /// Returns the simple namespace target of an <c>@using</c> directive's <c>Content</c>
        /// string, or <see langword="null"/> for forms we don't validate here (static using,
        /// aliases, malformed content).
        /// </summary>
        /// <remarks>
        /// Conservative on purpose: anything we can't cleanly classify falls through to "don't
        /// touch this using." That keeps the impl half identical to the decl half for those
        /// usings (the existing behaviour) -- worst case is the pre-existing duplicate diagnostic,
        /// not a regression.
        /// </remarks>
        private static string? TryGetPlainUsingNamespace(string content)
        {
            var span = content.AsSpan().Trim();
            if (span.IsEmpty)
            {
                return null;
            }

            // `using static System.Math;` -- the target is a type, not a namespace, so we don't
            // attempt resolution (would always fail under ResolvesAsNamespace).
            if (span.StartsWith("static ".AsSpan(), StringComparison.Ordinal) ||
                span.StartsWith("static\t".AsSpan(), StringComparison.Ordinal))
            {
                return null;
            }

            // `using Alias = Some.Namespace;` -- the alias target can be a type or namespace and
            // resolution logic is different; defer to the C# compiler.
            if (span.IndexOf('=') >= 0)
            {
                return null;
            }

            foreach (var ch in span)
            {
                if (ch != '.' && ch != '_' && !char.IsLetterOrDigit(ch))
                {
                    return null;
                }
            }

            return span.ToString();
        }

        /// <summary>
        /// Structural equality for the per-file "impl skip usings" set, treating null and empty
        /// sets as equal. Used by the enrichedDocuments comparer so that compilation changes that
        /// don't actually flip any using's resolvability don't invalidate downstream caches.
        /// </summary>
        private static bool AreImplSkipUsingsEqual(ImmutableHashSet<string>? a, ImmutableHashSet<string>? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            var aCount = a?.Count ?? 0;
            var bCount = b?.Count ?? 0;
            if (aCount == 0 && bCount == 0)
            {
                return true;
            }

            if (aCount != bCount)
            {
                return false;
            }

            return a!.SetEquals(b!);
        }
    }
}
