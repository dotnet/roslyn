using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents an asynchronously accessible project info.
    /// </summary>
    internal sealed class AsyncProjectInfo
    {
        /// <summary>
        /// The project id 
        /// </summary>
        public ProjectId ProjectId { get; private set; }

        /// <summary>
        /// The accessor that will asynchronously compute the project info.
        /// </summary>
        public ValueSource<ProjectInfo> Source { get; private set; }

        public AsyncProjectInfo(ProjectId projectId, ValueSource<ProjectInfo> source)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException("projectId");
            }

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.ProjectId = projectId;
            this.Source = source;
        }
    }
}