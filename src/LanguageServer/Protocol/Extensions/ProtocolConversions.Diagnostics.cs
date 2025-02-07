// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Features.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal static partial class ProtocolConversions
{
    /// <summary>
    /// Converts from <see cref="DiagnosticData"/> to <see cref="LSP.Diagnostic"/>
    /// </summary>
    /// <param name="diagnosticData">The diagnostic to convert</param>
    /// <param name="supportsVisualStudioExtensions">Whether the client is Visual Studio</param>
    /// <param name="project">The project the diagnostic is relevant to</param>
    /// <param name="isLiveSource">Whether the diagnostic is considered "live" and should supersede others</param>
    /// <param name="potentialDuplicate">Whether the diagnostic is potentially a duplicate to a build diagnostic</param>
    /// <param name="globalOptionService">The global options service</param>
    public static ImmutableArray<LSP.Diagnostic> ConvertDiagnostic(DiagnosticData diagnosticData, bool supportsVisualStudioExtensions, Project project, bool isLiveSource, bool potentialDuplicate, IGlobalOptionService globalOptionService)
    {
        if (!ShouldIncludeHiddenDiagnostic(diagnosticData, supportsVisualStudioExtensions))
        {
            return [];
        }

        var diagnostic = CreateLspDiagnostic(diagnosticData, project, isLiveSource, potentialDuplicate, supportsVisualStudioExtensions);

        // Check if we need to handle the unnecessary tag (fading).
        if (!diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
        {
            return [diagnostic];
        }

        // DiagnosticId supports fading, check if the corresponding VS option is turned on.
        if (!SupportsFadingOption(diagnosticData, globalOptionService))
        {
            return [diagnostic];
        }

        // Check to see if there are specific locations marked to fade.
        if (!diagnosticData.TryGetUnnecessaryDataLocations(out var unnecessaryLocations))
        {
            // There are no specific fading locations, just mark the whole diagnostic span as unnecessary.
            // We should always have at least one tag (build or intellisense error).
            Contract.ThrowIfNull(diagnostic.Tags, $"diagnostic {diagnostic.Identifier} was missing tags");
            diagnostic.Tags = diagnostic.Tags.Append(DiagnosticTag.Unnecessary);
            return [diagnostic];
        }

        if (supportsVisualStudioExtensions)
        {
            // Roslyn produces unnecessary diagnostics by using additional locations, however LSP doesn't support tagging
            // additional locations separately.  Instead we just create multiple hidden diagnostics for unnecessary squiggling.
            using var _ = ArrayBuilder<LSP.Diagnostic>.GetInstance(out var diagnosticsBuilder);
            diagnosticsBuilder.Add(diagnostic);
            foreach (var location in unnecessaryLocations)
            {
                var additionalDiagnostic = CreateLspDiagnostic(diagnosticData, project, isLiveSource, potentialDuplicate, supportsVisualStudioExtensions);
                additionalDiagnostic.Severity = LSP.DiagnosticSeverity.Hint;
                additionalDiagnostic.Range = GetRange(location);
                additionalDiagnostic.Tags = [DiagnosticTag.Unnecessary, VSDiagnosticTags.HiddenInEditor, VSDiagnosticTags.HiddenInErrorList, VSDiagnosticTags.SuppressEditorToolTip];
                diagnosticsBuilder.Add(additionalDiagnostic);
            }

            return diagnosticsBuilder.ToImmutableArray();
        }
        else
        {
            diagnostic.Tags = diagnostic.Tags != null ? diagnostic.Tags.Append(DiagnosticTag.Unnecessary) : [DiagnosticTag.Unnecessary];
            var diagnosticRelatedInformation = unnecessaryLocations.Value.Select(l => new DiagnosticRelatedInformation
            {
                Location = new LSP.Location
                {
                    Range = GetRange(l),
                    Uri = ProtocolConversions.CreateAbsoluteUri(l.UnmappedFileSpan.Path)
                },
                Message = diagnostic.Message
            }).ToArray();
            diagnostic.RelatedInformation = diagnosticRelatedInformation;
            return [diagnostic];
        }
    }

    private static LSP.VSDiagnostic CreateLspDiagnostic(
        DiagnosticData diagnosticData,
        Project project,
        bool isLiveSource,
        bool potentialDuplicate,
        bool supportsVisualStudioExtensions)
    {
        Contract.ThrowIfNull(diagnosticData.Message, $"Got a document diagnostic that did not have a {nameof(diagnosticData.Message)}");

        // We can just use VSDiagnostic as it doesn't have any default properties set that
        // would get automatically serialized.
        var diagnostic = new LSP.VSDiagnostic
        {
            Code = diagnosticData.Id,
            CodeDescription = ProtocolConversions.HelpLinkToCodeDescription(diagnosticData.GetValidHelpLinkUri()),
            Message = diagnosticData.Message,
            Severity = ConvertDiagnosticSeverity(diagnosticData.Severity, supportsVisualStudioExtensions),
            Tags = ConvertTags(diagnosticData, isLiveSource, potentialDuplicate),
            DiagnosticRank = ConvertRank(diagnosticData),
            Range = GetRange(diagnosticData.DataLocation)
        };

        if (supportsVisualStudioExtensions)
        {
            var expandedMessage = string.IsNullOrEmpty(diagnosticData.Description) ? null : diagnosticData.Description;
            var informationService = project.Solution.Services.GetRequiredService<IDiagnosticProjectInformationService>();

            diagnostic.DiagnosticType = diagnosticData.Category;
            diagnostic.ExpandedMessage = expandedMessage;
            diagnostic.Projects = [informationService.GetDiagnosticProjectInformation(project)];

            // Defines an identifier used by the client for merging diagnostics across projects. We want diagnostics
            // to be merged from separate projects if they have the same code, filepath, range, and message.
            //
            // Note: LSP pull diagnostics only operates on unmapped locations.
            diagnostic.Identifier = (diagnostic.Code, diagnosticData.DataLocation.UnmappedFileSpan.Path, diagnostic.Range, diagnostic.Message)
                .GetHashCode().ToString();
        }

        return diagnostic;
    }

    private static LSP.Range GetRange(DiagnosticDataLocation dataLocation)
    {
        // We currently do not map diagnostics spans as
        //   1.  Razor handles span mapping for razor files on their side.
        //   2.  LSP does not allow us to report document pull diagnostics for a different file path.
        //   3.  The VS LSP client does not support document pull diagnostics for files outside our content type.
        //   4.  This matches classic behavior where we only squiggle the original location anyway.

        // We also do not adjust the diagnostic locations to ensure they are in bounds because we've
        // explicitly requested up to date diagnostics as of the snapshot we were passed in.
        return new LSP.Range
        {
            Start = new Position
            {
                Character = dataLocation.UnmappedFileSpan.StartLinePosition.Character,
                Line = dataLocation.UnmappedFileSpan.StartLinePosition.Line,
            },
            End = new Position
            {
                Character = dataLocation.UnmappedFileSpan.EndLinePosition.Character,
                Line = dataLocation.UnmappedFileSpan.EndLinePosition.Line,
            }
        };
    }

    private static bool ShouldIncludeHiddenDiagnostic(DiagnosticData diagnosticData, bool supportsVisualStudioExtensions)
    {
        // VS can handle us reporting any kind of diagnostic using VS custom tags.
        if (supportsVisualStudioExtensions == true)
        {
            return true;
        }

        // Diagnostic isn't hidden - we should report this diagnostic in all scenarios.
        if (diagnosticData.Severity != DiagnosticSeverity.Hidden)
        {
            return true;
        }

        // Roslyn creates these for example in remove unnecessary imports, see RemoveUnnecessaryImportsConstants.DiagnosticFixableId.
        // These aren't meant to be visible in anyway, so we can safely exclude them.
        // TODO - We should probably not be creating these as separate diagnostics or have a 'really really' hidden tag.
        if (string.IsNullOrEmpty(diagnosticData.Message))
        {
            return false;
        }

        // Hidden diagnostics that are unnecessary are visible to the user in the form of fading.
        // We can report these diagnostics.
        if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
        {
            return true;
        }

        // We have a hidden diagnostic that has no fading.  This diagnostic can't be visible so don't send it to the client.
        return false;
    }

    private static VSDiagnosticRank? ConvertRank(DiagnosticData diagnosticData)
    {
        if (diagnosticData.Properties.TryGetValue(PullDiagnosticConstants.Priority, out var priority))
        {
            return priority switch
            {
                PullDiagnosticConstants.Low => VSDiagnosticRank.Low,
                PullDiagnosticConstants.Medium => VSDiagnosticRank.Default,
                PullDiagnosticConstants.High => VSDiagnosticRank.High,
                _ => null,
            };
        }

        return null;
    }

    private static LSP.DiagnosticSeverity ConvertDiagnosticSeverity(DiagnosticSeverity severity, bool supportsVisualStudioExtensions)
        => severity switch
        {
            // Hidden is translated in ConvertTags to pass along appropriate _ms tags
            // that will hide the item in a client that knows about those tags.
            DiagnosticSeverity.Hidden => LSP.DiagnosticSeverity.Hint,
            // VSCode shows information diagnostics as blue squiggles, and hint diagnostics as 3 dots.  We prefer the latter rendering so we return hint diagnostics in vscode.
            DiagnosticSeverity.Info => supportsVisualStudioExtensions ? LSP.DiagnosticSeverity.Information : LSP.DiagnosticSeverity.Hint,
            DiagnosticSeverity.Warning => LSP.DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error => LSP.DiagnosticSeverity.Error,
            _ => throw ExceptionUtilities.UnexpectedValue(severity),
        };

    /// <summary>
    /// If you make change in this method, please also update the corresponding file in
    /// src\VisualStudio\Xaml\Impl\Implementation\LanguageServer\Handler\Diagnostics\AbstractPullDiagnosticHandler.cs
    /// </summary>
    private static DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource, bool potentialDuplicate)
    {
        using var _ = ArrayBuilder<DiagnosticTag>.GetInstance(out var result);

        if (diagnosticData.Severity == DiagnosticSeverity.Hidden)
        {
            result.Add(VSDiagnosticTags.HiddenInEditor);
            result.Add(VSDiagnosticTags.HiddenInErrorList);
            result.Add(VSDiagnosticTags.SuppressEditorToolTip);
        }
        else
        {
            result.Add(VSDiagnosticTags.VisibleInErrorList);
        }

        if (diagnosticData.CustomTags.Contains(PullDiagnosticConstants.TaskItemCustomTag))
            result.Add(VSDiagnosticTags.TaskItem);

        // Let the host know that these errors represent potentially stale information from the past that should
        // be superseded by fresher info.
        if (potentialDuplicate)
            result.Add(VSDiagnosticTags.PotentialDuplicate);

        // Mark this also as a build error.  That way an explicitly kicked off build from a source like CPS can
        // override it.
        if (!isLiveSource)
            result.Add(VSDiagnosticTags.BuildError);

        result.Add(diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Build)
            ? VSDiagnosticTags.BuildError
            : VSDiagnosticTags.IntellisenseError);

        if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue))
            result.Add(VSDiagnosticTags.EditAndContinueError);

        return result.ToArray();
    }

    private static bool SupportsFadingOption(DiagnosticData diagnosticData, IGlobalOptionService globalOptionService)
    {
        if (IDEDiagnosticIdToOptionMappingHelper.TryGetMappedFadingOption(diagnosticData.Id, out var fadingOption))
        {
            Contract.ThrowIfNull(diagnosticData.Language, $"diagnostic {diagnosticData.Id} is missing a language");
            return globalOptionService.GetOption(fadingOption, diagnosticData.Language);
        }

        return true;
    }
}
