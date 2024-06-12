// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Can be acquired from <see cref="Solution.Services"/>, with <see cref="SolutionServices.GetService{ISupportedChangesService}"/>.
    /// </summary>
    public interface ISupportedChangesService : IWorkspaceService
    {
        /// <inheritdoc cref="Workspace.CanApplyChange"/>
        bool CanApplyChange(ApplyChangesKind kind);

        /// <inheritdoc cref="Workspace.CanApplyCompilationOptionChange"/>
        bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, Project project);

        /// <inheritdoc cref="Workspace.CanApplyParseOptionChange"/>
        bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project);
    }

    [ExportWorkspaceServiceFactory(typeof(ISupportedChangesService)), Shared]
    internal sealed class DefaultSupportedChangesServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultSupportedChangesServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new DefaultSupportedChangesService(workspaceServices.Workspace);

        private sealed class DefaultSupportedChangesService(Workspace workspace) : ISupportedChangesService
        {
            public bool CanApplyChange(ApplyChangesKind kind)
                => workspace.CanApplyChange(kind);

            public bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, Project project)
                => workspace.CanApplyCompilationOptionChange(oldOptions, newOptions, project);

            public bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project)
                => workspace.CanApplyParseOptionChange(oldOptions, newOptions, project);
        }
    }
}
