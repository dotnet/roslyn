// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

            { PropertyNames.ContinueOnError, PropertyValues.ErrorAndContinue }
        }.ToImmutableDictionary();

        private MSB.Evaluation.ProjectCollection _projectCollection;
        private MSBuildDiagnosticLogger _logger;
        private bool _started;

        ~ProjectBuildManager()
        {
            if (_started)
            {
                new InvalidOperationException("ProjectBuilderManager.Stop() not called.");
            }
        }

        private static MSB.Evaluation.Project FindProject(
            string path,
            IDictionary<string, string> globalProperties,
            MSB.Evaluation.ProjectCollection projectCollection,
            CancellationToken cancellationToken)
        {
            var loadedProjects = projectCollection.GetLoadedProjects(path);
            if (loadedProjects == null || loadedProjects.Count == 0)
            {
                return null;
            }

            // We need to walk through all of the projects that have been previously loaded from this path and
            // find the one that has the given set of global properties, plus the default global properties that
            // we load every project with.

            globalProperties = globalProperties ?? ImmutableDictionary<string, string>.Empty;
            var totalGlobalProperties = projectCollection.GlobalProperties.Count + globalProperties.Count;

            foreach (var loadedProject in loadedProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If this project has a different number of global properties than we expect, it's not the
                // one we're looking for.
                if (loadedProject.GlobalProperties.Count != totalGlobalProperties)
                {
                    continue;
                }

                // Since we loaded all of them, the projects in this collection should all have the default
                // global properties (i.e. the ones in _projectCollection.GlobalProperties). So, we just need to
                // check the extra global properties.

                var found = true;
                foreach (var (key, value) in globalProperties)
                {
                    // MSBuild escapes the values of a project's global properties, so we must too.
                    var escapedValue = MSB.Evaluation.ProjectCollection.Escape(value);

                    if (!loadedProject.GlobalProperties.TryGetValue(key, out var actualValue) ||
                        !string.Equals(actualValue, escapedValue, StringComparison.Ordinal))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return loadedProject;
                }
            }

            // We couldn't find a project with this path and the set of global properties we expect.
            return null;
        }

        public async Task<(MSB.Evaluation.Project project, DiagnosticLog log)> LoadProjectAsync(
            string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            var log = new DiagnosticLog();

            try
            {
                var projectCollection = _projectCollection ?? new MSB.Evaluation.ProjectCollection(s_defaultGlobalProperties);

                var project = FindProject(path, globalProperties, projectCollection, cancellationToken);

                if (project == null)
                {
                    using (var stream = FileUtilities.OpenAsyncRead(path))
                    using (var readStream = await SerializableBytes.CreateReadableStreamAsync(stream, cancellationToken).ConfigureAwait(false))
                    using (var xmlReader = XmlReader.Create(readStream, s_xmlReaderSettings))
                    {
                        var xml = MSB.Construction.ProjectRootElement.Create(xmlReader, projectCollection);

                        // When constructing a project from an XmlReader, MSBuild cannot determine the project file path.  Setting the
                        // path explicitly is necessary so that the reserved properties like $(MSBuildProjectDirectory) will work.
                        xml.FullPath = path;

                        project = new MSB.Evaluation.Project(xml, globalProperties, toolsVersion: null, projectCollection);
                    }
                }

                return (project, log);
            }
            catch (Exception e)
            {
                log.Add(e, path);
                return (project: null, log);
            }
        }

        public async Task<string> TryGetOutputFilePathAsync(
            string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            // This tries to get the project output path and retrieving the $(TargetPath) property.

            var (project, _) = await LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false);
            return project?.GetPropertyValue(PropertyNames.TargetPath);
        }

        public void Start()
        {
            if (_started)
            {
                throw new InvalidOperationException();
            }

            _projectCollection = new MSB.Evaluation.ProjectCollection(s_defaultGlobalProperties);

            _logger = new MSBuildDiagnosticLogger()
            {
                Verbosity = MSB.Framework.LoggerVerbosity.Normal
            };

            var buildParameters = new MSB.Execution.BuildParameters(_projectCollection)
            {
                Loggers = new MSB.Framework.ILogger[] { _logger }
            };

            MSB.Execution.BuildManager.DefaultBuildManager.BeginBuild(buildParameters);

            _started = true;
        }

        public void Stop()
        {
            if (!_started)
            {
                throw new InvalidOperationException();
            }

            MSB.Execution.BuildManager.DefaultBuildManager.EndBuild();

            // unload project so collection will release global strings
            _projectCollection.UnloadAllProjects();
            _projectCollection = null;
            _logger = null;
            _started = false;
        }

        public Task<MSB.Execution.ProjectInstance> BuildProjectAsync(
            MSB.Evaluation.Project project, DiagnosticLog log, CancellationToken cancellationToken)
        {
            Debug.Assert(_started);

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

            _logger.SetProjectAndLog(projectInstance.FullPath, log);

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
