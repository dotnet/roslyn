// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics;

[Export(typeof(ITaggerProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
[TagType(typeof(InlineDiagnosticsTag))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class InlineDiagnosticsTaggerProvider(
    TaggerHost taggerHost,
    IEditorFormatMapService editorFormatMapService,
    IClassificationFormatMapService classificationFormatMapService,
    IClassificationTypeRegistryService classificationTypeRegistryService)
    : AbstractDiagnosticsTaggerProvider<InlineDiagnosticsTag>(
        taggerHost, FeatureAttribute.ErrorSquiggles)
{
    private readonly IEditorFormatMap _editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
    private readonly IClassificationFormatMapService _classificationFormatMapService = classificationFormatMapService;
    private readonly IClassificationTypeRegistryService _classificationTypeRegistryService = classificationTypeRegistryService;

    protected sealed override ImmutableArray<IOption2> Options { get; } = [InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics];
    protected sealed override ImmutableArray<IOption2> FeatureOptions { get; } = [InlineDiagnosticsOptionsStorage.Location];

    protected sealed override bool IncludeDiagnostic(DiagnosticData diagnostic)
    {
        return
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error &&
            !string.IsNullOrWhiteSpace(diagnostic.Message) &&
            !diagnostic.IsSuppressed;
    }

    protected override InlineDiagnosticsTag? CreateTag(Workspace workspace, DiagnosticData diagnostic)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(diagnostic.Message));
        var errorType = GetErrorTypeFromDiagnostic(diagnostic);
        if (errorType is null)
        {
            return null;
        }

        if (diagnostic.DocumentId is null)
        {
            return null;
        }

        var project = workspace.CurrentSolution.GetProject(diagnostic.DocumentId.ProjectId);
        if (project is null)
        {
            return null;
        }

        var locationOption = GlobalOptions.GetOption(InlineDiagnosticsOptionsStorage.Location, project.Language);
        var navigateService = workspace.Services.GetRequiredService<INavigateToLinkService>();
        return new InlineDiagnosticsTag(errorType, diagnostic, _editorFormatMap, _classificationFormatMapService,
            _classificationTypeRegistryService, locationOption, navigateService);
    }

    private static string? GetErrorTypeFromDiagnostic(DiagnosticData diagnostic)
    {
        if (diagnostic.Severity == DiagnosticSeverity.Error)
        {
            return diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue)
                ? EditAndContinueErrorTypeDefinition.Name
                : PredefinedErrorTypeNames.SyntaxError;
        }
        else if (diagnostic.Severity == DiagnosticSeverity.Warning)
        {
            return PredefinedErrorTypeNames.Warning;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// TODO: is there anything we can do better here? Inline diagnostic tags are not really data, but more UI
    /// elements with specific controls, positions and events attached to them.  There doesn't seem to be a safe way
    /// to reuse any of these currently.  Ideally we could do something similar to inline-hints where there's a data
    /// tagger portion (which is async and has clean equality semantics), and then the UI portion which just
    /// translates those data-tags to the UI tags.
    /// <para>
    /// Doing direct equality means we'll always end up regenerating all tags.  But hopefully there won't be that
    /// many in a document to matter.
    /// </para>
    /// </summary>
    protected sealed override bool TagEquals(InlineDiagnosticsTag tag1, InlineDiagnosticsTag tag2)
        => tag1 == tag2;
}
