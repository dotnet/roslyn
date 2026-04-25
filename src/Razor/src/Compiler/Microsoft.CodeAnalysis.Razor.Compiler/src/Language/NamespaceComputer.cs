// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class NamespaceComputer
{
    private static ReadOnlySpan<char> PathSeparators => ['/', '\\'];
    private static ReadOnlySpan<char> NamespaceSeparators => ['.'];

    public static bool TryComputeNamespace(
        RazorCodeDocument codeDocument,
        bool fallbackToRootNamespace,
        bool considerImports,
        [NotNullWhen(true)] out string? @namespace,
        out SourceSpan? namespaceSpan)
    {
        var filePath = codeDocument.Source.FilePath;
        var relativePath = codeDocument.Source.RelativePath;

        if (filePath == null || relativePath == null || filePath.Length < relativePath.Length)
        {
            @namespace = null;
            namespaceSpan = null;
            return false;
        }

        // If the document or its imports contains a @namespace directive, we want to use that over the root namespace.
        if (TryGetNamespaceFromDirective(codeDocument, considerImports, out var directiveNamespaceName, out var directiveNamespaceSpan))
        {
            using var builder = new NamespaceBuilder();

            builder.AppendNamespace(directiveNamespaceName);

            var sourceFilePath = filePath.AsSpan();
            var directiveDirectorySpan = NormalizeDirectory(directiveNamespaceSpan.FilePath);

            // We're specifically using OrdinalIgnoreCase here because Razor treats all paths as case-insensitive.
            if (sourceFilePath.Length > directiveDirectorySpan.Length &&
                sourceFilePath.StartsWith(directiveDirectorySpan, StringComparison.OrdinalIgnoreCase))
            {
                // We know that the document containing the namespace directive is in the current document's hierarchy.
                // Compute the actual relative path and use that as the namespace suffix.
                var suffix = sourceFilePath[directiveDirectorySpan.Length..];
                builder.AppendRelativePath(suffix);
            }

            @namespace = builder.ToString();
            namespaceSpan = directiveNamespaceSpan;
            return true;
        }

        if (fallbackToRootNamespace)
        {
            var rootNamespace = codeDocument.CodeGenerationOptions.RootNamespace;

            if (!rootNamespace.IsNullOrEmpty() || codeDocument.FileKind.IsComponent())
            {
                using var builder = new NamespaceBuilder();

                builder.AppendNamespace(rootNamespace);
                builder.AppendRelativePath(relativePath.AsSpan());

                @namespace = builder.ToString();
                namespaceSpan = null;
                return true;
            }
        }

        // There was no valid @namespace directive.
        @namespace = null;
        namespaceSpan = null;
        return false;
    }

    // We want to normalize the path of the file containing the '@namespace' directive to just the containing
    // directory with a trailing separator.
    //
    // Not using Path.GetDirectoryName here because it doesn't meet these requirements, and we want to handle
    // both 'view engine' style paths and absolute paths.
    //
    // We also don't normalize the separators here. We expect that all documents are using a consistent style of path.
    //
    // If we can't normalize the path, we just return null so it will be ignored.
    private static ReadOnlySpan<char> NormalizeDirectory(string path)
    {
        var span = path.AsSpanOrDefault();

        if (span.IsEmpty)
        {
            return default;
        }

        var lastSeparator = span.LastIndexOfAny(PathSeparators);
        if (lastSeparator < 0)
        {
            return default;
        }

        // Includes the separator
        return span[..(lastSeparator + 1)];
    }

    private readonly ref struct NamespaceBuilder
    {
        private readonly PooledObject<StringBuilder> _pooledBuilder;

        public NamespaceBuilder()
        {
            _pooledBuilder = StringBuilderPool.GetPooledObject();
        }

        public void Dispose()
        {
            _pooledBuilder.Dispose();
        }

        public override string ToString()
            => _pooledBuilder.Object.ToString();

        public void AppendNamespace(string? namespaceName)
        {
            var builder = _pooledBuilder.Object;
            var tokenizer = new StringTokenizer(namespaceName, NamespaceSeparators);

            foreach (var token in tokenizer)
            {
                if (token.IsEmpty)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                CSharpIdentifier.AppendSanitized(builder, token);
            }
        }

        public void AppendRelativePath(ReadOnlySpan<char> relativePath)
        {
            var lastSeparatorIndex = relativePath.LastIndexOfAny(PathSeparators);
            if (lastSeparatorIndex < 0)
            {
                return;
            }

            relativePath = relativePath[..lastSeparatorIndex];

            var builder = _pooledBuilder.Object;
            var tokenizer = new StringTokenizer(relativePath, PathSeparators);

            foreach (var token in tokenizer)
            {
                if (token.IsEmpty)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                CSharpIdentifier.AppendSanitized(builder, token);
            }
        }
    }

    private static bool TryGetNamespaceFromDirective(
        RazorCodeDocument codeDocument,
        bool considerImports,
        [NotNullWhen(true)] out string? namespaceName,
        out SourceSpan namespaceSpan)
    {
        // If there are multiple @namespace directives in the hierarchy,
        // we want to pick the closest one to the current document.
        // So, we start with the current document before looking at imports.

        var visitor = new NamespaceDirectiveVisitor();

        if (codeDocument.TryGetSyntaxTree(out var syntaxTree) &&
            visitor.TryGetLastNamespaceDirective(syntaxTree, out namespaceName, out namespaceSpan))
        {
            return true;
        }

        if (considerImports &&
            codeDocument.TryGetImportSyntaxTrees(out var importSyntaxTrees))
        {
            // Be sure to walk the imports in reverse order since the last one is the closest to the document.
            for (var i = importSyntaxTrees.Length - 1; i >= 0; i--)
            {
                var importSyntaxTree = importSyntaxTrees[i];
                if (visitor.TryGetLastNamespaceDirective(importSyntaxTree, out namespaceName, out namespaceSpan))
                {
                    return true;
                }
            }
        }

        namespaceName = null;
        namespaceSpan = default;
        return false;
    }

    private sealed class NamespaceDirectiveVisitor : SyntaxWalker
    {
        private RazorSourceDocument? _source;
        private string? _lastNamespaceName;
        private SourceSpan _lastNamespaceSpan;

        public bool TryGetLastNamespaceDirective(
            RazorSyntaxTree syntaxTree,
            [NotNullWhen(true)] out string? namespaceName,
            out SourceSpan namespaceSpan)
        {
            _source = syntaxTree.Source;
            _lastNamespaceName = null;
            _lastNamespaceSpan = default;

            Visit(syntaxTree.Root);

            if (_lastNamespaceName.IsNullOrEmpty())
            {
                namespaceName = null;
                namespaceSpan = SourceSpan.Undefined;
                return false;
            }

            namespaceName = _lastNamespaceName;
            namespaceSpan = _lastNamespaceSpan;
            return true;
        }

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            Debug.Assert(_source != null);

            if (node.IsDirective(NamespaceDirective.Directive) &&
                node.DirectiveBody.CSharpCode.Children is [_, CSharpSyntaxNode @namespace, ..])
            {
                _lastNamespaceName = @namespace.GetContent();
                _lastNamespaceSpan = @namespace.GetSourceSpan(_source);
            }

            base.VisitRazorDirective(node);
        }
    }
}
