// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
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

        private SolutionInfo(SolutionAttributes attributes, IEnumerable<ProjectInfo>? projects)
        {
            Attributes = attributes;
            Projects = projects.ToImmutableReadOnlyListOrEmpty();
        }

        /// <summary>
        /// Create a new instance of a SolutionInfo.
        /// </summary>
        public static SolutionInfo Create(
            SolutionId id,
            VersionStamp version,
            string? filePath = null,
            IEnumerable<ProjectInfo>? projects = null)
        {
            return new SolutionInfo(new SolutionAttributes(id, version, filePath), projects);
        }

        private SolutionInfo With(
            SolutionAttributes? attributes = null,
            IEnumerable<ProjectInfo>? projects = null)
        {
            var newAttributes = attributes ?? Attributes;
            var newProjects = projects ?? Projects;

            if (newAttributes == Attributes &&
                newProjects == Projects)
            {
                return this;
            }

            return new SolutionInfo(newAttributes, newProjects);
        }

        internal SolutionInfo WithVersion(VersionStamp version)
        {
            return With(attributes: new SolutionAttributes(Attributes.Id, version, Attributes.FilePath));
        }

        internal SolutionInfo WithFilePath(string? filePath)
        {
            return With(attributes: new SolutionAttributes(Attributes.Id, Attributes.Version, filePath));
        }

        internal SolutionInfo WithProjects(IEnumerable<ProjectInfo> projects)
        {
            return With(projects: projects);
        }

        /// <summary>
        /// type that contains information regarding this solution itself but
        /// no tree information such as project info
        /// </summary>
        internal class SolutionAttributes : IChecksummedObject, IObjectWritable
        {
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

            public SolutionAttributes(SolutionId id, VersionStamp version, string? filePath)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Version = version;
                FilePath = filePath;
            }

            public SolutionAttributes WithVersion(VersionStamp versionStamp)
            {
                return new SolutionAttributes(Id, versionStamp, FilePath);
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            public void WriteTo(ObjectWriter writer)
            {
                Id.WriteTo(writer);

                // TODO: figure out a way to send version info over as well.
                //       right now, version get updated automatically, so 2 can't be exactly match
                // info.Version.WriteTo(writer);

                writer.WriteString(FilePath);
            }

            public static SolutionAttributes ReadFrom(ObjectReader reader)
            {
                var solutionId = SolutionId.ReadFrom(reader);
                // var version = VersionStamp.ReadFrom(reader);
                var filePath = reader.ReadString();

                return new SolutionAttributes(solutionId, VersionStamp.Create(), filePath);
            }

            private Checksum? _lazyChecksum;
            Checksum IChecksummedObject.Checksum
            {
                get
                {
                    if (_lazyChecksum == null)
                    {
                        _lazyChecksum = Checksum.Create(WellKnownSynchronizationKind.SolutionAttributes, this);
                    }

                    return _lazyChecksum;
                }
            }
        }
    }
}
