// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal readonly struct EmbeddedLanguageClassificationContext
{
    internal readonly SolutionServices SolutionServices;

    private readonly SegmentedList<ClassifiedSpan> _result;

    /// <summary>
    /// The portion of the string or character token to classify.
    /// </summary>
    private readonly TextSpan _spanToClassify;

    public Project Project { get; }

    /// <summary>
    /// The string or character token to classify.
    /// </summary>
    public SyntaxToken SyntaxToken { get; }

    /// <summary>
    /// SemanticModel that <see cref="SyntaxToken"/> is contained in.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    public CancellationToken CancellationToken { get; }

    internal readonly ClassificationOptions Options;
    internal readonly IVirtualCharService VirtualCharService;

    internal EmbeddedLanguageClassificationContext(
        SolutionServices solutionServices,
        Project project,
        SemanticModel semanticModel,
        SyntaxToken syntaxToken,
        TextSpan spanToClassify,
        ClassificationOptions options,
        IVirtualCharService virtualCharService,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        SolutionServices = solutionServices;
        Project = project;
        SemanticModel = semanticModel;
        SyntaxToken = syntaxToken;
        _spanToClassify = spanToClassify;
        Options = options;
        VirtualCharService = virtualCharService;
        _result = result;
        CancellationToken = cancellationToken;
    }

    public void AddClassification(string classificationType, TextSpan span)
    {
        // Ignore characters that don't intersect with the requested span.  That avoids potentially adding lots of
        // classifications for portions of a large string that are out of view.
        if (span.IntersectsWith(_spanToClassify))
            _result.Add(new ClassifiedSpan(classificationType, span));
    }
}
