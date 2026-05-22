// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class DocumentClassifierPassBase : IntermediateNodePassBase, IRazorDocumentClassifierPass
{
    private ImmutableArray<ICodeTargetExtension> _targetExtensions;

    protected abstract string DocumentKind { get; }

    protected ImmutableArray<ICodeTargetExtension> TargetExtensions
        => _targetExtensions.NullToEmpty();

    protected override void OnInitialized()
    {
        _targetExtensions = Engine.TryGetFeature(out IRazorTargetExtensionFeature? feature)
            ? feature.TargetExtensions.ToImmutable()
            : [];
    }

    protected sealed override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (documentNode.DocumentKind != null)
        {
            return;
        }

        if (!IsMatch(codeDocument, documentNode))
        {
            return;
        }

        documentNode.DocumentKind = DocumentKind;
        documentNode.Target = CreateTarget(codeDocument);
        if (documentNode.Target == null)
        {
            throw new InvalidOperationException($"{nameof(CreateTarget)} must return a non-null {nameof(CodeTarget)}.");
        }

        Rewrite(codeDocument, documentNode);
    }

    private void Rewrite(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        // Rewrite the document from a flat structure to use a sensible default structure,
        // a namespace and class declaration with a single 'razor' method.
        var children = new List<IntermediateNode>(documentNode.Children);
        documentNode.Children.Clear();

        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true
        };

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true
        };

        var method = new MethodDeclarationIntermediateNode
        {
            IsPrimaryMethod = true
        };

        var documentBuilder = IntermediateNodeBuilder.Create(documentNode);

        var namespaceBuilder = IntermediateNodeBuilder.Create(documentBuilder.Current);
        namespaceBuilder.Push(@namespace);

        var classBuilder = IntermediateNodeBuilder.Create(namespaceBuilder.Current);
        classBuilder.Push(@class);

        var methodBuilder = IntermediateNodeBuilder.Create(classBuilder.Current);
        methodBuilder.Push(method);

        var visitor = new Visitor(namespaceBuilder, classBuilder, methodBuilder);

        for (var i = 0; i < children.Count; i++)
        {
            visitor.Visit(children[i]);
        }

        // Note that this is called at the *end* of rewriting so that user code can see the tree
        // and look at its content to make a decision.
        OnDocumentStructureCreated(codeDocument, @namespace, @class, method);
    }

    protected abstract bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

    // virtual to allow replacing the code target wholesale.
    protected virtual CodeTarget CreateTarget(RazorCodeDocument codeDocument)
        => CodeTarget.CreateDefault(codeDocument, builder =>
        {
            builder.TargetExtensions.AddRange(TargetExtensions);
        });

    protected virtual void OnDocumentStructureCreated(
        RazorCodeDocument codeDocument,
        NamespaceDeclarationIntermediateNode @namespace,
        ClassDeclarationIntermediateNode @class,
        MethodDeclarationIntermediateNode @method)
    {
        // Intentionally empty.
    }

    private class Visitor : IntermediateNodeVisitor
    {
        private readonly IntermediateNodeBuilder _namespace;
        private readonly IntermediateNodeBuilder _class;
        private readonly IntermediateNodeBuilder _method;

        public Visitor(IntermediateNodeBuilder @namespace, IntermediateNodeBuilder @class, IntermediateNodeBuilder method)
        {
            _namespace = @namespace;
            _class = @class;
            _method = method;
        }

        public override void VisitUsingDirective(UsingDirectiveIntermediateNode node)
        {
            var children = _namespace.Current.Children;
            var i = children.Count - 1;
            for (; i >= 0; i--)
            {
                var child = children[i];
                if (child is UsingDirectiveIntermediateNode)
                {
                    break;
                }
            }

            _namespace.Insert(i + 1, node);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            if (node is MemberDeclarationIntermediateNode)
            {
                _class.Add(node);
                return;
            }

            _method.Add(node);
        }
    }
}
