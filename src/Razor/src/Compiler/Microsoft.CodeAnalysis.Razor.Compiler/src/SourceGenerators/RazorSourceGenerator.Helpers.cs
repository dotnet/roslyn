// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
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

        // Discovery ownership keys omit generic arity.
        internal static string StripGenericArity(string typeName)
        {
            var baseName = TypeNameHelper.GetNonGenericTypeName(typeName, out _);
            return baseName.Length == typeName.Length ? typeName : baseName.ToString();
        }

        // Derived descriptors share their owner's namespace and identifier; view components use synthetic names.
        internal static string GetOwningTypeName(TagHelperDescriptor descriptor)
        {
            if (descriptor.Metadata is ViewComponentMetadata viewComponent &&
                viewComponent.OriginalTypeNameObject.Name is { } originalName)
            {
                return viewComponent.OriginalTypeNameObject.Namespace is { Length: > 0 } originalNamespace
                    ? originalNamespace + "." + originalName
                    : originalName;
            }

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

        // Reference equality allows the descriptor and dependency projections to compare independently.
        private sealed class SourceDiscoveryResult(TagHelperCollection tagHelpers, ImmutableArray<SourceTypeDependency> dependencies)
        {
            public TagHelperCollection TagHelpers { get; } = tagHelpers;

            public ImmutableArray<SourceTypeDependency> Dependencies { get; } = dependencies;
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

        // Resolve by short name without binding every declaration; descriptor filtering removes collisions.
        private static ImmutableArray<INamedTypeSymbol> ResolveSlowDiscoveryTypes(
            Compilation compilation,
            ImmutableHashSet<string> slowDiscoveryTypeNames,
            CancellationToken cancellationToken)
        {
            if (slowDiscoveryTypeNames.IsEmpty)
            {
                return [];
            }

            var shortNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in slowDiscoveryTypeNames)
            {
                var lastDot = name.LastIndexOf('.');
                shortNames.Add(lastDot >= 0 ? name.Substring(lastDot + 1) : name);
            }

            using var builder = new PooledArrayBuilder<INamedTypeSymbol>();

            foreach (var symbol in compilation.GetSymbolsWithName(shortNames.Contains, SymbolFilter.Type, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (symbol is INamedTypeSymbol typeSymbol && HasPublicContainingTypes(typeSymbol))
                {
                    builder.Add(typeSymbol);
                }
            }

            return builder.ToImmutable();

            static bool HasPublicContainingTypes(INamedTypeSymbol type)
            {
                // Assembly discovery only descends into public containing types.
                for (var containingType = type.ContainingType; containingType is not null; containingType = containingType.ContainingType)
                {
                    if (containingType.DeclaredAccessibility != Accessibility.Public)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // Conservative ownership is safe because selected types are rediscovered against the augmented compilation.
        private static ImmutableHashSet<string> GetSlowDiscoveryTypeNames(
            ImmutableArray<SourceTypeDependency> dependencies,
            ImmutableHashSet<string> fallbackTypeNames)
        {
            if (fallbackTypeNames.IsEmpty || dependencies.IsDefaultOrEmpty)
            {
                return fallbackTypeNames;
            }

            var builder = fallbackTypeNames.ToBuilder();
            foreach (var dependency in dependencies)
            {
                if (dependency.RequiresSlowDiscovery || dependency.SourceBaseTypeNames.Any(fallbackTypeNames.Contains))
                {
                    builder.Add(dependency.TypeName);
                }
            }

            return builder.ToImmutable();
        }

        internal sealed record SourceTypeDependency(string TypeName, ImmutableArray<string> SourceBaseTypeNames, bool RequiresSlowDiscovery)
        {
            public bool Equals(SourceTypeDependency? other)
                => other is not null &&
                   TypeName == other.TypeName &&
                   RequiresSlowDiscovery == other.RequiresSlowDiscovery &&
                   SourceBaseTypeNames.SequenceEqual(other.SourceBaseTypeNames);

            public override int GetHashCode()
                => TypeName.GetHashCode();
        }

        private sealed class SourceTypeDependencyCollector(IAssemblySymbol assembly)
        {
            private readonly ImmutableArray<SourceTypeDependency>.Builder _dependencies = ImmutableArray.CreateBuilder<SourceTypeDependency>();

            public void Visit(INamedTypeSymbol type, bool isNonComponentCandidate)
            {
                if (isNonComponentCandidate)
                {
                    // Non-component producers can depend on attributes, properties, or methods omitted from stubs.
                    _dependencies.Add(new SourceTypeDependency(GetDiscoveryTypeName(type), [], RequiresSlowDiscovery: true));
                    return;
                }

                var isComponentCandidate = ComponentDetectionConventions.IsComponent(type, ComponentsApi.IComponent.MetadataName);
                var requiresSlowDiscovery = false;
                ImmutableArray<string>.Builder? baseNames = null;

                // Metadata types can't reference this compilation's stubs, so the walk stops at the first one.
                for (var current = type; current is not null && SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, assembly); current = current.BaseType)
                {
                    if (current.BaseType is { } baseType)
                    {
                        requiresSlowDiscovery |= ContainsErrorType(baseType);

                        if (SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, assembly))
                        {
                            (baseNames ??= ImmutableArray.CreateBuilder<string>()).Add(GetDiscoveryTypeName(baseType));
                        }
                    }
                }

                if (isComponentCandidate && !requiresSlowDiscovery)
                {
                    requiresSlowDiscovery = HasErrorAttributeArguments(type.GetAttributes()) ||
                        HasErrorTypeParameter(type) ||
                        HasErrorParameter(type);
                }

                if (requiresSlowDiscovery || baseNames is not null)
                {
                    _dependencies.Add(new SourceTypeDependency(
                        GetDiscoveryTypeName(type),
                        baseNames?.ToImmutable() ?? [],
                        requiresSlowDiscovery));
                }
            }

            public ImmutableArray<SourceTypeDependency> ToImmutable() => _dependencies.ToImmutable();

            private static bool HasErrorTypeParameter(INamedTypeSymbol type)
            {
                foreach (var typeParameter in type.TypeParameters)
                {
                    if (HasErrorAttributeArguments(typeParameter.GetAttributes()))
                    {
                        return true;
                    }

                    foreach (var constraintType in typeParameter.ConstraintTypes)
                    {
                        if (ContainsErrorType(constraintType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private static bool HasErrorParameter(INamedTypeSymbol type)
            {
                foreach (var (property, kind) in ComponentTagHelperProducer.GetProperties(type))
                {
                    if (kind != ComponentTagHelperProducer.PropertyKind.Ignored &&
                        (ContainsErrorType(property.Type) || HasErrorAttributeArguments(property.GetAttributes())))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasErrorAttributeArguments(ImmutableArray<AttributeData> attributes)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeClass is IErrorTypeSymbol)
                    {
                        return true;
                    }

                    foreach (var argument in attribute.ConstructorArguments)
                    {
                        if (ContainsErrorConstant(argument))
                        {
                            return true;
                        }
                    }

                    foreach (var (_, argument) in attribute.NamedArguments)
                    {
                        if (ContainsErrorConstant(argument))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private static bool ContainsErrorConstant(TypedConstant constant)
            {
                if (constant.Kind == TypedConstantKind.Error ||
                    (constant.Type is { } type && ContainsErrorType(type)))
                {
                    return true;
                }

                if (constant.Kind == TypedConstantKind.Array && !constant.IsNull)
                {
                    foreach (var element in constant.Values)
                    {
                        if (ContainsErrorConstant(element))
                        {
                            return true;
                        }
                    }
                }
                else if (constant.Kind == TypedConstantKind.Type &&
                    constant.Value is ITypeSymbol valueType && ContainsErrorType(valueType))
                {
                    return true;
                }

                return false;
            }

            private static bool ContainsErrorType(ITypeSymbol type)
            {
                switch (type)
                {
                    case IErrorTypeSymbol:
                        return true;
                    case IArrayTypeSymbol array:
                        return ContainsErrorType(array.ElementType);
                    case IPointerTypeSymbol pointer:
                        return ContainsErrorType(pointer.PointedAtType);
                    case INamedTypeSymbol named:
                        foreach (var typeArgument in named.TypeArguments)
                        {
                            if (ContainsErrorType(typeArgument))
                            {
                                return true;
                            }
                        }

                        return named.ContainingType is { } containingType && ContainsErrorType(containingType);
                    default:
                        return false;
                }
            }
        }

        // Nested-type name collisions are conservatively rediscovered together.
        private static string GetDiscoveryTypeName(INamedTypeSymbol type)
            => type.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
                ? containingNamespace.GetFullName() + "." + type.Name
                : type.Name;

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
