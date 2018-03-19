// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

        private readonly MSB.Evaluation.ProjectCollection _projectCollection;
        private readonly MSBuildDiagnosticLogger _logger;
        private bool _started;

        public ProjectBuildManager()
        {
            var properties = new Dictionary<string, string>()
            {
                { "DesignTimeBuild", "true" }, // this will tell msbuild to not build the dependent projects
                { "BuildingInsideVisualStudio", "true" }, // this will force CoreCompile task to execute even if all inputs and outputs are up to date
                { "BuildProjectReferences", "false" },
                { "BuildingProject", "false" },
                { "ProvideCommandLineArgs", "true" }, // retrieve the command-line arguments to the compiler
                { "SkipCompilerExecution", "true" }, // don't actually run the compiler
                { "ContinueOnError", "ErrorAndContinue" }
            };

            _projectCollection = new MSB.Evaluation.ProjectCollection(properties);

            _logger = new MSBuildDiagnosticLogger()
            {
                Verbosity = MSB.Framework.LoggerVerbosity.Normal
            };
        }

        private MSB.Evaluation.Project FindProject(string path, IDictionary<string, string> globalProperties)
        {
            var loadedProjects = _projectCollection.GetLoadedProjects(path);
            if (loadedProjects == null || loadedProjects.Count == 0)
            {
                return null;
            }

            globalProperties = globalProperties ?? ImmutableDictionary<string, string>.Empty;
            var totalGlobalProperties = _projectCollection.GlobalProperties.Count + globalProperties.Count;

            foreach (var loadedProject in loadedProjects)
            {
                if (loadedProject.GlobalProperties.Count != totalGlobalProperties)
                {
                    continue;
                }

                // All projects in the collection should have the default global properties, so
                // there's no need to check collection.GlobalProperties.

                var found = true;
                foreach (var globalProp in globalProperties)
                {
                    if (!loadedProject.GlobalProperties.Contains(globalProp))
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

            return null;
        }

        public async Task<(MSB.Evaluation.Project project, DiagnosticLog log)> LoadProjectAsync(
            string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            var log = new DiagnosticLog();

            try
            {
                var project = FindProject(path, globalProperties);
                if (project != null)
                {
                    return (project, log);
                }

                using (var stream = await ReadFileAsync(path, cancellationToken).ConfigureAwait(false))
                using (var xmlReader = XmlReader.Create(stream, s_xmlReaderSettings))
                {
                    var xml = MSB.Construction.ProjectRootElement.Create(xmlReader, _projectCollection);

                    // When constructing a project from an XmlReader, MSBuild cannot determine the project file path.  Setting the
                    // path explicitly is necessary so that the reserved properties like $(MSBuildProjectDirectory) will work.
                    xml.FullPath = path;

                    project = new MSB.Evaluation.Project(xml, globalProperties, toolsVersion: null, _projectCollection);

                    return (project, log);
                }
            }
            catch (Exception e)
            {
                log.Add(e, path);
                return (project: null, log);
            }
        }

        private static async Task<MemoryStream> ReadFileAsync(string path, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];

            using (var stream = FileUtilities.OpenAsyncRead(path))
            {
                var totalBytesRead = 0;

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    totalBytesRead += bytesRead;

                    // If we're at the end of the buffer, check the next byte to see if we're done.
                    if (totalBytesRead == buffer.Length)
                    {
                        var nextByte = stream.ReadByte();
                        if (nextByte == -1)
                        {
                            break;
                        }

                        // We're not done yet. Resize the buffer and write the byte we just read.
                        Array.Resize(ref buffer, buffer.Length * 2);
                        buffer[totalBytesRead] = (byte)nextByte;
                        totalBytesRead++;
                    }
                }

                if (totalBytesRead != buffer.Length)
                {
                    Array.Resize(ref buffer, totalBytesRead);
                }
            }

            return new MemoryStream(buffer);
        }

        public async Task<string> TryGetOutputFilePathAsync(
            string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            var (project, _) = await LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false);
            return project?.GetPropertyValue("TargetPath");
        }

        public void Start()
        {
            if (_started)
            {
                throw new InvalidOperationException();
            }

            var buildParameters = new MSB.Execution.BuildParameters(_projectCollection);
            buildParameters.Loggers = new MSB.Framework.ILogger[] { _logger };

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
            _started = false;
        }

        public Task<MSB.Execution.ProjectInstance> BuildProjectAsync(
            MSB.Evaluation.Project project, DiagnosticLog log, CancellationToken cancellationToken)
        {
            var targets = new[] { "Compile", "CoreCompile" };

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
            using (await s_buildManagerLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                return await BuildAsync(MSB.Execution.BuildManager.DefaultBuildManager, requestData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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
                    try
                    {
                        buildManager.CancelAllSubmissions();
                        registration.Dispose();
                    }
                    finally
                    {
                        taskSource.TrySetCanceled();
                    }
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
