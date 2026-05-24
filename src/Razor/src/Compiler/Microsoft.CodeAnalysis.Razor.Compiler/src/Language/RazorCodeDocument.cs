// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorCodeDocument
{
    public RazorSourceDocument Source { get; }
    public ImmutableArray<RazorSourceDocument> Imports { get; }
    public RazorParserOptions ParserOptions { get; }
    public RazorCodeGenerationOptions CodeGenerationOptions { get; }

    public RazorFileKind FileKind => ParserOptions.FileKind;

    private readonly TagHelperCollection? _tagHelpers;
    private readonly TagHelperCollection? _referencedTagHelpers;
    // The canonical syntax tree produced by parsing and syntax-tree passes, before tag helper rewriting.
    // Established by DefaultRazorTagHelperContextDiscoveryPhase (which reads it from _tagHelperRewrittenSyntaxTree
    // on the first run). Once set, this field is stable throughout the rest of the pipeline.
    private readonly RazorSyntaxTree? _syntaxTree;
    // The working syntax tree: initially set by DefaultRazorParsingPhase (same value as _syntaxTree),
    // updated by DefaultRazorSyntaxTreePhase, and finally replaced with the tag-helper-rewritten tree
    // by DefaultRazorTagHelperRewritePhase.
    private readonly RazorSyntaxTree? _tagHelperRewrittenSyntaxTree;
    private readonly ImmutableArray<RazorSyntaxTree> _importSyntaxTrees;
    private readonly TagHelperDocumentContext? _tagHelperContext;
    private readonly DocumentIntermediateNode? _documentNode;
    private readonly RazorCSharpDocument? _csharpDocument;
    private readonly ImmutableArray<DirectiveTagHelperContribution> _directiveTagHelperContributions;

    private RazorCodeDocument(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions parserOptions,
        RazorCodeGenerationOptions codeGenerationOptions,
        TagHelperCollection? tagHelpers,
        TagHelperCollection? referencedTagHelpers,
        RazorSyntaxTree? syntaxTree,
        RazorSyntaxTree? tagHelperRewrittenSyntaxTree,
        ImmutableArray<RazorSyntaxTree> importSyntaxTrees,
        TagHelperDocumentContext? tagHelperContext,
        DocumentIntermediateNode? documentNode,
        RazorCSharpDocument? csharpDocument,
        ImmutableArray<DirectiveTagHelperContribution> directiveTagHelperContributions)
    {
        Source = source;
        Imports = imports.NullToEmpty();

        ParserOptions = parserOptions;
        CodeGenerationOptions = codeGenerationOptions;

        _tagHelpers = tagHelpers;
        _referencedTagHelpers = referencedTagHelpers;
        _syntaxTree = syntaxTree;
        _tagHelperRewrittenSyntaxTree = tagHelperRewrittenSyntaxTree;
        _importSyntaxTrees = importSyntaxTrees;
        _tagHelperContext = tagHelperContext;
        _documentNode = documentNode;
        _csharpDocument = csharpDocument;
        _directiveTagHelperContributions = directiveTagHelperContributions.NullToEmpty();
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        RazorParserOptions? parserOptions = null,
        RazorCodeGenerationOptions? codeGenerationOptions = null)
        => Create(source, imports: [], parserOptions, codeGenerationOptions);

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions? parserOptions = null,
        RazorCodeGenerationOptions? codeGenerationOptions = null)
    {
        ArgHelper.ThrowIfNull(source);

        return new RazorCodeDocument(
            source,
            imports,
            parserOptions ?? RazorParserOptions.Default,
            codeGenerationOptions ?? RazorCodeGenerationOptions.Default,
            tagHelpers: null,
            referencedTagHelpers: null,
            syntaxTree: null,
            tagHelperRewrittenSyntaxTree: null,
            importSyntaxTrees: default,
            tagHelperContext: null,
            documentNode: null,
            csharpDocument: null,
            directiveTagHelperContributions: default);
    }

    internal bool TryGetTagHelpers([NotNullWhen(true)] out TagHelperCollection? result)
    {
        result = _tagHelpers;
        return result is not null;
    }

    internal TagHelperCollection? GetTagHelpers()
        => _tagHelpers;

    internal TagHelperCollection GetRequiredTagHelpers()
        => _tagHelpers.AssumeNotNull();

    internal RazorCodeDocument WithTagHelpers(TagHelperCollection? value)
    {
        if (Equals(value, _tagHelpers))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, value, _referencedTagHelpers, _syntaxTree, _tagHelperRewrittenSyntaxTree, _importSyntaxTrees, _tagHelperContext, _documentNode, _csharpDocument, _directiveTagHelperContributions);
    }

    internal bool TryGetReferencedTagHelpers([NotNullWhen(true)] out TagHelperCollection? result)
    {
        result = _referencedTagHelpers;
        return result is not null;
    }

    internal TagHelperCollection? GetReferencedTagHelpers()
        => _referencedTagHelpers;

    internal TagHelperCollection GetRequiredReferencedTagHelpers()
        => _referencedTagHelpers.AssumeNotNull();

    internal RazorCodeDocument WithReferencedTagHelpers(TagHelperCollection value)
    {
        if (Equals(value, _referencedTagHelpers))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, value, _syntaxTree, _tagHelperRewrittenSyntaxTree, _importSyntaxTrees, _tagHelperContext, _documentNode, _csharpDocument, _directiveTagHelperContributions);
    }

    internal bool TryGetSyntaxTree([NotNullWhen(true)] out RazorSyntaxTree? result)
    {
        result = _syntaxTree;
        return result is not null;
    }

    internal RazorSyntaxTree? GetSyntaxTree()
        => _syntaxTree;

    internal RazorSyntaxTree GetRequiredSyntaxTree()
        => _syntaxTree.AssumeNotNull();

    internal RazorCodeDocument WithSyntaxTree(RazorSyntaxTree value)
    {
        Debug.Assert(value is not null);
        if (ReferenceEquals(value, _syntaxTree))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, _referencedTagHelpers, value, _tagHelperRewrittenSyntaxTree, _importSyntaxTrees, _tagHelperContext, _documentNode, _csharpDocument, _directiveTagHelperContributions);
    }

    internal bool TryGetTagHelperRewrittenSyntaxTree([NotNullWhen(true)] out RazorSyntaxTree? result)
    {
        result = _tagHelperRewrittenSyntaxTree;
        return result is not null;
    }

    internal RazorSyntaxTree? GetTagHelperRewrittenSyntaxTree()
        => _tagHelperRewrittenSyntaxTree;

    internal RazorSyntaxTree GetRequiredTagHelperRewrittenSyntaxTree()
        => _tagHelperRewrittenSyntaxTree.AssumeNotNull();

    internal RazorCodeDocument WithTagHelperRewrittenSyntaxTree(RazorSyntaxTree value)
    {
        Debug.Assert(value is not null);
        if (ReferenceEquals(value, _tagHelperRewrittenSyntaxTree))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, _referencedTagHelpers, _syntaxTree, value, _importSyntaxTrees, _tagHelperContext, _documentNode, _csharpDocument, _directiveTagHelperContributions);
    }

    internal bool TryGetImportSyntaxTrees(out ImmutableArray<RazorSyntaxTree> result)
    {
        if (!_importSyntaxTrees.IsDefault)
        {
            result = _importSyntaxTrees;
            return true;
        }

        result = default;
        return false;
    }

    internal ImmutableArray<RazorSyntaxTree> GetImportSyntaxTrees()
        => _importSyntaxTrees.IsDefault ? [] : _importSyntaxTrees;

    internal RazorCodeDocument WithImportSyntaxTrees(ImmutableArray<RazorSyntaxTree> value)
    {
        Debug.Assert(!value.IsDefault);
        Debug.Assert(value.IsEmpty || value.All(static t => t is not null));

        if (ReferenceEquals(ImmutableCollectionsMarshal.AsArray(value), ImmutableCollectionsMarshal.AsArray(_importSyntaxTrees)))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, _referencedTagHelpers, _syntaxTree, _tagHelperRewrittenSyntaxTree, value, _tagHelperContext, _documentNode, _csharpDocument, _directiveTagHelperContributions);
    }

    internal bool TryGetTagHelperContext([NotNullWhen(true)] out TagHelperDocumentContext? result)
    {
        result = _tagHelperContext;
        return result is not null;
    }

    internal TagHelperDocumentContext? GetTagHelperContext()
        => _tagHelperContext;

    internal TagHelperDocumentContext GetRequiredTagHelperContext()
        => _tagHelperContext.AssumeNotNull();

    internal RazorCodeDocument WithTagHelperContext(TagHelperDocumentContext value)
    {
        Debug.Assert(value is not null);

        if (ReferenceEquals(value, _tagHelperContext))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, _referencedTagHelpers, _syntaxTree, _tagHelperRewrittenSyntaxTree, _importSyntaxTrees, value, _documentNode, _csharpDocument, _directiveTagHelperContributions);
    }

    internal bool TryGetDocumentNode([NotNullWhen(true)] out DocumentIntermediateNode? result)
    {
        result = _documentNode;
        return result is not null;
    }

    internal DocumentIntermediateNode? GetDocumentNode()
        => _documentNode;

    internal DocumentIntermediateNode GetRequiredDocumentNode()
        => _documentNode.AssumeNotNull();

    internal RazorCodeDocument WithDocumentNode(DocumentIntermediateNode value)
    {
        Debug.Assert(value is not null);
        if (ReferenceEquals(value, _documentNode))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, _referencedTagHelpers, _syntaxTree, _tagHelperRewrittenSyntaxTree, _importSyntaxTrees, _tagHelperContext, value, _csharpDocument, _directiveTagHelperContributions);
    }

    internal bool TryGetCSharpDocument([NotNullWhen(true)] out RazorCSharpDocument? result)
    {
        result = _csharpDocument;
        return result is not null;
    }

    internal RazorCSharpDocument? GetCSharpDocument()
        => _csharpDocument;

    internal RazorCSharpDocument GetRequiredCSharpDocument()
        => _csharpDocument.AssumeNotNull();

    internal RazorCodeDocument WithCSharpDocument(RazorCSharpDocument value)
    {
        Debug.Assert(value is not null);
        if (ReferenceEquals(value, _csharpDocument))
        {
            return this;
        }
        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, _referencedTagHelpers, _syntaxTree, _tagHelperRewrittenSyntaxTree, _importSyntaxTrees, _tagHelperContext, _documentNode, value, _directiveTagHelperContributions);
    }

    internal ImmutableArray<DirectiveTagHelperContribution> GetDirectiveTagHelperContributions()
        => _directiveTagHelperContributions;

    internal RazorCodeDocument WithDirectiveTagHelperContributions(ImmutableArray<DirectiveTagHelperContribution> value)
    {
        if (value == _directiveTagHelperContributions)
        {
            return this;
        }

        return new RazorCodeDocument(Source, Imports, ParserOptions, CodeGenerationOptions, _tagHelpers, _referencedTagHelpers, _syntaxTree, _tagHelperRewrittenSyntaxTree, _importSyntaxTrees, _tagHelperContext, _documentNode, _csharpDocument, value);
    }

    // In general documents will have a relative path (relative to the project root).
    // We can only really compute a nice namespace when we know a relative path.
    //
    // However all kinds of thing are possible in tools. We shouldn't barf here if the document isn't set up correctly.
    internal bool TryGetNamespace(
        bool fallbackToRootNamespace,
        bool considerImports,
        [NotNullWhen(true)] out string? @namespace,
        out SourceSpan? namespaceSpan)
    {
        if (NamespaceComputer.TryComputeNamespace(this, fallbackToRootNamespace, considerImports, out @namespace, out namespaceSpan))
        {
            VerifyNamespace(this, fallbackToRootNamespace, considerImports, @namespace);
            return true;
        }

        return false;

        [Conditional("DEBUG")]
        static void VerifyNamespace(RazorCodeDocument codeDocument, bool fallbackToRootNamespace, bool considerImports, string? @namespace)
        {
            // In debug mode, even if we're cached, lets take the hit to run this again and make sure the cached value is correct.
            // This is to help us find issues with caching logic during development.
            var validateResult = NamespaceComputer.TryComputeNamespace(
                codeDocument, fallbackToRootNamespace, considerImports, out var validateNamespace, out _);

            Debug.Assert(validateResult, "We couldn't compute the namespace, but have a cached value, so something has gone wrong");
            Debug.Assert(validateNamespace == @namespace, $"We cached a namespace of {@namespace} but calculated that it should be {validateNamespace}");
        }
    }
}
