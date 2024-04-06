// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

/// <summary>
/// An AnnotationTable helps you attach your own annotation types/instances to syntax.  
/// 
/// It maintains a map between your instances and actual SyntaxAnnotation's used to annotate the nodes
/// and offers an API that matches the true annotation API on SyntaxNode.
/// 
/// The table controls the lifetime of when you can find and retrieve your annotations. You won't be able to 
/// find your annotations via HasAnnotations/GetAnnotations unless you use the same annotation table for these operations
/// that you used for the WithAdditionalAnnotations operation.  
/// 
/// Your custom annotations are not serialized with the syntax tree, so they won't move across boundaries unless the 
/// same AnnotationTable is available on both ends.
/// 
/// also, note that this table is not thread safe.
/// </summary>
internal class AnnotationTable<TAnnotation>(string annotationKind) where TAnnotation : class
{
    private int _globalId;

    private readonly Dictionary<TAnnotation, SyntaxAnnotation> _realAnnotationMap = [];
    private readonly Dictionary<string, TAnnotation> _annotationMap = [];

    private IEnumerable<SyntaxAnnotation> GetOrCreateRealAnnotations(TAnnotation[] annotations)
    {
        foreach (var annotation in annotations)
        {
            yield return this.GetOrCreateRealAnnotation(annotation);
        }
    }

    private SyntaxAnnotation GetOrCreateRealAnnotation(TAnnotation annotation)
    {
        if (!_realAnnotationMap.TryGetValue(annotation, out var realAnnotation))
        {
            var id = Interlocked.Increment(ref _globalId);
            var idString = id.ToString();

            realAnnotation = new SyntaxAnnotation(annotationKind, idString);
            _annotationMap.Add(idString, annotation);
            _realAnnotationMap.Add(annotation, realAnnotation);
        }

        return realAnnotation;
    }

    private IEnumerable<SyntaxAnnotation> GetRealAnnotations(TAnnotation[] annotations)
    {
        foreach (var annotation in annotations)
        {
            var realAnnotation = this.GetRealAnnotation(annotation);
            if (realAnnotation != null)
            {
                yield return realAnnotation;
            }
        }
    }

    private SyntaxAnnotation? GetRealAnnotation(TAnnotation annotation)
    {
        _realAnnotationMap.TryGetValue(annotation, out var realAnnotation);
        return realAnnotation;
    }

