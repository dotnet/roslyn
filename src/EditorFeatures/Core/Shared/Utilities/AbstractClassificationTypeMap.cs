// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

/// <summary>
/// Note: we very intentionally layer things in the following fashion (from lowest to highest priority):
/// <list type="number">
/// <item>
/// Roslyn Syntactic Classifications.  These go into the "Editor Lexical" bucket, lower than everything else.
/// </item>
/// <item>
/// Roslyn Semantic Classifications.  These go into the "Editor Syntactic" bucket.  Effectively *almost* always beating
/// out the classifications produced by Roslyn's syntactic classifier.
/// </item>
/// <item>
/// Comments.  These go into the highest bucket "Editor Semantic".  This ensures that comments override *everything*,
/// causing classification to instantly 'snap' to the commented-out classification when the user comments code.  Without
/// this, our stale semantic classifications could show up *above* the commented code *up until* the point that semantic
/// classification ran again.
/// </item>
/// </list>
/// </summary>
internal abstract class AbstractClassificationTypeMap : IClassificationTypeMap
{
    private readonly Dictionary<string, IClassificationType> _identityMap;
    private readonly IClassificationTypeRegistryService _registryService;
    private readonly ClassificationLayer _classificationLayer;

    public AbstractClassificationTypeMap(
        IClassificationTypeRegistryService registryService,
        ClassificationLayer classificationLayer)
    {
        _registryService = registryService;
        _classificationLayer = classificationLayer;

        // Prepopulate the identity map with the constant string values from ClassificationTypeNames
        var fields = typeof(ClassificationTypeNames).GetFields();
        _identityMap = new Dictionary<string, IClassificationType>(fields.Length, ReferenceEqualityComparer.Instance);

        foreach (var field in fields)
        {
            // The strings returned from reflection do not have reference-identity with the string constants used by
            // the compiler. Fortunately, a call to string.Intern fixes them.
            var rawValue = (string?)field.GetValue(null);
            Contract.ThrowIfNull(rawValue);
            var value = string.Intern(rawValue);

            // Note:
            //
            // Classification of syntax and semantics happens on different cadences.  For that reason, prioritize the
            // classification 'comments' to be higher than everything else (place it on the editors highest 'semantic'
            // layer). That way, if a user comments something out, they'll see things snap to the commented state
            // immediately, instead of having to wait for semantic-classification to finish and return no items for that
            // region.
            var layer = value == ClassificationTypeNames.Comment
                ? ClassificationLayer.Semantic
                : classificationLayer;

            _identityMap.Add(value, registryService.GetClassificationType(layer, value));
        }
    }

    public IClassificationType GetClassificationType(string name)
    {
        var type = GetClassificationTypeWorker(name);
        if (type == null)
        {
            FatalError.ReportAndCatch(new Exception($"classification type doesn't exist for {name}"));
        }

        return type ?? GetClassificationTypeWorker(ClassificationTypeNames.Text);
    }

    private IClassificationType GetClassificationTypeWorker(string name)
    {
        return _identityMap.TryGetValue(name, out var result)
            ? result
            : _registryService.GetClassificationType(_classificationLayer, name);
    }
}
