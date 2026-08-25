// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
#pragma warning disable RS1041 // This compiler extension should not be implemented in an assembly with target framework '.NET 8.0'. References to other target frameworks will cause the compiler to behave unpredictably.
    [Generator]
#pragma warning restore RS1041 // This compiler extension should not be implemented in an assembly with target framework '.NET 8.0'. References to other target frameworks will cause the compiler to behave unpredictably.
    public partial class RazorSourceGenerator : IIncrementalGenerator
    {
        private static RazorSourceGeneratorEventSource Log => RazorSourceGeneratorEventSource.Log;

        // Testing usage only.
        private readonly string? _testSuppressUniqueIds;

        public RazorSourceGenerator()
        {
        }

        internal RazorSourceGenerator(string testUniqueIds)
        {
            _testSuppressUniqueIds = testUniqueIds;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var analyzerConfigOptions = context.AnalyzerConfigOptionsProvider;
            var parseOptions = context.ParseOptionsProvider;
            var compilation = context.CompilationProvider;
            var additionalTexts = context.AdditionalTextsProvider;
            var metadataRefs = context.MetadataReferencesProvider;

            var razorSourceGeneratorOptions = analyzerConfigOptions
                .Combine(parseOptions)
                .Combine(metadataRefs.Collect())
                .Select(ComputeRazorSourceGeneratorOptions)
                .WithTrackingName("RazorSourceGeneratorOptions")
                .ReportDiagnostics(context);

            var sourceItems = additionalTexts
                .Where(static (file) => FileUtilities.IsAnyRazorFilePath(file.Path, StringComparison.OrdinalIgnoreCase))
                .Combine(analyzerConfigOptions)
                .Select(ComputeProjectItems)
                .ReportDiagnostics(context);

            var hasRazorFiles = sourceItems.Collect()
                .Select(static (sourceItems, _) => sourceItems.Any());

            var importFiles = sourceItems.Where(static file =>
            {
                var path = file.FilePath;
                if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    return string.Equals(fileName, "_Imports", StringComparison.OrdinalIgnoreCase);
                }
                else if (path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    return string.Equals(fileName, "_ViewImports", StringComparison.OrdinalIgnoreCase);
                }

                return false;
            });

            // Parse and lower each Razor file through decl C# lowering (engine phases up to and including
            // decl lowering). A splittable component yields the resolution-independent decl half here; a
            // fallback component (or non-component) yields a null main-run decl, and the separate
            // declaration engine produces a discovery-only decl for the fallback components.
            var withOptions = sourceItems
                .Combine(importFiles.Collect())
                .WithLambdaComparer((old, @new) => old.Left.Equals(@new.Left) && old.Right.SequenceEqual(@new.Right))
                .Combine(razorSourceGeneratorOptions);

            var processedDocuments = withOptions
                .Select((pair, cancellationToken) =>
                {
                    var ((sourceItem, imports), razorSourceGeneratorOptions) = pair;

                    RazorSourceGeneratorEventSource.Log.ParseRazorDocumentStart(sourceItem.RelativePhysicalPath);

                    var projectEngine = GetGenerationProjectEngine(sourceItem, imports, razorSourceGeneratorOptions);
                    var document = projectEngine.ProcessInitialParse(sourceItem, cancellationToken);

                    // A component the split couldn't partition needs its full descriptor from the separate
                    // declaration engine (the fallback discovery path); the split phase records its type
                    // name. Its discovery-only decl is added to the augmented discovery compilation but
                    // never emitted -- the single impl already carries the whole class, and any type shell
                    // for resolution is emitted from GetDeclCSharpDocument() below.
                    RazorCSharpDocument? fallbackDecl = null;
                    string? fallbackTypeName = null;
                    if (document.CodeDocument.GetRequiredDocumentNode().FallbackComponentTypeName is { } typeName &&
                        FileUtilities.IsRazorComponentFilePath(sourceItem.FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        RazorSourceGeneratorEventSource.Log.GenerateDeclarationCodeStart(sourceItem.FilePath);
                        fallbackDecl = GetFallbackDiscoveryDeclDocument(document.CodeDocument, sourceItem, imports, razorSourceGeneratorOptions, cancellationToken);
                        fallbackTypeName = typeName;
                        RazorSourceGeneratorEventSource.Log.GenerateDeclarationCodeStop(sourceItem.FilePath);
                    }

                    RazorSourceGeneratorEventSource.Log.ParseRazorDocumentStop(sourceItem.RelativePhysicalPath);
                    return (projectEngine, path: sourceItem.RelativePhysicalPath, document, fallbackDecl, fallbackTypeName);
                })
                .WithTrackingName("ProcessedDocuments");

            // The generation flow (UTF-8 map, tag-helper rewrite, impl codegen) only needs the parsed
            // document; keeping it a 3-tuple keeps the fallback decl out of that flow's cache key.
            var parsedDocuments = processedDocuments
                .Select(static (item, _) => (item.projectEngine, item.path, item.document))
                .WithTrackingName("ParsedDocuments");

            // Split decls: the resolution-independent decl half of a splittable component, emitted via
            // pre-compilation source output so it is in the compilation that tag-helper discovery sees
            // (and compiled into the output as the decl partial alongside the impl partial).
            var declSources = processedDocuments
                .Select(static (item, _) =>
                {
                    var declCSharpDocument = item.document.CodeDocument.GetCSharpDocument(declarationDocument: true);
                    return (hintName: GetIdentifierFromPath(item.path), declCSharpDocument);
                })
                .Where(static item => item.declCSharpDocument is not null)
                .WithLambdaComparer(static (a, b) =>
                    a.hintName == b.hintName &&
                    a.declCSharpDocument!.Text.ContentEquals(b.declCSharpDocument!.Text))
                .WithTrackingName("DeclSources");

#pragma warning disable RSEXPERIMENTAL007 // RegisterPreCompilationSourceOutput is experimental: emit the split decl before the compilation is built so tag-helper discovery sees it.
            context.RegisterPreCompilationSourceOutput(declSources, static (context, pair) =>
            {
                var (hintName, declCSharpDocument) = pair;
                context.AddSource(GetDeclIdentifierFromHintName(hintName), declCSharpDocument!.Text);
            });
#pragma warning restore RSEXPERIMENTAL007

            // Fallback decls: discovery-only decl syntax trees for components that couldn't be split.
            // Empty when everything splits -- in which case slowDiscovery below is skipped entirely.
            var fallbackDeclTrees = processedDocuments
                .Select(static (item, _) => item.fallbackDecl)
                .Where(static decl => decl is not null)
                // Match the split-decl path (DeclSources): a fallback component's discovery decl is
                // markup-free and checksum-suppressed, so a markup-only edit leaves it byte-identical.
                // Comparing on text keeps a new-but-equal instance from re-parsing here and, through the
                // Collect below, from re-running slow discovery over every fallback component.
                .WithLambdaComparer(static (a, b) => a!.Text.ContentEquals(b!.Text))
                .Combine(parseOptions)
                .Select(static (pair, ct) =>
                    CSharpSyntaxTree.ParseText(pair.Left!.Text, (CSharpParseOptions)pair.Right, cancellationToken: ct))
                .Collect()
                .WithTrackingName("FallbackDeclTrees");

            // fastDiscovery: tag helpers in the standard compilation, which -- once the split decls are
            // added via pre-compilation output -- already contains every splittable component.
            var fastTagHelpers = compilation
                .Combine(razorSourceGeneratorOptions)
                .Select(static (pair, cancellationToken) =>
                {
                    var (compilation, razorSourceGeneratorOptions) = pair;

                    RazorSourceGeneratorEventSource.Log.DiscoverTagHelpersFromCompilationStart();
                    var tagHelperFeature = GetStaticTagHelperFeature(compilation);
                    var collection = tagHelperFeature.GetTagHelpers(compilation.Assembly, cancellationToken);
                    RazorSourceGeneratorEventSource.Log.DiscoverTagHelpersFromCompilationStop();

                    return collection;
                })
                .WithLambdaComparer(static (a, b) => a!.SequenceEqual(b!))
                .WithTrackingName("FastTagHelpers");

            // The namespace-qualified names (no generic arity) of the fallback components' types, collected
            // from the IR-derived name captured while each document was processed. A fallback type can also
            // be declared partially in the main compilation (a component-as-tag-helper partial), so this
            // drives excluding those types from fastDiscovery -- slowDiscovery discovers them completely over
            // the augmented compilation instead.
            var fallbackTypeNames = processedDocuments
                .Select(static (item, _) => item.fallbackTypeName)
                .Collect()
                .Select(static (names, _) =>
                {
                    var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
                    foreach (var name in names)
                    {
                        if (name is not null)
                        {
                            builder.Add(name);
                        }
                    }

                    return builder.ToImmutable();
                })
                .WithTrackingName("FallbackTypeNames");

            // slowDiscovery: only runs when something fell back. Augment the compilation with the fallback
            // decl trees, discover over it, and keep just the fallback components' types (the augmented
            // assembly also contains the split decls and user source that fastDiscovery covers). A fallback
            // type is discovered here completely -- including any half declared in a partial C# file -- so
            // slowDiscovery, not fastDiscovery, owns the full descriptor for it.
            var slowTagHelpers = fallbackDeclTrees
                .Combine(compilation)
                .Combine(fallbackTypeNames)
                .Select(static (pair, cancellationToken) =>
                {
                    var ((fallbackTrees, compilation), fallbackTypeNames) = pair;
                    if (fallbackTrees.IsDefaultOrEmpty)
                    {
                        return TagHelperCollection.Empty;
                    }

                    var augmented = compilation.AddSyntaxTrees(fallbackTrees);

                    // Discover only the fallback components' own types rather than re-walking the whole
                    // augmented assembly. fastDiscovery already covered every splittable component, so the
                    // full walk here would rediscover all of them just to keep the few fallback types. The
                    // fallback types are exactly the ones declared by the discovery-only decl trees we just
                    // added, and tag-helper producers examine each type independently, so discovering just
                    // those yields the same descriptors the full walk would for them.
                    var fallbackTypes = ResolveFallbackTypes(augmented, fallbackTypeNames, cancellationToken);
                    if (fallbackTypes.IsDefaultOrEmpty)
                    {
                        return TagHelperCollection.Empty;
                    }

                    var tagHelperFeature = GetStaticTagHelperFeature(augmented);
                    var discovered = tagHelperFeature.GetTagHelpers(fallbackTypes, cancellationToken);
                    if (discovered.IsEmpty)
                    {
                        return TagHelperCollection.Empty;
                    }

                    // A resolved type can be a partial that also produces a non-fallback descriptor name;
                    // keep only the fallback components' types, matching the ownership split with fastDiscovery.
                    return discovered.Where(fallbackTypeNames, static (descriptor, names) => names.Contains(StripGenericArity(GetOwningTypeName(descriptor))));
                })
                .WithLambdaComparer(static (a, b) => a!.SequenceEqual(b!))
                .WithTrackingName("SlowTagHelpers");

            // fastDiscovery covers the standard compilation (split decls + user types); slowDiscovery owns
            // the fallback types completely. Exclude the fallback types from fastDiscovery so a fallback
            // type that is also a compilation partial isn't emitted twice (or with fastDiscovery's
            // incomplete, partial-only descriptor).
            var tagHelpersFromCompilation = fastTagHelpers
                .Combine(slowTagHelpers)
                .Combine(fallbackTypeNames)
                .Select(static (pair, _) =>
                {
                    var ((fast, slow), fallbackTypeNames) = pair;
                    var fastOwned = fallbackTypeNames.IsEmpty
                        ? fast
                        : fast.Where(fallbackTypeNames, static (descriptor, names) => !names.Contains(StripGenericArity(GetOwningTypeName(descriptor))));
                    return TagHelperCollection.Merge(fastOwned, slow);
                })
                .WithTrackingName("TagHelpersFromCompilation");

            var tagHelpersFromReferences = compilation
                .Combine(razorSourceGeneratorOptions)
                .Combine(hasRazorFiles)
                .WithLambdaComparer(static (a, b) =>
                {
                    var ((compilationA, razorSourceGeneratorOptionsA), hasRazorFilesA) = a;
                    var ((compilationB, razorSourceGeneratorOptionsB), hasRazorFilesB) = b;

                    // When using the generator cache in the compiler it's possible to encounter metadata references that are different instances
                    // but ultimately represent the same underlying assembly. We compare the module version ids to determine if the references are the same
                    if (!compilationA.References.SequenceEqual(compilationB.References, new LambdaComparer<MetadataReference>((old, @new) =>
                    {
                        if (ReferenceEquals(old, @new))
                        {
                            return true;
                        }

                        if (old is null || @new is null)
                        {
                            return false;
                        }

                        var oldSymbol = compilationA.GetAssemblyOrModuleSymbol(old);
                        var newSymbol = compilationB.GetAssemblyOrModuleSymbol(@new);

                        if (SymbolEqualityComparer.Default.Equals(oldSymbol, newSymbol))
                        {
                            return true;
                        }

                        if (oldSymbol is not IAssemblySymbol oldAssembly || newSymbol is not IAssemblySymbol newAssembly)
                        {
                            return false;
                        }

                        // Compare the MVIDs of the modules in each assembly. If they aren't present or don't match we don't consider them equal
                        var oldModules = oldAssembly.Modules.ToArray();
                        var newModules = newAssembly.Modules.ToArray();
                        if (oldModules.Length != newModules.Length)
                        {
                            return false;
                        }

                        for (int i = 0; i < oldModules.Length; i++)
                        {
                            var oldMetadata = oldModules[i].GetMetadata();
                            var newMetadata = newModules[i].GetMetadata();

                            if (oldMetadata is null || newMetadata is null)
                            {
                                return false;
                            }

                            if (oldMetadata.GetModuleVersionId() != newMetadata.GetModuleVersionId())
                            {
                                return false;
                            }
                        }

                        // All module MVIDs matched.
                        return true;
                    })))
                    {
                        return false;
                    }

                    if (razorSourceGeneratorOptionsA != razorSourceGeneratorOptionsB)
                    {
                        return false;
                    }

                    return hasRazorFilesA == hasRazorFilesB;
                })
                .Select(static (pair, cancellationToken) =>
                {
                    var ((compilation, razorSourceGeneratorOptions), hasRazorFiles) = pair;
                    if (!hasRazorFiles)
                    {
                        // If there's no razor code in this app, don't do anything.
                        return [];
                    }

                    RazorSourceGeneratorEventSource.Log.DiscoverTagHelpersFromReferencesStart();
                    var tagHelperFeature = GetStaticTagHelperFeature(compilation);

                    using var collections = new MemoryBuilder<TagHelperCollection>(initialCapacity: 512, clearArray: true);

                    foreach (var reference in compilation.References)
                    {
                        if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                        {
                            var collection = tagHelperFeature.GetTagHelpers(assembly, cancellationToken);
                            if (!collection.IsEmpty)
                            {
                                collections.Append(collection);
                            }
                        }
                    }

                    RazorSourceGeneratorEventSource.Log.DiscoverTagHelpersFromReferencesStop();

                    return TagHelperCollection.Merge(collections.AsMemory().Span);
                })
                .WithTrackingName("TagHelpersFromReferences");

            var allTagHelpers = tagHelpersFromCompilation
                .Combine(tagHelpersFromReferences)
                .Select(static (pair, _) =>
                {
                    return TagHelperCollection.Merge(pair.Left, pair.Right);
                });

            // Build a map of which @inherits base types support UTF-8 WriteLiteral.
            var utf8SupportMap = parsedDocuments
                .Select(static (item, _) =>
                {
                    var codeDocument = item.Item3.CodeDocument;
                    return (codeDocument, InheritsValue: codeDocument.GetInheritsDirectiveValue());
                })
                .Where(static item => item.InheritsValue is not null)
                .Select(static (item, _) => new DefaultUtf8WriteLiteralFeature.InheritsInfo(
                    item.codeDocument.Source.FilePath ?? string.Empty, item.InheritsValue!, item.codeDocument.GetUsingDirectives()))
                .Collect()
                .Combine(compilation)
                .Select(static (pair, _) =>
                {
                    var (inheritsInfos, compilation) = pair;
                    return DefaultUtf8WriteLiteralFeature.Utf8SupportMap.Create(inheritsInfos, compilation);
                })
                .WithTrackingName("Utf8SupportMap");

            var csharpDocuments = parsedDocuments

                // Add the tag helpers in, but ignore if they've changed or not, only reprocessing the actual document changed
                .Combine(allTagHelpers)
                .WithLambdaComparer((old, @new) => old.Left.Equals(@new.Left))
                .Select(static (pair, cancellationToken) =>
                {
                    var ((projectEngine, filePath, codeDocument), allTagHelpers) = pair;
                    RazorSourceGeneratorEventSource.Log.RewriteTagHelpersStart(filePath);

                    codeDocument = projectEngine.ProcessTagHelpers(codeDocument, allTagHelpers, checkForIdempotency: false, cancellationToken);

                    RazorSourceGeneratorEventSource.Log.RewriteTagHelpersStop(filePath);
                    return (projectEngine, filePath, codeDocument);
                })
                .WithTrackingName("RewrittenTagHelpers")

                // next we do a second parse, along with the helpers, but check for idempotency. If the tag helpers used on the previous parse match, the compiler can skip re-writing them
                .Combine(allTagHelpers)
                .Select(static (pair, cancellationToken) =>
                {
                    var ((projectEngine, filePath, document), allTagHelpers) = pair;
                    RazorSourceGeneratorEventSource.Log.CheckAndRewriteTagHelpersStart(filePath);

                    document = projectEngine.ProcessTagHelpers(document, allTagHelpers, checkForIdempotency: true, cancellationToken);

                    RazorSourceGeneratorEventSource.Log.CheckAndRewriteTagHelpersStop(filePath);
                    return (projectEngine, filePath, document);
                })
                .WithTrackingName("CheckedAndRewrittenTagHelpers")
                .Combine(utf8SupportMap)
                .Select((pair, cancellationToken) =>
                {
                    var ((projectEngine, filePath, document), utf8SupportMap) = pair;

                    RazorSourceGeneratorEventSource.Log.RazorCodeGenerateStart(filePath);
                    document = projectEngine.ProcessRemaining(document, utf8SupportMap, cancellationToken);

                    RazorSourceGeneratorEventSource.Log.RazorCodeGenerateStop(filePath);
                    return (filePath, document);
                })
                .WithTrackingName("GeneratedCode")
                .Select(static (pair, _) =>
                {
                    var (filePath, document) = pair;
                    return (
                        hintName: GetIdentifierFromPath(filePath),
                        codeDocument: document.CodeDocument,
                        csharpDocument: document.CodeDocument.GetRequiredCSharpDocument(declarationDocument: false),
                        declCSharpDocument: document.CodeDocument.GetCSharpDocument(declarationDocument: true));
                })
                .WithLambdaComparer(static (a, b) =>
                {
                    // If either side has diagnostics on either document, force uncached output.
                    if (a.csharpDocument.Diagnostics.Length > 0 || b.csharpDocument.Diagnostics.Length > 0)
                    {
                        return false;
                    }
                    if ((a.declCSharpDocument?.Diagnostics.Length ?? 0) > 0 || (b.declCSharpDocument?.Diagnostics.Length ?? 0) > 0)
                    {
                        return false;
                    }

                    if (!a.csharpDocument.Text.ContentEquals(b.csharpDocument.Text))
                    {
                        return false;
                    }

                    return (a.declCSharpDocument, b.declCSharpDocument) switch
                    {
                        (null, null) => true,
                        (not null, not null) => a.declCSharpDocument.Text.ContentEquals(b.declCSharpDocument.Text),
                        _ => false,
                    };
                })
                .WithTrackingName("CSharpDocuments");

            context.RegisterImplementationSourceOutput(csharpDocuments, static (context, pair) =>
            {
                var (hintName, _, csharpDocument, declCSharpDocument) = pair;

                RazorSourceGeneratorEventSource.Log.AddSyntaxTrees(hintName);
                foreach (var razorDiagnostic in csharpDocument.Diagnostics)
                {
                    var csharpDiagnostic = razorDiagnostic.AsDiagnostic();
                    context.ReportDiagnostic(csharpDiagnostic);
                }

                // Decl diagnostics are surfaced here even though the split decl source itself is emitted
                // via RegisterPreCompilationSourceOutput: the pre-comp output runs before the compilation
                // exists, but diagnostics want to be reported alongside the compilation, so they flow
                // through this implementation-time output instead.
                if (declCSharpDocument is not null)
                {
                    foreach (var razorDiagnostic in declCSharpDocument.Diagnostics)
                    {
                        var csharpDiagnostic = razorDiagnostic.AsDiagnostic();
                        context.ReportDiagnostic(csharpDiagnostic);
                    }
                }

                context.AddSource(hintName, csharpDocument.Text);
            });

            var hostOutputs = csharpDocuments
                .Collect()
                .Combine(allTagHelpers)
                .WithTrackingName("HostOutputs");

#pragma warning disable RSEXPERIMENTAL004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            context.RegisterHostOutput(hostOutputs, (context, pair) =>
#pragma warning restore RSEXPERIMENTAL004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            {
                var (documents, tagHelpers) = pair;

                using var filePathToDocument = new PooledDictionaryBuilder<string, (string, RazorCodeDocument)>();
                using var hintNameToFilePath = new PooledDictionaryBuilder<string, string>();

                foreach (var (hintName, codeDocument, _, declCSharpDocument) in documents)
                {
                    filePathToDocument.Add(codeDocument.Source.FilePath!, (hintName, codeDocument));
                    hintNameToFilePath.Add(hintName, codeDocument.Source.FilePath!);

                    if (declCSharpDocument is not null)
                    {
                        hintNameToFilePath.Add(GetDeclIdentifierFromHintName(hintName), codeDocument.Source.FilePath!);
                    }
                }

                context.AddOutput(nameof(RazorGeneratorResult), new RazorGeneratorResult(tagHelpers, filePathToDocument.ToImmutable(), hintNameToFilePath.ToImmutable()));
            });
        }
    }
}


