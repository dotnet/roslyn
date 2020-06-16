﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
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

        private SolutionInfo(SolutionAttributes attributes, IReadOnlyList<ProjectInfo> projects, IReadOnlyList<AnalyzerReference> analyzerReferences)
        {
            Attributes = attributes;
            Projects = projects;
            AnalyzerReferences = analyzerReferences;
        }

        // 3.5.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        /// <summary>
        /// Create a new instance of a SolutionInfo.
        /// </summary>
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
        {
            return new SolutionInfo(
                new SolutionAttributes(
                    id ?? throw new ArgumentNullException(nameof(id)),
                    version,
                    filePath,
                    telemetryId: default),
                PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(projects, nameof(projects)),
                PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences)));
        }

        internal ImmutableHashSet<string> GetProjectLanguages()
            => Projects.Select(p => p.Language).ToImmutableHashSet();

        internal SolutionInfo WithTelemetryId(Guid telemetryId)
            => new SolutionInfo(Attributes.With(telemetryId: telemetryId), Projects, AnalyzerReferences);

        /// <summary>
        /// type that contains information regarding this solution itself but
        /// no tree information such as project info
        /// </summary>
        internal sealed class SolutionAttributes : IChecksummedObject, IObjectWritable
        {
            private Checksum? _lazyChecksum;

            /// <summary>
            /// The unique Id of the solution.
            /// </summary>
            public SolutionId Id { get; }

            /// <summary>
            /// The version of the solution.
            /// </summary>
            public VersionStamp Version { get; }

            /// <summary>
            /// The path to the solution file, or null if there is no solution file.
            /// </summary>
            public string? FilePath { get; }

            /// <summary>
            /// The id report during telemetry events.
            /// </summary>
            public Guid TelemetryId { get; }

            public SolutionAttributes(SolutionId id, VersionStamp version, string? filePath, Guid telemetryId)
            {
                Id = id;
                Version = version;
                FilePath = filePath;
                TelemetryId = telemetryId;
            }

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

            bool IObjectWritable.ShouldReuseInSerialization => true;

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

            Checksum IChecksummedObject.Checksum
                => _lazyChecksum ??= Checksum.Create(WellKnownSynchronizationKind.SolutionAttributes, this);
        }
    }
}
