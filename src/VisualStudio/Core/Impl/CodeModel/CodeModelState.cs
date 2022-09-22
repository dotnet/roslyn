// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Shared state common to all code model objects.
    /// </summary>
    internal sealed class CodeModelState
    {
        public IThreadingContext ThreadingContext { get; }
        public IServiceProvider ServiceProvider { get; }
        public ICodeModelService CodeModelService { get; }
        public ISyntaxFactsService SyntaxFactsService { get; }
        public ICodeGenerationService CodeGenerator { get; }
        public VisualStudioWorkspace Workspace { get; }
        public ProjectCodeModelFactory ProjectCodeModelFactory { get; }

        public CodeModelState(
            IThreadingContext threadingContext,
            IServiceProvider serviceProvider,
            Microsoft.CodeAnalysis.Host.LanguageServices languageServices,
            VisualStudioWorkspace workspace,
            ProjectCodeModelFactory projectCodeModelFactory)
        {
            Debug.Assert(threadingContext != null);
            Debug.Assert(serviceProvider != null);
            Debug.Assert(languageServices != null);
            Debug.Assert(workspace != null);

            // ⚠ This code runs on the main thread. Language services accessed here should be preloaded in
            // ProjectCodemodelFactory to avoid blocking MEF operations.
            this.ThreadingContext = threadingContext;
            this.ServiceProvider = serviceProvider;
            this.CodeModelService = languageServices.GetService<ICodeModelService>();
            this.SyntaxFactsService = languageServices.GetService<ISyntaxFactsService>();
            this.CodeGenerator = languageServices.GetService<ICodeGenerationService>();
            this.ProjectCodeModelFactory = projectCodeModelFactory;
            this.Workspace = workspace;
        }
    }
}
