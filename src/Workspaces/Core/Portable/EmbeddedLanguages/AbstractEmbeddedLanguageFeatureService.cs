// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageServices;
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
        protected readonly ImmutableArray<Lazy<TService, EmbeddedLanguageMetadata>> LegacyServices;

        /// <summary>
        /// Ordered mapping of a lang ID (like 'Json') to all the services for that language. This allows for multiple
        /// classifiers to be available.  The first service though that returns results for a string will 'win' and no
        /// other services will contribute.
        /// </summary>
        protected readonly Dictionary<string, ArrayBuilder<Lazy<TService, EmbeddedLanguageMetadata>>> IdentifierToServices = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Information about the embedded language.
        /// </summary>
        protected readonly EmbeddedLanguageInfo Info;

        /// <summary>
        /// Helper to look at string literals and determine what language they are annotated to take.
        /// </summary>
        protected readonly EmbeddedLanguageDetector Detector;

        protected AbstractEmbeddedLanguageFeatureService(
            string languageName,
            EmbeddedLanguageInfo info,
            ISyntaxKinds syntaxKinds,
            IEnumerable<Lazy<TService, EmbeddedLanguageMetadata>> allServices)
        {
            // Order the classifiers to respect the [Order] annotations.
            var orderedClassifiers = ExtensionOrderer.Order(allServices).Where(c => c.Metadata.Language == languageName).ToImmutableArray();

            // Grab out the services that handle unannotated literals and APIs.
            LegacyServices = orderedClassifiers.WhereAsArray(c => c.Metadata.SupportsUnannotatedAPIs);

            foreach (var classifier in orderedClassifiers)
            {
                foreach (var identifier in classifier.Metadata.Identifiers)
                    IdentifierToServices.MultiAdd(identifier, classifier);
            }

            foreach (var (_, services) in IdentifierToServices)
                services.RemoveDuplicates();

            Info = info;
            Detector = new EmbeddedLanguageDetector(info, IdentifierToServices.Keys.ToImmutableArray());

            SyntaxTokenKinds.Add(syntaxKinds.CharacterLiteralToken);
            SyntaxTokenKinds.Add(syntaxKinds.StringLiteralToken);
            SyntaxTokenKinds.Add(syntaxKinds.InterpolatedStringTextToken);

            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.SingleLineRawStringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.MultiLineRawStringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.UTF8StringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.UTF8SingleLineRawStringLiteralToken);
            SyntaxTokenKinds.AddIfNotNull(syntaxKinds.UTF8MultiLineRawStringLiteralToken);
        }
    }
}
