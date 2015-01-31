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
        public IServiceProvider ServiceProvider { get; private set; }
        public ICodeModelService CodeModelService { get; private set; }
        public ISyntaxFactsService SyntaxFactsService { get; private set; }
        public ICodeGenerationService CodeGenerator { get; private set; }
        public VisualStudioWorkspace Workspace { get; private set; }

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
