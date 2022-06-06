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
    internal abstract class AbstractEmbeddedLanguageBraceMatcher : IBraceMatcher
    {
        /// <summary>
        /// The kinds of literal tokens that we want to do embedded language classification for.
        /// </summary>
        private readonly HashSet<int> _syntaxTokenKinds = new();

        private readonly EmbeddedLanguageInfo _info;
        private readonly EmbeddedLanguageDetector _detector;

        /// <summary>
        /// Brace matchers that can annotated older APIs not updated to use the [StringSyntax] attribute.
        /// </summary>
        private readonly ImmutableArray<Lazy<IEmbeddedLanguageBraceMatchingService, EmbeddedLanguageMetadata>> _legacyServices;

        /// <summary>
        /// Ordered mapping of a lang ID (like 'Json') to all the brace matchers that can actually highlight that
        /// language. This allows for multiple services to be available.  The first service though that returns results
        /// for a string will 'win' and no other services will contribute.
        /// </summary>
        private readonly Dictionary<string, ArrayBuilder<Lazy<IEmbeddedLanguageBraceMatchingService, EmbeddedLanguageMetadata>>> _identifierToServices = new(StringComparer.OrdinalIgnoreCase);

        protected AbstractEmbeddedLanguageBraceMatcher(
            string languageName,
            EmbeddedLanguageInfo info,
            ISyntaxKinds syntaxKinds,
            IEnumerable<Lazy<IEmbeddedLanguageBraceMatchingService, EmbeddedLanguageMetadata>> allServices)
        {
            // Order the classifiers to respect the [Order] annotations.
            var orderedServices = ExtensionOrderer.Order(allServices).Where(c => c.Metadata.Language == languageName).ToImmutableArray();

            // Grab out the classifiers that handle unannotated literals and APIs.
            _legacyServices = orderedServices.WhereAsArray(c => c.Metadata.SupportsUnannotatedAPIs);

            foreach (var service in orderedServices)
            {
                foreach (var identifier in service.Metadata.Identifiers)
                    _identifierToServices.MultiAdd(identifier, service);
            }

            foreach (var (_, services) in _identifierToServices)
                services.RemoveDuplicates();

            _info = info;
            _detector = new EmbeddedLanguageDetector(info, _identifierToServices.Keys.ToImmutableArray());

            _syntaxTokenKinds.Add(syntaxKinds.CharacterLiteralToken);
            _syntaxTokenKinds.Add(syntaxKinds.StringLiteralToken);
            _syntaxTokenKinds.Add(syntaxKinds.InterpolatedStringTextToken);

            _syntaxTokenKinds.AddIfNotNull(syntaxKinds.SingleLineRawStringLiteralToken);
            _syntaxTokenKinds.AddIfNotNull(syntaxKinds.MultiLineRawStringLiteralToken);
            _syntaxTokenKinds.AddIfNotNull(syntaxKinds.UTF8StringLiteralToken);
            _syntaxTokenKinds.AddIfNotNull(syntaxKinds.UTF8SingleLineRawStringLiteralToken);
            _syntaxTokenKinds.AddIfNotNull(syntaxKinds.UTF8MultiLineRawStringLiteralToken);
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteEmbeddedLanguageBraceMatcherService, BraceMatchingResult?>(
                   document.Project,
                   (service, solutionInfo, cancellationToken) => service.FindBracesAsync(
                       solutionInfo, document.Id, position, options, cancellationToken),
                   cancellationToken).ConfigureAwait(false);

                // if the remote call fails do nothing (error has already been reported)
                if (result.HasValue)
                    return result.Value;

                return null;
            }
            else
            {
                return await FindBracesInCurrentProcessAsync(
                    document, position, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<BraceMatchingResult?> FindBracesInCurrentProcessAsync(
            Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return FindBracesInCurrentProcess(semanticModel, position, options, cancellationToken);
        }

        private BraceMatchingResult? FindBracesInCurrentProcess(
            SemanticModel semanticModel,
            int position,
            BraceMatchingOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IEmbeddedLanguageBraceMatchingService>.GetInstance(out var serviceBuffer);
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
            private readonly ArrayBuilder<IEmbeddedLanguageBraceMatchingService> _serviceBuffer;
            private readonly CancellationToken _cancellationToken;

            public Worker(
                AbstractEmbeddedLanguageBraceMatcher service,
                SemanticModel semanticModel,
                int position,
                BraceMatchingOptions options,
                ArrayBuilder<IEmbeddedLanguageBraceMatchingService> serviceBuffer,
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
                if (token.Span.IntersectsWith(_position) && _service._syntaxTokenKinds.Contains(token.RawKind))
                {
                    _serviceBuffer.Clear();

                    // First, see if this is a string annotated with either a comment or [StringSyntax] attribute. If
                    // so, delegate to the first classifier we have registered for whatever language ID we find.
                    if (_service._detector.IsEmbeddedLanguageToken(token, _semanticModel, _cancellationToken, out var identifier, out _) &&
                        _service._identifierToServices.TryGetValue(identifier, out var services))
                    {
                        foreach (var service in services)
                        {
                            // keep track of what classifiers we've run so we don't call into them multiple times.
                            _serviceBuffer.Add(service.Value);

                            // If this service added values then need to check the other ones.
                            var result = service.Value.FindBraces(_semanticModel, token, _options, _cancellationToken);
                            if (result.HasValue)
                                return result;
                        }
                    }

                    // It wasn't an annotated API.  See if it's some legacy API our historical classifiers have direct
                    // support for (for example, .net APIs prior to Net6).
                    foreach (var legacyService in _service._legacyServices)
                    {
                        // don't bother trying to classify again if we already tried above.
                        if (_serviceBuffer.Contains(legacyService.Value))
                            continue;

                        // If this service added values then need to check the other ones.
                        var result = legacyService.Value.FindBraces(_semanticModel, token, _options, _cancellationToken);
                        if (result.HasValue)
                            return result;
                    }
                }

                return null;
            }
        }
    }
}
