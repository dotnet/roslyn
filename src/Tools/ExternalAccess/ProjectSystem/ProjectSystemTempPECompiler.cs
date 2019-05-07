// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    [Export(typeof(IProjectSystemTempPECompiler))]
    [Shared]
    internal sealed class ProjectSystemTempPECompiler : IProjectSystemTempPECompiler
    {
        private readonly ITempPECompiler _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectSystemTempPECompiler(ITempPECompiler implementation)
        {
            _implementation = implementation;
        }

        public Task<bool> CompileAsync(ProjectSystemWorkspaceProjectContextWrapper context, string outputFileName, ISet<string> filesToInclude, CancellationToken cancellationToken)
            => _implementation.CompileAsync(context.WorkspaceProjectContext, outputFileName, filesToInclude, cancellationToken);
    }
}