    public TSyntaxNode WithAdditionalAnnotations<TSyntaxNode>(TSyntaxNode node, params TAnnotation[] annotations) where TSyntaxNode : SyntaxNode
        => node.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());

    public SyntaxToken WithAdditionalAnnotations(SyntaxToken token, params TAnnotation[] annotations)
        => token.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());

    public SyntaxTrivia WithAdditionalAnnotations(SyntaxTrivia trivia, params TAnnotation[] annotations)
        => trivia.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());

    public SyntaxNodeOrToken WithAdditionalAnnotations(SyntaxNodeOrToken nodeOrToken, params TAnnotation[] annotations)
        => nodeOrToken.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());

    public TSyntaxNode WithoutAnnotations<TSyntaxNode>(TSyntaxNode node, params TAnnotation[] annotations) where TSyntaxNode : SyntaxNode
        => node.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());

    public SyntaxToken WithoutAnnotations(SyntaxToken token, params TAnnotation[] annotations)
        => token.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());

    public SyntaxTrivia WithoutAnnotations(SyntaxTrivia trivia, params TAnnotation[] annotations)
        => trivia.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());

    public SyntaxNodeOrToken WithoutAnnotations(SyntaxNodeOrToken nodeOrToken, params TAnnotation[] annotations)
        => nodeOrToken.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());

    private IEnumerable<TAnnotation> GetAnnotations(IEnumerable<SyntaxAnnotation> realAnnotations)
    {
        foreach (var ra in realAnnotations)
        {
            Contract.ThrowIfNull(ra.Data);
            if (_annotationMap.TryGetValue(ra.Data, out var annotation))
            {
                yield return annotation;
            }
        }
    }

    public IEnumerable<TAnnotation> GetAnnotations(SyntaxNode node)
        => GetAnnotations(node.GetAnnotations(annotationKind));

    public IEnumerable<TAnnotation> GetAnnotations(SyntaxToken token)
        => GetAnnotations(token.GetAnnotations(annotationKind));

    public IEnumerable<TAnnotation> GetAnnotations(SyntaxTrivia trivia)
        => GetAnnotations(trivia.GetAnnotations(annotationKind));

    public IEnumerable<TAnnotation> GetAnnotations(SyntaxNodeOrToken nodeOrToken)
        => GetAnnotations(nodeOrToken.GetAnnotations(annotationKind));

    public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(node).OfType<TSpecificAnnotation>();

    public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxToken token) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(token).OfType<TSpecificAnnotation>();

    public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxTrivia trivia) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(trivia).OfType<TSpecificAnnotation>();

    public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxNodeOrToken nodeOrToken) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(nodeOrToken).OfType<TSpecificAnnotation>();

    public bool HasAnnotations(SyntaxNode node)
        => node.HasAnnotations(annotationKind);

    public bool HasAnnotations(SyntaxToken token)
        => token.HasAnnotations(annotationKind);

    public bool HasAnnotations(SyntaxTrivia trivia)
        => trivia.HasAnnotations(annotationKind);

    public bool HasAnnotations(SyntaxNodeOrToken nodeOrToken)
        => nodeOrToken.HasAnnotations(annotationKind);

    public bool HasAnnotations<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(node).OfType<TSpecificAnnotation>().Any();

    public bool HasAnnotations<TSpecificAnnotation>(SyntaxToken token) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(token).OfType<TSpecificAnnotation>().Any();

    public bool HasAnnotations<TSpecificAnnotation>(SyntaxTrivia trivia) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(trivia).OfType<TSpecificAnnotation>().Any();

    public bool HasAnnotations<TSpecificAnnotation>(SyntaxNodeOrToken nodeOrToken) where TSpecificAnnotation : TAnnotation
        => this.GetAnnotations(nodeOrToken).OfType<TSpecificAnnotation>().Any();

    public bool HasAnnotation(SyntaxNode node, TAnnotation annotation)
        => node.HasAnnotation(this.GetRealAnnotation(annotation));

    public bool HasAnnotation(SyntaxToken token, TAnnotation annotation)
        => token.HasAnnotation(this.GetRealAnnotation(annotation));

    public bool HasAnnotation(SyntaxTrivia trivia, TAnnotation annotation)
        => trivia.HasAnnotation(this.GetRealAnnotation(annotation));

    public bool HasAnnotation(SyntaxNodeOrToken nodeOrToken, TAnnotation annotation)
        => nodeOrToken.HasAnnotation(this.GetRealAnnotation(annotation));

    public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(SyntaxNode node)
        => node.GetAnnotatedNodesAndTokens(annotationKind);

    public IEnumerable<SyntaxNode> GetAnnotatedNodes(SyntaxNode node)
        => node.GetAnnotatedNodesAndTokens(annotationKind).Where(nt => nt.IsNode).Select(nt => nt.AsNode()!);

    public IEnumerable<SyntaxToken> GetAnnotatedTokens(SyntaxNode node)
        => node.GetAnnotatedNodesAndTokens(annotationKind).Where(nt => nt.IsToken).Select(nt => nt.AsToken());

    public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(SyntaxNode node)
        => node.GetAnnotatedTrivia(annotationKind);

    public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        => node.GetAnnotatedNodesAndTokens(annotationKind).Where(this.HasAnnotations<TSpecificAnnotation>);

    public IEnumerable<SyntaxNode> GetAnnotatedNodes<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        => node.GetAnnotatedNodesAndTokens(annotationKind).Where(nt => nt.IsNode && this.HasAnnotations<TSpecificAnnotation>(nt)).Select(nt => nt.AsNode()!);

    public IEnumerable<SyntaxToken> GetAnnotatedTokens<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        => node.GetAnnotatedNodesAndTokens(annotationKind).Where(nt => nt.IsToken && this.HasAnnotations<TSpecificAnnotation>(nt)).Select(nt => nt.AsToken());

    public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        => node.GetAnnotatedTrivia(annotationKind).Where(this.HasAnnotations<TSpecificAnnotation>);
}
