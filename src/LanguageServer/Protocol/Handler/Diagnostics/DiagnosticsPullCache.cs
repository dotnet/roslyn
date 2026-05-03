// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract partial class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    internal readonly record struct DiagnosticsRequestState(Project Project, int GlobalStateVersion, RequestContext Context, IDiagnosticSource DiagnosticSource);

    /// <summary>
    /// Cache where we store the data produced by prior requests so that they can be returned if nothing of significance
    /// changed. The <see cref="VersionStamp"/> is produced by <see
    /// cref="Project.GetDependentVersionAsync(CancellationToken)"/> while the <see cref="Checksum"/> is produced by
    /// <see cref="CodeAnalysis.Diagnostics.Extensions.GetDiagnosticChecksumAsync"/>.  The former is faster and works
    /// well for us in the normal case.  The latter still allows us to reuse diagnostics when changes happen that update
    /// the version stamp but not the content (for example, forking LSP text).
    /// </summary>
    private sealed class DiagnosticsPullCache(IGlobalOptionService globalOptions, string uniqueKey)
        : VersionedPullCache<(int globalStateVersion, Checksum dependentChecksum), DiagnosticsRequestState, ImmutableArray<DiagnosticData>>(uniqueKey)
    {
        private readonly IGlobalOptionService _globalOptions = globalOptions;

        public override async Task<(int globalStateVersion, Checksum dependentChecksum)> ComputeVersionAsync(DiagnosticsRequestState state, CancellationToken cancellationToken)
        {
            return (state.GlobalStateVersion, await state.Project.GetDiagnosticChecksumAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <inheritdoc cref="VersionedPullCache{TVersion, TState, TComputedData}.ComputeDataAsync(TState, CancellationToken)"/>
        public override async Task<ImmutableArray<DiagnosticData>> ComputeDataAsync(DiagnosticsRequestState state, CancellationToken cancellationToken)
        {
            var diagnostics = await state.DiagnosticSource.GetDiagnosticsAsync(state.Context, cancellationToken).ConfigureAwait(false);
            state.Context.TraceDebug($"Found {diagnostics.Length} diagnostics for {state.DiagnosticSource.ToDisplayString()}");
            return diagnostics;
        }

        public override Checksum ComputeChecksum(ImmutableArray<DiagnosticData> data, string language)
        {
            // Create checksums of diagnostic data and sort to ensure stable ordering for comparison.
            using var _ = ArrayBuilder<Checksum>.GetInstance(out var builder);
            foreach (var datum in data)
                builder.Add(Checksum.Create(datum, SerializeDiagnosticData));

            // Ensure that if fading options change that we recompute the checksum as it will produce different data
            // that we would report to the client.
            var option1 = _globalOptions.GetOption(FadingOptions.FadeOutUnreachableCode, language);
            var option2 = _globalOptions.GetOption(FadingOptions.FadeOutUnusedImports, language);
            var option3 = _globalOptions.GetOption(FadingOptions.FadeOutUnusedMembers, language);

            var value =
                (option1 ? (1 << 2) : 0) |
                (option2 ? (1 << 1) : 0) |
                (option3 ? (1 << 0) : 0);

            builder.Add(new Checksum(0, value));
            builder.Sort();

            return Checksum.Create(builder);
        }

        private static void SerializeDiagnosticData(DiagnosticData diagnosticData, ObjectWriter writer)
        {
            writer.WriteString(diagnosticData.Id);
            writer.WriteString(diagnosticData.Category);
            writer.WriteString(diagnosticData.Message);
            writer.WriteInt32((int)diagnosticData.Severity);
            writer.WriteInt32((int)diagnosticData.DefaultSeverity);
            writer.WriteBoolean(diagnosticData.IsEnabledByDefault);
            writer.WriteInt32(diagnosticData.WarningLevel);

            // Ensure the tag order is stable before we write it.
            foreach (var tag in diagnosticData.CustomTags.Sort())
            {
                writer.WriteString(tag);
            }

            foreach (var key in diagnosticData.Properties.Keys.ToImmutableArray().Sort())
            {
                writer.WriteString(key);
                writer.WriteString(diagnosticData.Properties[key]);
            }

            writer.WriteGuid(diagnosticData.ProjectId.Id);

            WriteDiagnosticDataLocation(diagnosticData.DataLocation, writer);

            foreach (var additionalLocation in diagnosticData.AdditionalLocations)
            {
                WriteDiagnosticDataLocation(additionalLocation, writer);
            }

            writer.WriteString(diagnosticData.Language);
            writer.WriteString(diagnosticData.Title);
            writer.WriteString(diagnosticData.Description);
            writer.WriteString(diagnosticData.HelpLink);
            writer.WriteBoolean(diagnosticData.IsSuppressed);

            static void WriteDiagnosticDataLocation(DiagnosticDataLocation location, ObjectWriter writer)
            {
                WriteFileLinePositionSpan(location.UnmappedFileSpan, writer);
                if (location.DocumentId != null)
                    writer.WriteGuid(location.DocumentId.Id);

                WriteFileLinePositionSpan(location.MappedFileSpan, writer);
            }

            static void WriteFileLinePositionSpan(FileLinePositionSpan fileSpan, ObjectWriter writer)
            {
                writer.WriteString(fileSpan.Path);
                WriteLinePositionSpan(fileSpan.Span, writer);
                writer.WriteBoolean(fileSpan.HasMappedPath);
            }

            static void WriteLinePositionSpan(LinePositionSpan span, ObjectWriter writer)
            {
                WriteLinePosition(span.Start, writer);
                WriteLinePosition(span.End, writer);
            }

            static void WriteLinePosition(LinePosition position, ObjectWriter writer)
            {
                writer.WriteInt32(position.Line);
                writer.WriteInt32(position.Character);
            }

        }
    }
}
