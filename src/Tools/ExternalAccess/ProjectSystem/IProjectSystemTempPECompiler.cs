// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    public interface IProjectSystemTempPECompiler
    {
        Task<bool> CompileAsync(ProjectSystemWorkspaceProjectContextWrapper context, string outputFileName, ISet<string> filesToInclude, CancellationToken cancellationToken);
    }
}
