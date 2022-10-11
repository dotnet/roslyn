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

        private sealed class DefaultSupportedChangesService : ISupportedChangesService
        {
            private readonly Workspace _workspace;

            public DefaultSupportedChangesService(Workspace workspace)
            {
                _workspace = workspace;
            }

            public bool CanApplyChange(ApplyChangesKind kind)
                => _workspace.CanApplyChange(kind);

            public bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, Project project)
                => _workspace.CanApplyCompilationOptionChange(oldOptions, newOptions, project);

            public bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project)
                => _workspace.CanApplyParseOptionChange(oldOptions, newOptions, project);
        }
    }

    internal static class SupportedChangesServiceExtensions
    {
        public static bool CanApplyChange(this Solution solution, ApplyChangesKind kind)
            => solution.Services.GetRequiredService<ISupportedChangesService>().CanApplyChange(kind);

        public static bool CanApplyParseOptionChange(this Project project, ParseOptions oldOptions, ParseOptions newOptions)
            => project.Solution.Services.GetRequiredService<ISupportedChangesService>().CanApplyParseOptionChange(oldOptions, newOptions, project);
    }
}
