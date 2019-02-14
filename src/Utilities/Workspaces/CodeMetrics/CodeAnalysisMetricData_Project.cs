// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    internal abstract partial class CodeAnalysisMetricData
    {
        public async static Task<CodeAnalysisMetricData> ComputeAsync(Project project, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!project.SupportsCompilation)
            {
                throw new NotSupportedException("Project must support compilation.");
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            return await ComputeAsync(compilation.Assembly, compilation, cancellationToken).ConfigureAwait(false);
        }
    }
}
