// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A class that represents all the arguments necessary to create a new solution instance.
/// </summary>
public sealed class SolutionInfo
{
    internal SolutionAttributes Attributes { get; }

    /// <summary>
    /// The unique Id of the solution.
    /// </summary>
    public SolutionId Id => Attributes.Id;

    /// <summary>
    /// The version of the solution.
    /// </summary>
    public VersionStamp Version => Attributes.Version;

    /// <summary>
    /// The path to the solution file, or null if there is no solution file.
    /// </summary>
    public string? FilePath => Attributes.FilePath;

    /// <summary>
    /// A list of projects initially associated with the solution.
    /// </summary>
    public IReadOnlyList<ProjectInfo> Projects { get; }

    /// <summary>
    /// The analyzers initially associated with this solution.
    /// </summary>
    public IReadOnlyList<AnalyzerReference> AnalyzerReferences { get; }

    /// <summary>
    /// Per-language analyzer config options that are used as a fallback if the option is not present in <see cref="AnalyzerConfigOptionsResult"/> produced by the compiler.
    /// Implements a top-level (but not global) virtual editorconfig file that's in scope for all source files of the solution.
    /// </summary>
    internal ImmutableDictionary<string, StructuredAnalyzerConfigOptions> FallbackAnalyzerOptions { get; }

    private SolutionInfo(
        SolutionAttributes attributes,
        IReadOnlyList<ProjectInfo> projects,
        IReadOnlyList<AnalyzerReference> analyzerReferences,
        ImmutableDictionary<string, StructuredAnalyzerConfigOptions> fallbackAnalyzerOptions)
    {
        Attributes = attributes;
        Projects = projects;
        AnalyzerReferences = analyzerReferences;
        FallbackAnalyzerOptions = fallbackAnalyzerOptions;
    }

    // 3.5.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
    /// <summary>
    /// Create a new instance of a SolutionInfo.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SolutionInfo Create(
        SolutionId id,
        VersionStamp version,
        string? filePath,
        IEnumerable<ProjectInfo>? projects)
    {
        return Create(id, version, filePath, projects, analyzerReferences: null);
    }

    /// <summary>
    /// Create a new instance of a SolutionInfo.
    /// </summary>
    public static SolutionInfo Create(
        SolutionId id,
        VersionStamp version,
        string? filePath = null,
        IEnumerable<ProjectInfo>? projects = null,
        IEnumerable<AnalyzerReference>? analyzerReferences = null)
        => Create(id, version, filePath, projects, analyzerReferences, ImmutableDictionary<string, StructuredAnalyzerConfigOptions>.Empty);

    /// <summary>
    /// Create a new instance of a SolutionInfo.
    /// </summary>
    internal static SolutionInfo Create(
        SolutionId id,
        VersionStamp version,
        string? filePath,
        IEnumerable<ProjectInfo>? projects,
        IEnumerable<AnalyzerReference>? analyzerReferences,
        ImmutableDictionary<string, StructuredAnalyzerConfigOptions> fallbackAnalyzerOptions)
    {
        return new SolutionInfo(
            new SolutionAttributes(
                id ?? throw new ArgumentNullException(nameof(id)),
                version,
                filePath,
                telemetryId: default),
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(projects, nameof(projects)),
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences)),
            fallbackAnalyzerOptions);
    }

    internal SolutionInfo WithTelemetryId(Guid telemetryId)
        => new(Attributes.With(telemetryId: telemetryId), Projects, AnalyzerReferences, FallbackAnalyzerOptions);

    /// <summary>
    /// type that contains information regarding this solution itself but
    /// no tree information such as project info
    /// </summary>
    internal sealed class SolutionAttributes(SolutionId id, VersionStamp version, string? filePath, Guid telemetryId)
    {
        private SingleInitNullable<Checksum> _lazyChecksum;

        /// <summary>
        /// The unique Id of the solution.
        /// </summary>
        public SolutionId Id { get; } = id;

        /// <summary>
        /// The version of the solution.
        /// </summary>
        public VersionStamp Version { get; } = version;

        /// <summary>
        /// The path to the solution file, or null if there is no solution file.
        /// </summary>
        public string? FilePath { get; } = filePath;

        /// <summary>
        /// The id report during telemetry events.
        /// </summary>
        public Guid TelemetryId { get; } = telemetryId;

        public SolutionAttributes With(
            VersionStamp? version = null,
            Optional<string?> filePath = default,
            Optional<Guid> telemetryId = default)
        {
            var newVersion = version ?? Version;
            var newFilePath = filePath.HasValue ? filePath.Value : FilePath;
            var newTelemetryId = telemetryId.HasValue ? telemetryId.Value : TelemetryId;

            if (newVersion == Version &&
                newFilePath == FilePath &&
                newTelemetryId == TelemetryId)
            {
                return this;
            }

            return new SolutionAttributes(Id, newVersion, newFilePath, newTelemetryId);
        }

        public void WriteTo(ObjectWriter writer)
        {
            Id.WriteTo(writer);

            // TODO: figure out a way to send version info over as well.
            //       right now, version get updated automatically, so 2 can't be exactly match
            // info.Version.WriteTo(writer);

            writer.WriteString(FilePath);
            writer.WriteGuid(TelemetryId);
        }

        public static SolutionAttributes ReadFrom(ObjectReader reader)
        {
            var solutionId = SolutionId.ReadFrom(reader);
            // var version = VersionStamp.ReadFrom(reader);
            var filePath = reader.ReadString();
            var telemetryId = reader.ReadGuid();

            return new SolutionAttributes(solutionId, VersionStamp.Create(), filePath, telemetryId);
        }

        public Checksum Checksum
            => _lazyChecksum.Initialize(valueFactory: static @this => Checksum.Create(@this, static (@this, writer) => @this.WriteTo(writer)), arg: this);
    }
}
