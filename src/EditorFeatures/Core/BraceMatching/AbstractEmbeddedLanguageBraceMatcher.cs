// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    // Note: this type could be concrete, but we cannot export IBraceMatcher's for multiple
    // languages at once.  So all logic is contained here.  The derived types only exist for
    // exporting purposes.
    internal abstract class AbstractEmbeddedLanguageBraceMatcher :
        AbstractEmbeddedLanguageFeatureService<IEmbeddedLanguageBraceMatcher>, IBraceMatcher
    {
        protected AbstractEmbeddedLanguageBraceMatcher(
            string languageName,
            EmbeddedLanguageInfo info,
            ISyntaxKinds syntaxKinds,
            IEnumerable<Lazy<IEmbeddedLanguageBraceMatcher, EmbeddedLanguageMetadata>> allServices)
            : base(languageName, info, syntaxKinds, allServices)
        {
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return FindBraces(semanticModel, position, options, cancellationToken);
        }

        private BraceMatchingResult? FindBraces(
            SemanticModel semanticModel,
            int position,
            BraceMatchingOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IEmbeddedLanguageBraceMatcher>.GetInstance(out var serviceBuffer);
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var worker = new Worker(this, semanticModel, position, options, serviceBuffer, cancellationToken);
            return worker.Recurse(root);
        }

        private ref struct Worker
        {
            private readonly AbstractEmbeddedLanguageBraceMatcher _service;
            private readonly SemanticModel _semanticModel;
            private readonly int _position;
            private readonly BraceMatchingOptions _options;
            private readonly ArrayBuilder<IEmbeddedLanguageBraceMatcher> _serviceBuffer;
            private readonly CancellationToken _cancellationToken;

            public Worker(
                AbstractEmbeddedLanguageBraceMatcher service,
                SemanticModel semanticModel,
                int position,
                BraceMatchingOptions options,
                ArrayBuilder<IEmbeddedLanguageBraceMatcher> serviceBuffer,
                CancellationToken cancellationToken)
            {
                _service = service;
                _semanticModel = semanticModel;
                _position = position;
                _options = options;
                _serviceBuffer = serviceBuffer;
                _cancellationToken = cancellationToken;
            }

            public BraceMatchingResult? Recurse(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (node.Span.IntersectsWith(_position))
                {
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                        {
                            var value = Recurse(child.AsNode()!);
                            if (value.HasValue)
                                return value;
                        }
                        else
                        {
                            var value = ProcessToken(child.AsToken());
                            if (value.HasValue)
                                return value;
                        }
                    }
                }

                return null;
            }

            private BraceMatchingResult? ProcessToken(SyntaxToken token)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                return ClassifyToken(token);
            }

            private BraceMatchingResult? ClassifyToken(SyntaxToken token)
            {
                if (token.Span.IntersectsWith(_position) && _service.SyntaxTokenKinds.Contains(token.RawKind))
                {
                    _serviceBuffer.Clear();

                    // First, see if this is a string annotated with either a comment or [StringSyntax] attribute. If
                    // so, delegate to the first classifier we have registered for whatever language ID we find.
                    if (_service.Detector.IsEmbeddedLanguageToken(token, _semanticModel, _cancellationToken, out var identifier, out _) &&
                        _service.IdentifierToServices.TryGetValue(identifier, out var services))
                    {
                        foreach (var service in services)
                        {
                            // keep track of what classifiers we've run so we don't call into them multiple times.
                            _serviceBuffer.Add(service.Value);

                            // If this service added values then need to check the other ones.
                            var result = service.Value.FindBraces(_semanticModel, token, _position, _options, _cancellationToken);
                            if (result.HasValue)
                                return result;
                        }
                    }

                    // It wasn't an annotated API.  See if it's some legacy API our historical classifiers have direct
                    // support for (for example, .net APIs prior to Net6).
                    foreach (var legacyService in _service.LegacyServices)
                    {
                        // don't bother trying to classify again if we already tried above.
                        if (_serviceBuffer.Contains(legacyService.Value))
                            continue;

                        // If this service added values then need to check the other ones.
                        var result = legacyService.Value.FindBraces(_semanticModel, token, _position, _options, _cancellationToken);
                        if (result.HasValue)
                            return result;
                    }
                }

                return null;
            }
        }
    }
}
