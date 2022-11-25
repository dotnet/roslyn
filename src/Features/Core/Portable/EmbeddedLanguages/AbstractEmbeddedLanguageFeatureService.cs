// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    internal abstract class AbstractEmbeddedLanguageFeatureService<TService>
        where TService : IEmbeddedLanguageFeatureService
    {
        /// <summary>
        /// The kinds of literal tokens that we want to do embedded language work for.
        /// </summary>
        protected readonly HashSet<int> SyntaxTokenKinds = new();

        /// <summary>
        /// Services that can annotated older APIs not updated to use the [StringSyntax] attribute.
        /// </summary>
        private readonly ImmutableArray<Lazy<TService, EmbeddedLanguageMetadata>> _legacyServices;

        /// <summary>
        /// Ordered mapping of a lang ID (like 'Json') to all the services for that language. This allows for multiple
        /// classifiers to be available.  The first service though that returns results for a string will 'win' and no
        /// other services will contribute.
        /// </summary>
        private readonly ImmutableDictionary<string, ImmutableArray<Lazy<TService, EmbeddedLanguageMetadata>>> _identifierToServices;

        /// <summary>
        /// Information about the embedded language.
        /// </summary>
        protected readonly EmbeddedLanguageInfo Info;

        /// <summary>
        /// Helper to look at string literals and determine what language they are annotated to take.
        /// </summary>
        private readonly EmbeddedLanguageDetector _detector;

        protected AbstractEmbeddedLanguageFeatureService(
            string languageName,
            EmbeddedLanguageInfo info,
            ISyntaxKinds syntaxKinds,
            IEnumerable<Lazy<TService, EmbeddedLanguageMetadata>> allServices)
        {
            // Order the classifiers to respect the [Order] annotations.
            var orderedClassifiers = ExtensionOrderer.Order(allServices).Where(c => c.Metadata.Languages.Contains(languageName)).ToImmutableArray();

            // Grab out the services that handle unannotated literals and APIs.
            _legacyServices = orderedClassifiers.WhereAsArray(c => c.Metadata.SupportsUnannotatedAPIs);

            using var _ = PooledDictionary<string, ArrayBuilder<Lazy<TService, EmbeddedLanguageMetadata>>>.GetInstance(out var map);

            foreach (var classifier in orderedClassifiers)
            {
                foreach (var identifier in classifier.Metadata.Identifiers)
                    map.MultiAdd(identifier, classifier);
            }

            foreach (var (_, services) in map)
                services.RemoveDuplicates();

            this._identifierToServices = map.ToImmutableDictionary(
                kvp => kvp.Key, kvp => kvp.Value.ToImmutableAndFree(), StringComparer.OrdinalIgnoreCase);

            Info = info;
            _detector = new EmbeddedLanguageDetector(info, _identifierToServices.Keys.ToImmutableArray());

            SyntaxTokenKinds.Add(syntaxKinds.CharacterLiteralToken);
            SyntaxTokenKinds.Add(syntaxKinds.StringLiteralToken);
            SyntaxTokenKinds.Add(syntaxKinds.InterpolatedStringTextToken);

            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.SingleLineRawStringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.MultiLineRawStringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.Utf8StringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.Utf8SingleLineRawStringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.Utf8MultiLineRawStringLiteralToken);
        }

        protected ImmutableArray<Lazy<TService, EmbeddedLanguageMetadata>> GetServices(
            SemanticModel semanticModel,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            // First, see if this is a string annotated with either a comment or [StringSyntax] attribute. If
            // so, delegate to the first classifier we have registered for whatever language ID we find.
            if (this._detector.IsEmbeddedLanguageToken(token, semanticModel, cancellationToken, out var identifier, out _) &&
                _identifierToServices.TryGetValue(identifier, out var services))
            {
                Contract.ThrowIfTrue(services.IsDefaultOrEmpty);
                return services;
            }

            // If not, see if any of our legacy services might be able to handle this.
            return _legacyServices;
        }
    }
}
