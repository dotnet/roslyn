// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Provides TempPE compiler access
    /// </summary>
    internal interface ITempPECompiler
    {
        /// <summary>
        /// Compiles specific files into the TempPE DLL to provide designer support
        /// </summary>
        /// <param name="context">The project context.</param>
        /// <param name="outputFileName">Initial project binary output path.</param>
        /// <param name="filesToInclude">Array of file paths from the project that should be included in the output.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<bool> CompileAsync(IWorkspaceProjectContext context, string outputFileName, string[] filesToInclude, CancellationToken cancellationToken);
    }
}
