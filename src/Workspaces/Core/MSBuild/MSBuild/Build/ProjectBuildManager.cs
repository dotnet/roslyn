// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild.Build
{
    internal class ProjectBuildManager
    {
        private static readonly XmlReaderSettings s_xmlReaderSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        private static readonly ImmutableDictionary<string, string> s_defaultGlobalProperties = new Dictionary<string, string>()
        {
            // this will tell msbuild to not build the dependent projects
            { PropertyNames.DesignTimeBuild, bool.TrueString },

            // this will force CoreCompile task to execute even if all inputs and outputs are up to date
            { PropertyNames.BuildingInsideVisualStudio, bool.TrueString },

            { PropertyNames.BuildProjectReferences, bool.FalseString },
            { PropertyNames.BuildingProject, bool.FalseString },

            // retrieve the command-line arguments to the compiler
            { PropertyNames.ProvideCommandLineArgs, bool.TrueString },

            // don't actually run the compiler
            { PropertyNames.SkipCompilerExecution, bool.TrueString },

            { PropertyNames.ContinueOnError, PropertyValues.ErrorAndContinue },

            // this ensures that the parent project's configuration and platform will be used for
            // referenced projects. So, setting Configuration=Release will also cause any project
            // references to also be built with Configuration=Release. This is necessary for getting
            // a more-likely-to-be-correct output path from project references.
            { PropertyNames.ShouldUnsetParentConfigurationAndPlatform, bool.FalseString }
        }.ToImmutableDictionary();

        private readonly ImmutableDictionary<string, string> _additionalGlobalProperties;

        private MSB.Evaluation.ProjectCollection _batchBuildProjectCollection;
        private MSBuildDiagnosticLogger _batchBuildLogger;
        private bool _batchBuildStarted;

        ~ProjectBuildManager()
        {
            if (_batchBuildStarted)
            {
                new InvalidOperationException("ProjectBuilderManager.Stop() not called.");
            }
        }

        public ProjectBuildManager(ImmutableDictionary<string, string> additionalGlobalProperties)
        {
            _additionalGlobalProperties = additionalGlobalProperties ?? ImmutableDictionary<string, string>.Empty;
        }

        private ImmutableDictionary<string, string> AllGlobalProperties
            => s_defaultGlobalProperties.AddRange(_additionalGlobalProperties);

        private static async Task<(MSB.Evaluation.Project project, DiagnosticLog log)> LoadProjectAsync(
            string path, MSB.Evaluation.ProjectCollection projectCollection, CancellationToken cancellationToken)
        {
            var log = new DiagnosticLog();

            try
            {
                var loadedProjects = projectCollection.GetLoadedProjects(path);
                if (loadedProjects != null && loadedProjects.Count > 0)
                {
                    Debug.Assert(loadedProjects.Count == 1);

                    return (loadedProjects.First(), log);
                }

                using (var stream = FileUtilities.OpenAsyncRead(path))
                using (var readStream = await SerializableBytes.CreateReadableStreamAsync(stream, cancellationToken).ConfigureAwait(false))
                using (var xmlReader = XmlReader.Create(readStream, s_xmlReaderSettings))
                {
                    var xml = MSB.Construction.ProjectRootElement.Create(xmlReader, projectCollection);

                    // When constructing a project from an XmlReader, MSBuild cannot determine the project file path.  Setting the
                    // path explicitly is necessary so that the reserved properties like $(MSBuildProjectDirectory) will work.
                    xml.FullPath = path;

                    var project = new MSB.Evaluation.Project(xml, globalProperties: null, toolsVersion: null, projectCollection);

                    return (project, log);
                }
            }
            catch (Exception e)
            {
                log.Add(e, path);
                return (project: null, log);
            }
        }

        public Task<(MSB.Evaluation.Project project, DiagnosticLog log)> LoadProjectAsync(
            string path, CancellationToken cancellationToken)
        {
            if (_batchBuildStarted)
            {
                return LoadProjectAsync(path, _batchBuildProjectCollection, cancellationToken);
            }
            else
            {
                var projectCollection = new MSB.Evaluation.ProjectCollection(AllGlobalProperties);
                try
                {
                    return LoadProjectAsync(path, projectCollection, cancellationToken);
                }
                finally
                {
                    // unload project so collection will release global strings
                    projectCollection.UnloadAllProjects();
                }
            }
        }

        public async Task<string> TryGetOutputFilePathAsync(
            string path, CancellationToken cancellationToken)
        {
            Debug.Assert(_batchBuildStarted);

            // This tries to get the project output path and retrieving the evaluated $(TargetPath) property.

            var (project, _) = await LoadProjectAsync(path, cancellationToken).ConfigureAwait(false);
            return project?.GetPropertyValue(PropertyNames.TargetPath);
        }

        public bool BatchBuildStarted => _batchBuildStarted;

        public void StartBatchBuild(IDictionary<string, string> globalProperties = null)
        {
            if (_batchBuildStarted)
            {
                throw new InvalidOperationException();
            }

            globalProperties = globalProperties ?? ImmutableDictionary<string, string>.Empty;
            var allProperties = s_defaultGlobalProperties.AddRange(globalProperties);
            _batchBuildProjectCollection = new MSB.Evaluation.ProjectCollection(allProperties);

            _batchBuildLogger = new MSBuildDiagnosticLogger()
            {
                Verbosity = MSB.Framework.LoggerVerbosity.Normal
            };

            var buildParameters = new MSB.Execution.BuildParameters(_batchBuildProjectCollection)
            {
                Loggers = new MSB.Framework.ILogger[] { _batchBuildLogger }
            };

            MSB.Execution.BuildManager.DefaultBuildManager.BeginBuild(buildParameters);

            _batchBuildStarted = true;
        }

        public void EndBatchBuild()
        {
            if (!_batchBuildStarted)
            {
                throw new InvalidOperationException();
            }

            MSB.Execution.BuildManager.DefaultBuildManager.EndBuild();

            // unload project so collection will release global strings
            _batchBuildProjectCollection.UnloadAllProjects();
            _batchBuildProjectCollection = null;
            _batchBuildLogger = null;
            _batchBuildStarted = false;
        }

        public Task<MSB.Execution.ProjectInstance> BuildProjectAsync(
            MSB.Evaluation.Project project, DiagnosticLog log, CancellationToken cancellationToken)
        {
            Debug.Assert(_batchBuildStarted);

            var targets = new[] { TargetNames.Compile, TargetNames.CoreCompile };

            return BuildProjectAsync(project, targets, log, cancellationToken);
        }

        private async Task<MSB.Execution.ProjectInstance> BuildProjectAsync(
            MSB.Evaluation.Project project, string[] targets, DiagnosticLog log, CancellationToken cancellationToken)
        {
            // create a project instance to be executed by build engine.
            // The executed project will hold the final model of the project after execution via msbuild.
            var projectInstance = project.CreateProjectInstance();

            // Verify targets
            foreach (var target in targets)
            {
                if (!projectInstance.Targets.ContainsKey(target))
                {
                    log.Add(string.Format(WorkspaceMSBuildResources.Project_does_not_contain_0_target, target), projectInstance.FullPath);
                    return projectInstance;
                }
            }

            _batchBuildLogger.SetProjectAndLog(projectInstance.FullPath, log);

            var buildRequestData = new MSB.Execution.BuildRequestData(projectInstance, targets);

            var result = await BuildAsync(buildRequestData, cancellationToken).ConfigureAwait(false);

            if (result.OverallResult == MSB.Execution.BuildResultCode.Failure)
            {
                if (result.Exception != null)
                {
                    log.Add(result.Exception, projectInstance.FullPath);
                }
            }

            return projectInstance;
        }

        // this lock is static because we are using the default build manager, and there is only one per process
        private static readonly SemaphoreSlim s_buildManagerLock = new SemaphoreSlim(initialCount: 1);

        private async Task<MSB.Execution.BuildResult> BuildAsync(MSB.Execution.BuildRequestData requestData, CancellationToken cancellationToken)
        {
            // only allow one build to use the default build manager at a time
            using (await s_buildManagerLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                return await BuildAsync(MSB.Execution.BuildManager.DefaultBuildManager, requestData, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task<MSB.Execution.BuildResult> BuildAsync(MSB.Execution.BuildManager buildManager, MSB.Execution.BuildRequestData requestData, CancellationToken cancellationToken)
        {
            var taskSource = new TaskCompletionSource<MSB.Execution.BuildResult>();

            // enable cancellation of build
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    // Note: We only ever expect that a single submission is being built,
                    // even though we're calling CancelAllSubmissions(). If MSBuildWorkspace is
                    // ever updated to support parallel builds, we'll likely need to update this code.

                    taskSource.TrySetCanceled();
                    buildManager.CancelAllSubmissions();
                    registration.Dispose();
                });
            }

            // execute build async
            try
            {
                buildManager.PendBuildRequest(requestData).ExecuteAsync(sub =>
                {
                    // when finished
                    try
                    {
                        var result = sub.BuildResult;
                        registration.Dispose();
                        taskSource.TrySetResult(result);
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(e);
                    }
                }, null);
            }
            catch (Exception e)
            {
                taskSource.SetException(e);
            }

            return taskSource.Task;
        }
    }
}
