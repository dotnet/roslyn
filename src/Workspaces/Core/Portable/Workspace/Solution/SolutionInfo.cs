// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        public string FilePath => Attributes.FilePath;

        /// <summary>
        /// A list of projects initially associated with the solution.
        /// </summary>
        public IReadOnlyList<ProjectInfo> Projects { get; }

        private SolutionInfo(SolutionAttributes attributes, IEnumerable<ProjectInfo> projects)
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
            string filePath = null,
            IEnumerable<ProjectInfo> projects = null)
        {
            return new SolutionInfo(new SolutionAttributes(id, version, filePath), projects);
        }

        private SolutionInfo With(
            SolutionAttributes attributes = null,
            IEnumerable<ProjectInfo> projects = null)
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

        internal SolutionInfo WithFilePath(string filePath)
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
            public string FilePath { get; }

            public SolutionAttributes(SolutionId id, VersionStamp version, string filePath)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Version = version;
                FilePath = filePath;
            }

            public void WriteTo(ObjectWriter writer)
            {
                // these information is volatile. it can be different
                // per session or not content based value. basically not
                // persistable. these information will not be included in checksum
                Id.WriteTo(writer);
                Version.WriteTo(writer);

                writer.WriteString(FilePath);
            }

            public static SolutionAttributes ReadFrom(ObjectReader reader)
            {
                var solutionId = SolutionId.ReadFrom(reader);
                var version = VersionStamp.ReadFrom(reader);

                var filePath = reader.ReadString();

                return new SolutionAttributes(solutionId, version, filePath);
            }

            private Checksum _lazyChecksum;
            Checksum IChecksummedObject.Checksum
            {
                get
                {
                    if (_lazyChecksum == null)
                    {
                        using (var stream = SerializableBytes.CreateWritableStream())
                        using (var writer = new ObjectWriter(stream))
                        {
                            writer.WriteString(nameof(SolutionAttributes));

                            if (FilePath == null)
                            {
                                // this checksum is not persistable because
                                // this info doesn't have non volatile info
                                Id.WriteTo(writer);
                            }

                            // these information is not volatile. it won't be different
                            // per session, basically persistable content based values. 
                            // only these information will be included in checksum
                            writer.WriteString(FilePath);

                            _lazyChecksum = Checksum.Create(stream);
                        }
                    }

                    return _lazyChecksum;
                }
            }
        }
    }
}
