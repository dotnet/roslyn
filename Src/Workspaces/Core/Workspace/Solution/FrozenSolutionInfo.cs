using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class FrozenSolutionInfo : SolutionInfo
    {
        private readonly SolutionId id;
        private readonly VersionStamp version;
        private readonly string filePath;
        private readonly string configuration;
        private readonly ImmutableList<FrozenProjectInfo> projects;

        public override SolutionId Id
        {
            get
            {
                return this.id;
            }
        }

        public override VersionStamp Version
        {
            get
            {
                return this.version;
            }
        }

        public override string FilePath
        {
            get
            {
                return this.filePath;
            }
        }

        public override string Configuration
        {
            get 
            {
                return this.configuration;
            }
        }

        public override IEnumerable<ProjectInfo> Projects
        {
            get
            {
                return this.projects;
            }
        }

        /// <summary>
        /// Create a new instance of SolutionInfo
        /// </summary>
        public FrozenSolutionInfo(
            SolutionId id,
            VersionStamp version,
            string filePath,
            string configuration,
            IEnumerable<FrozenProjectInfo> projects)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            this.id = id;
            this.version = version;
            this.filePath = filePath;
            this.configuration = configuration;
            this.projects = projects.ToImmutableList();
        }
    }
}