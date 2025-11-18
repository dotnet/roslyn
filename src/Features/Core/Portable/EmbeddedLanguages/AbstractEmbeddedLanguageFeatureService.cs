// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages;

internal abstract class AbstractEmbeddedLanguageFeatureService<TService>
    where TService : IEmbeddedLanguageFeatureService
{
    /// <summary>
    /// The kinds of literal tokens that we want to do embedded language work for.
    /// </summary>
    protected readonly HashSet<int> SyntaxTokenKinds = [];

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
        // Order the feature providers to respect the [Order] annotations.
        var orderedFeatureProviders = ExtensionOrderer.Order(allServices).WhereAsArray(c => c.Metadata.Languages.Contains(languageName));

        // Grab out the services that handle unannotated literals and APIs.
        _legacyServices = orderedFeatureProviders.WhereAsArray(c => c.Metadata.SupportsUnannotatedAPIs);

        using var _ = PooledDictionary<string, ArrayBuilder<Lazy<TService, EmbeddedLanguageMetadata>>>.GetInstance(out var map);

        foreach (var featureProvider in orderedFeatureProviders)
        {
            foreach (var identifier in featureProvider.Metadata.Identifiers)
                map.MultiAdd(identifier, featureProvider);
        }

        foreach (var (_, services) in map)
            services.RemoveDuplicates();

        this._identifierToServices = map.ToImmutableDictionary(
            kvp => kvp.Key, kvp => kvp.Value.ToImmutableAndFree(), StringComparer.OrdinalIgnoreCase);

        Info = info;
        var languageIdentifiers = _identifierToServices.Keys.ToImmutableArray();
        _detector = new EmbeddedLanguageDetector(info, languageIdentifiers, GetCommentDetector(languageIdentifiers));

        SyntaxTokenKinds.Add(syntaxKinds.CharacterLiteralToken);
        SyntaxTokenKinds.Add(syntaxKinds.StringLiteralToken);
        SyntaxTokenKinds.Add(syntaxKinds.InterpolatedStringTextToken);

        SyntaxTokenKinds.AddIfNotNull(syntaxKinds.SingleLineRawStringLiteralToken);
        SyntaxTokenKinds.AddIfNotNull(syntaxKinds.MultiLineRawStringLiteralToken);
        SyntaxTokenKinds.AddIfNotNull(syntaxKinds.Utf8StringLiteralToken);
        SyntaxTokenKinds.AddIfNotNull(syntaxKinds.Utf8SingleLineRawStringLiteralToken);
        SyntaxTokenKinds.AddIfNotNull(syntaxKinds.Utf8MultiLineRawStringLiteralToken);
    }

    private static EmbeddedLanguageCommentDetector GetCommentDetector(ImmutableArray<string> languageIdentifiers)
    {
        // Well known language detectors we can cache.

        if (languageIdentifiers.SetEquals(JsonLanguageDetector.LanguageIdentifiers, StringComparer.OrdinalIgnoreCase))
            return JsonLanguageDetector.CommentDetector;

        if (languageIdentifiers.SetEquals(RegexLanguageDetector.LanguageIdentifiers, StringComparer.OrdinalIgnoreCase))
            return RegexLanguageDetector.CommentDetector;

        if (languageIdentifiers.SetEquals(DateAndTimeLanguageDetector.LanguageIdentifiers, StringComparer.OrdinalIgnoreCase))
            return DateAndTimeLanguageDetector.CommentDetector;

        return new EmbeddedLanguageCommentDetector(languageIdentifiers);
    }

    protected ImmutableArray<Lazy<TService, EmbeddedLanguageMetadata>> GetServices(
        SemanticModel semanticModel,
        SyntaxToken token,
        CancellationToken cancellationToken)
    {
        // First, see if this is a string annotated with either a comment or [StringSyntax] attribute. If
        // so, delegate to the first feature provider we have registered for whatever language ID we find.
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
