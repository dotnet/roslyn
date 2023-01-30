﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(IWorkspaceProjectContextFactory))]
    internal partial class CPSProjectFactory : IWorkspaceProjectContextFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioProjectFactory _projectFactory;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IProjectCodeModelFactory _projectCodeModelFactory;
        private readonly IAsyncServiceProvider _serviceProvider;

        /// <summary>
        /// Solutions containing projects that use older compiler toolset that does not provide a checksum algorithm.
        /// Used only for EnC issue diagnostics.
        /// </summary>
        private ImmutableHashSet<string> _solutionsWithMissingChecksumAlgorithm = ImmutableHashSet<string>.Empty;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CPSProjectFactory(
            IThreadingContext threadingContext,
            VisualStudioProjectFactory projectFactory,
            VisualStudioWorkspaceImpl workspace,
            IProjectCodeModelFactory projectCodeModelFactory,
            SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _projectFactory = projectFactory;
            _workspace = workspace;
            _projectCodeModelFactory = projectCodeModelFactory;
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public ImmutableArray<string> EvaluationPropertyNames
            => BuildPropertyNames.InitialEvaluationPropertyNames;

        public ImmutableArray<string> EvaluationItemNames
            => BuildPropertyNames.InitialEvaluationItemNames;

        // Kept around onyl for integration tests.
        [Obsolete]
        public Task<IWorkspaceProjectContext> CreateProjectContextAsync(
            string languageName,
            string projectUniqueName,
            string? projectFilePath,
            Guid projectGuid,
            object? hierarchy,
            string? binOutputPath,
            string? assemblyName,
            CancellationToken cancellationToken)
        {
            var data = new IntegrationTestEvaluationData(projectFilePath ?? "", projectFilePath ?? "", assemblyName ?? "", binOutputPath ?? "", "SHA256");
            return CreateProjectContextAsync(projectGuid, projectUniqueName, languageName, data, hierarchy, cancellationToken);
        }

        [Obsolete]
        internal sealed class IntegrationTestEvaluationData : EvaluationData
        {
            public string ProjectFilePath { get; }
            public string TargetPath { get; }
            public string AssemblyName { get; }
            public string OutputAssembly { get; }
            public string ChecksumAlgorithm { get; }

            public IntegrationTestEvaluationData(string projectFilePath, string targetPath, string assemblyName, string outputAssembly, string checksumAlgorithm)
            {
                ProjectFilePath = projectFilePath;
                TargetPath = targetPath;
                AssemblyName = assemblyName;
                OutputAssembly = outputAssembly;
                ChecksumAlgorithm = checksumAlgorithm;
            }

            public override string GetPropertyValue(string name)
                => name switch
                {
                    BuildPropertyNames.MSBuildProjectFullPath => ProjectFilePath,
                    BuildPropertyNames.TargetPath => TargetPath,
                    BuildPropertyNames.AssemblyName => AssemblyName,
                    BuildPropertyNames.CommandLineArgsForDesignTimeEvaluation => "-checksumalgorithm:" + ChecksumAlgorithm,
                    _ => throw ExceptionUtilities.UnexpectedValue(name)
                };

            public override ImmutableArray<string> GetItemValues(string name)
                => name switch
                {
                    BuildPropertyNames.IntermediateAssembly => ImmutableArray.Create(OutputAssembly),
                    _ => throw ExceptionUtilities.UnexpectedValue(name)
                };
        }

        public async Task<IWorkspaceProjectContext> CreateProjectContextAsync(Guid id, string uniqueName, string languageName, EvaluationData data, object? hostObject, CancellationToken cancellationToken)
        {
            // Read all required properties from EvaluationData before we start updating anything.

            // Compatibility with older SDKs:
            // If the IDE loads a project that uses an older version of compiler targets or the SDK some msbuild properties/items might not be available
            // (those that were added in a later version). For each property/item we read here that is defined in the compiler targets or the SDK
            // we need to handle its absence, as long as we support that version of the compilers/SDK.

            var projectFilePath = data.GetRequiredPropertyAbsolutePathValue(BuildPropertyNames.MSBuildProjectFullPath);

            var creationInfo = new VisualStudioProjectCreationInfo
            {
                AssemblyName = data.GetPropertyValue(BuildPropertyNames.AssemblyName),
                FilePath = projectFilePath,
                Hierarchy = hostObject as IVsHierarchy,
                ProjectGuid = id,
            };

            string? binOutputPath, objOutputPath, commandLineArgs;
            if (languageName is LanguageNames.CSharp or LanguageNames.VisualBasic)
            {
                binOutputPath = data.GetRequiredPropertyAbsolutePathValue(BuildPropertyNames.TargetPath);
                objOutputPath = GetIntermediateAssemblyPath(data, projectFilePath);

                // Property added in VS 17.4 compiler targets capturing values of LangVersion and DefineConstants.
                // ChecksumAlgorithm value added to the property in 17.5.
                //
                // Impact on Hot Reload: incorrect ChecksumAlgorithm will prevent Hot Reload in detecting changes correctly in certain scenarios.
                // However, given that projects that explicitly set ChecksumAlgorithm to a non-default value are rare and the project system
                // will eventually call us to update the algorithm to the correct value, Hot Reload will likely not be impacted in practice.
                commandLineArgs = data.GetPropertyValue(BuildPropertyNames.CommandLineArgsForDesignTimeEvaluation);

                // Let EnC service known the checksum might not match, in case we need to diagnose related issue.
                if (commandLineArgs.IsEmpty())
                {
                    ImmutableInterlocked.Update(ref _solutionsWithMissingChecksumAlgorithm, static (set, solutionPath) => set.Add(solutionPath), _workspace.CurrentSolution.FilePath ?? "");
                }
            }
            else
            {
                binOutputPath = data.GetPropertyValue(BuildPropertyNames.TargetPath);
                objOutputPath = null;
                commandLineArgs = null;
            }

            var visualStudioProject = await _projectFactory.CreateAndAddToWorkspaceAsync(
                uniqueName, languageName, creationInfo, cancellationToken).ConfigureAwait(false);

            // At this point we've mutated the workspace.  So we're no longer cancellable.
            cancellationToken = CancellationToken.None;

            if (languageName == LanguageNames.FSharp)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var shell = await _serviceProvider.GetServiceAsync<SVsShell, IVsShell7>(_threadingContext.JoinableTaskFactory).ConfigureAwait(true);

                // Force the F# package to load; this is necessary because the F# package listens to WorkspaceChanged to 
                // set up some items, and the F# project system doesn't guarantee that the F# package has been loaded itself
                // so we're caught in the middle doing this.
                var packageId = Guids.FSharpPackageId;
                await shell.LoadPackageAsync(ref packageId);

                await TaskScheduler.Default;
            }

            var project = new CPSProject(visualStudioProject, _workspace, _projectCodeModelFactory, id);

            // Set the properties in a batch; if we set the property directly we'll be taking a synchronous lock here and
            // potentially block up thread pool threads. Doing this in a batch means the global lock will be acquired asynchronously.
            project.StartBatch();

            if (!string.IsNullOrEmpty(commandLineArgs))
            {
                project.SetOptions(commandLineArgs!);
            }

            if (objOutputPath != null)
            {
                project.CompilationOutputAssemblyFilePath = objOutputPath;
            }

            project.BinOutputPath = binOutputPath;

            await project.EndBatchAsync().ConfigureAwait(false);

            return project;
        }

        private static string? GetIntermediateAssemblyPath(EvaluationData data, string projectFilePath)
        {
            const string itemName = BuildPropertyNames.IntermediateAssembly;

            var values = data.GetItemValues(itemName);
            if (values.Length != 1)
            {
                // TODO: Throw once we update integration tests to the latest VS (https://github.com/dotnet/roslyn/issues/65439)
                // var joinedValues = string.Join(";", values);
                // throw new InvalidProjectDataException(itemName, joinedValues, $"Item group '{itemName}' is required to specify a single value: '{joinedValues}'.");
                return null;
            }

            var path = values[0];

            if (!PathUtilities.IsAbsolute(path))
            {
                path = Path.Combine(PathUtilities.GetDirectoryName(projectFilePath), path);
            }

            if (!PathUtilities.IsAbsolute(path))
            {
                throw new InvalidProjectDataException(itemName, values[0], $"Item group '{itemName}' is required to specify an absolute path or a path relative to the directory containing the project: '{values[0]}'.");
            }

            return path;
        }
    }
}
