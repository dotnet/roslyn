// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Shared state common to all code model objects.
    /// </summary>
    internal sealed class CodeModelState
    {
        public IServiceProvider ServiceProvider { get; }
        public ICodeModelService CodeModelService { get; }
        public ISyntaxFactsService SyntaxFactsService { get; }
        public ICodeGenerationService CodeGenerator { get; }
        public VisualStudioWorkspace Workspace { get; }

        public CodeModelState(
            IServiceProvider serviceProvider,
            HostLanguageServices languageServices,
            VisualStudioWorkspace workspace)
        {
            Debug.Assert(serviceProvider != null);
            Debug.Assert(languageServices != null);
            Debug.Assert(workspace != null);

            this.ServiceProvider = serviceProvider;
            this.CodeModelService = languageServices.GetService<ICodeModelService>();
            this.SyntaxFactsService = languageServices.GetService<ISyntaxFactsService>();
            this.CodeGenerator = languageServices.GetService<ICodeGenerationService>();
            this.Workspace = workspace;
        }
    }
}
