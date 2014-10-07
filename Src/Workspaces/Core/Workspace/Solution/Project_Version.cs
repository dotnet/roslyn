using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Internal.Log;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    internal partial class Project
    {
        public VersionStamp Version
        {
            get
            {
                return this.projectState.Version;
            }
        }

        public Task<VersionStamp> GetLatestDocumentVersionAsync(CancellationToken cancellationToken)
        {
            return this.projectState.GetLatestDocumentVersionAsync(cancellationToken);
        }

        public Task<VersionStamp> GetDependentVersionAsync(CancellationToken cancellationToken)
        {
            return this.solution.GetDependentVersionAsync(this.Id, cancellationToken);
        }
    }
}