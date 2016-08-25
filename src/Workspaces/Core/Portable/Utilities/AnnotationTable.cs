﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
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
    internal class AnnotationTable<TAnnotation> where TAnnotation : class
    {
        private int _globalId;

        private readonly Dictionary<TAnnotation, SyntaxAnnotation> _realAnnotationMap = new Dictionary<TAnnotation, SyntaxAnnotation>();
        private readonly Dictionary<string, TAnnotation> _annotationMap = new Dictionary<string, TAnnotation>();

        private readonly string _annotationKind;

        public AnnotationTable(string annotationKind)
        {
            _annotationKind = annotationKind;
        }

        private IEnumerable<SyntaxAnnotation> GetOrCreateRealAnnotations(TAnnotation[] annotations)
        {
            foreach (var annotation in annotations)
            {
                yield return this.GetOrCreateRealAnnotation(annotation);
            }
        }

        private SyntaxAnnotation GetOrCreateRealAnnotation(TAnnotation annotation)
        {
            SyntaxAnnotation realAnnotation;
            if (!_realAnnotationMap.TryGetValue(annotation, out realAnnotation))
            {
                var id = Interlocked.Increment(ref _globalId);
                var idString = id.ToString();

                realAnnotation = new SyntaxAnnotation(_annotationKind, idString);
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

        private SyntaxAnnotation GetRealAnnotation(TAnnotation annotation)
        {
            SyntaxAnnotation realAnnotation;
            _realAnnotationMap.TryGetValue(annotation, out realAnnotation);
            return realAnnotation;
        }

        public TSyntaxNode WithAdditionalAnnotations<TSyntaxNode>(TSyntaxNode node, params TAnnotation[] annotations) where TSyntaxNode : SyntaxNode
        {
            return node.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());
        }

        public SyntaxToken WithAdditionalAnnotations(SyntaxToken token, params TAnnotation[] annotations)
        {
            return token.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());
        }

        public SyntaxTrivia WithAdditionalAnnotations(SyntaxTrivia trivia, params TAnnotation[] annotations)
        {
            return trivia.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());
        }

        public SyntaxNodeOrToken WithAdditionalAnnotations(SyntaxNodeOrToken nodeOrToken, params TAnnotation[] annotations)
        {
            return nodeOrToken.WithAdditionalAnnotations(this.GetOrCreateRealAnnotations(annotations).ToArray());
        }

        public TSyntaxNode WithoutAnnotations<TSyntaxNode>(TSyntaxNode node, params TAnnotation[] annotations) where TSyntaxNode : SyntaxNode
        {
            return node.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());
        }

        public SyntaxToken WithoutAnnotations(SyntaxToken token, params TAnnotation[] annotations)
        {
            return token.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());
        }

        public SyntaxTrivia WithoutAnnotations(SyntaxTrivia trivia, params TAnnotation[] annotations)
        {
            return trivia.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());
        }

        public SyntaxNodeOrToken WithoutAnnotations(SyntaxNodeOrToken nodeOrToken, params TAnnotation[] annotations)
        {
            return nodeOrToken.WithoutAnnotations(GetRealAnnotations(annotations).ToArray());
        }

        private IEnumerable<TAnnotation> GetAnnotations(IEnumerable<SyntaxAnnotation> realAnnotations)
        {
            foreach (var ra in realAnnotations)
            {
                TAnnotation annotation;
                if (_annotationMap.TryGetValue(ra.Data, out annotation))
                {
                    yield return annotation;
                }
            }
        }

        public IEnumerable<TAnnotation> GetAnnotations(SyntaxNode node)
        {
            return GetAnnotations(node.GetAnnotations(_annotationKind));
        }

        public IEnumerable<TAnnotation> GetAnnotations(SyntaxToken token)
        {
            return GetAnnotations(token.GetAnnotations(_annotationKind));
        }

        public IEnumerable<TAnnotation> GetAnnotations(SyntaxTrivia trivia)
        {
            return GetAnnotations(trivia.GetAnnotations(_annotationKind));
        }

        public IEnumerable<TAnnotation> GetAnnotations(SyntaxNodeOrToken nodeOrToken)
        {
            return GetAnnotations(nodeOrToken.GetAnnotations(_annotationKind));
        }

        public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(node).OfType<TSpecificAnnotation>();
        }

        public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxToken token) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(token).OfType<TSpecificAnnotation>();
        }

        public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxTrivia trivia) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(trivia).OfType<TSpecificAnnotation>();
        }

        public IEnumerable<TSpecificAnnotation> GetAnnotations<TSpecificAnnotation>(SyntaxNodeOrToken nodeOrToken) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(nodeOrToken).OfType<TSpecificAnnotation>();
        }

        public bool HasAnnotations(SyntaxNode node)
        {
            return node.HasAnnotations(_annotationKind);
        }

        public bool HasAnnotations(SyntaxToken token)
        {
            return token.HasAnnotations(_annotationKind);
        }

        public bool HasAnnotations(SyntaxTrivia trivia)
        {
            return trivia.HasAnnotations(_annotationKind);
        }

        public bool HasAnnotations(SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.HasAnnotations(_annotationKind);
        }

        public bool HasAnnotations<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(node).OfType<TSpecificAnnotation>().Any();
        }

        public bool HasAnnotations<TSpecificAnnotation>(SyntaxToken token) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(token).OfType<TSpecificAnnotation>().Any();
        }

        public bool HasAnnotations<TSpecificAnnotation>(SyntaxTrivia trivia) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(trivia).OfType<TSpecificAnnotation>().Any();
        }

        public bool HasAnnotations<TSpecificAnnotation>(SyntaxNodeOrToken nodeOrToken) where TSpecificAnnotation : TAnnotation
        {
            return this.GetAnnotations(nodeOrToken).OfType<TSpecificAnnotation>().Any();
        }

        public bool HasAnnotation(SyntaxNode node, TAnnotation annotation)
        {
            return node.HasAnnotation(this.GetRealAnnotation(annotation));
        }

        public bool HasAnnotation(SyntaxToken token, TAnnotation annotation)
        {
            return token.HasAnnotation(this.GetRealAnnotation(annotation));
        }

        public bool HasAnnotation(SyntaxTrivia trivia, TAnnotation annotation)
        {
            return trivia.HasAnnotation(this.GetRealAnnotation(annotation));
        }

        public bool HasAnnotation(SyntaxNodeOrToken nodeOrToken, TAnnotation annotation)
        {
            return nodeOrToken.HasAnnotation(this.GetRealAnnotation(annotation));
        }

        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(SyntaxNode node)
        {
            return node.GetAnnotatedNodesAndTokens(_annotationKind);
        }

        public IEnumerable<SyntaxNode> GetAnnotatedNodes(SyntaxNode node)
        {
            return node.GetAnnotatedNodesAndTokens(_annotationKind).Where(nt => nt.IsNode).Select(nt => nt.AsNode());
        }

        public IEnumerable<SyntaxToken> GetAnnotatedTokens(SyntaxNode node)
        {
            return node.GetAnnotatedNodesAndTokens(_annotationKind).Where(nt => nt.IsToken).Select(nt => nt.AsToken());
        }

        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(SyntaxNode node)
        {
            return node.GetAnnotatedTrivia(_annotationKind);
        }

        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        {
            return node.GetAnnotatedNodesAndTokens(_annotationKind).Where(nt => this.HasAnnotations<TSpecificAnnotation>(nt));
        }

        public IEnumerable<SyntaxNode> GetAnnotatedNodes<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        {
            return node.GetAnnotatedNodesAndTokens(_annotationKind).Where(nt => nt.IsNode && this.HasAnnotations<TSpecificAnnotation>(nt)).Select(nt => nt.AsNode());
        }

        public IEnumerable<SyntaxToken> GetAnnotatedTokens<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        {
            return node.GetAnnotatedNodesAndTokens(_annotationKind).Where(nt => nt.IsToken && this.HasAnnotations<TSpecificAnnotation>(nt)).Select(nt => nt.AsToken());
        }

        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia<TSpecificAnnotation>(SyntaxNode node) where TSpecificAnnotation : TAnnotation
        {
            return node.GetAnnotatedTrivia(_annotationKind).Where(tr => this.HasAnnotations<TSpecificAnnotation>(tr));
        }
    }
}
