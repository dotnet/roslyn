// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild.Build
{
    internal class ProjectBuildManager
    {
        private static readonly XmlReaderSettings s_xmlReaderSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        private static readonly ImmutableDictionary<string, string> s_defaultGlobalProperties = new Dictionary<string, string>()
        {
            // this will tell msbuild to not build the dependent projects
            { PropertyNames.DesignTimeBuild, bool.TrueString },

            // this will force CoreCompile task to execute even if all inputs and outputs are up to date
#if NETCOREAPP
            { PropertyNames.NonExistentFile, "__NonExistentSubDir__\\__NonExistentFile__" },
#else
            // Setting `BuildingInsideVisualStudio` indirectly sets NonExistentFile:
            // https://github.com/microsoft/msbuild/blob/ab9b2f36a5ff7a85f842b205d5529e77fdc9d7ab/src/Tasks/Microsoft.Common.CurrentVersion.targets#L3462-L3470
            { PropertyNames.BuildingInsideVisualStudio, bool.TrueString },
#endif

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
        private readonly ILogger? _msbuildLogger;
        private MSB.Evaluation.ProjectCollection? _batchBuildProjectCollection;
        private MSBuildDiagnosticLogger? _batchBuildLogger;

        ~ProjectBuildManager()
        {
            if (BatchBuildStarted)
            {
                throw new InvalidOperationException($"{nameof(ProjectBuildManager)}.{nameof(EndBatchBuild)} not called.");
            }
        }

        public ProjectBuildManager(ImmutableDictionary<string, string> additionalGlobalProperties, ILogger? msbuildLogger = null)
        {
            _additionalGlobalProperties = additionalGlobalProperties ?? ImmutableDictionary<string, string>.Empty;
            _msbuildLogger = msbuildLogger;
        }

        private ImmutableDictionary<string, string> AllGlobalProperties
            => s_defaultGlobalProperties.AddRange(_additionalGlobalProperties);

        private static async Task<(MSB.Evaluation.Project? project, DiagnosticLog log)> LoadProjectAsync(
            string path, MSB.Evaluation.ProjectCollection? projectCollection, CancellationToken cancellationToken)
        {
            var log = new DiagnosticLog();

            try
            {
                var loadedProjects = projectCollection?.GetLoadedProjects(path);
                if (loadedProjects != null && loadedProjects.Count > 0)
                {
                    Debug.Assert(loadedProjects.Count == 1);

                    return (loadedProjects.First(), log);
                }

                using var stream = FileUtilities.OpenAsyncRead(path);
                using var readStream = await SerializableBytes.CreateReadableStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                using var xmlReader = XmlReader.Create(readStream, s_xmlReaderSettings);
                var xml = MSB.Construction.ProjectRootElement.Create(xmlReader, projectCollection);

                // When constructing a project from an XmlReader, MSBuild cannot determine the project file path.  Setting the
                // path explicitly is necessary so that the reserved properties like $(MSBuildProjectDirectory) will work.
                xml.FullPath = path;

                // Roughly matches the VS project load settings for their design time builds.
                var projectLoadSettings = MSB.Evaluation.ProjectLoadSettings.RejectCircularImports
                    | MSB.Evaluation.ProjectLoadSettings.IgnoreEmptyImports
                    | MSB.Evaluation.ProjectLoadSettings.IgnoreMissingImports
                    | MSB.Evaluation.ProjectLoadSettings.IgnoreInvalidImports
                    | MSB.Evaluation.ProjectLoadSettings.DoNotEvaluateElementsWithFalseCondition
                    | MSB.Evaluation.ProjectLoadSettings.FailOnUnresolvedSdk;

                var project = new MSB.Evaluation.Project(
                    xml,
                    globalProperties: null,
                    toolsVersion: null,
                    projectCollection,
                    projectLoadSettings);

                return (project, log);
            }
            catch (Exception e)
            {
                log.Add(e, path);
                return (project: null, log);
            }
        }

        public Task<(MSB.Evaluation.Project? project, DiagnosticLog log)> LoadProjectAsync(
            string path, CancellationToken cancellationToken)
        {
            if (BatchBuildStarted)
            {
                return LoadProjectAsync(path, _batchBuildProjectCollection, cancellationToken);
            }
            else
            {
                var projectCollection = new MSB.Evaluation.ProjectCollection(
                    AllGlobalProperties,
                    _msbuildLogger != null ? ImmutableArray.Create(_msbuildLogger) : ImmutableArray<MSB.Framework.ILogger>.Empty,
                    MSB.Evaluation.ToolsetDefinitionLocations.Default);
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

        public async Task<string?> TryGetOutputFilePathAsync(
            string path, CancellationToken cancellationToken)
        {
            Debug.Assert(BatchBuildStarted);

            // This tries to get the project output path and retrieving the evaluated $(TargetPath) property.

            var (project, _) = await LoadProjectAsync(path, cancellationToken).ConfigureAwait(false);
            return project?.GetPropertyValue(PropertyNames.TargetPath);
        }

        public bool BatchBuildStarted { get; private set; }

        public void StartBatchBuild(IDictionary<string, string>? globalProperties = null)
        {
            if (BatchBuildStarted)
            {
                throw new InvalidOperationException();
            }

            globalProperties ??= ImmutableDictionary<string, string>.Empty;
            var allProperties = s_defaultGlobalProperties.RemoveRange(globalProperties.Keys).AddRange(globalProperties);

            _batchBuildLogger = new MSBuildDiagnosticLogger()
            {
                Verbosity = MSB.Framework.LoggerVerbosity.Normal
            };

            // Pass in the binlog (if any) to the ProjectCollection to ensure evaluation results are included in it.
            //
            // We do not need to include the _batchBuildLogger in the ProjectCollection - it just collects the
            // DiagnosticLog from the build steps, but evaluation already separately reports the DiagnosticLog.
            var loggers = _msbuildLogger is not null
                ? ImmutableArray.Create(_msbuildLogger)
                : ImmutableArray<MSB.Framework.ILogger>.Empty;

            _batchBuildProjectCollection = new MSB.Evaluation.ProjectCollection(allProperties, loggers, MSB.Evaluation.ToolsetDefinitionLocations.Default);

            var buildParameters = new MSB.Execution.BuildParameters(_batchBuildProjectCollection)
            {
                // The loggers are not inherited from the project collection, so specify both the
                // binlog logger and the _batchBuildLogger for the build steps.
                Loggers = loggers.Add(_batchBuildLogger),
                // If we have an additional logger and it's diagnostic, then we need to opt into task inputs globally, or otherwise
                // it won't get any log events. This logic matches https://github.com/dotnet/msbuild/blob/fa6710d2720dcf1230a732a8858ffe71bcdbe110/src/Build/Instance/ProjectInstance.cs#L2365-L2371
                LogTaskInputs = _msbuildLogger is not null && _msbuildLogger.Verbosity == LoggerVerbosity.Diagnostic
            };

            MSB.Execution.BuildManager.DefaultBuildManager.BeginBuild(buildParameters);

            BatchBuildStarted = true;
        }

        public void EndBatchBuild()
        {
            if (!BatchBuildStarted)
            {
                throw new InvalidOperationException();
            }

            MSB.Execution.BuildManager.DefaultBuildManager.EndBuild();

            // unload project so collection will release global strings
            _batchBuildProjectCollection?.UnloadAllProjects();
            _batchBuildProjectCollection = null;
            _batchBuildLogger = null;
            BatchBuildStarted = false;
        }

        public Task<MSB.Execution.ProjectInstance> BuildProjectAsync(
            MSB.Evaluation.Project project, DiagnosticLog log, CancellationToken cancellationToken)
        {
            Debug.Assert(BatchBuildStarted);

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
                    log.Add(string.Format(WorkspaceMSBuildBuildHostResources.Project_does_not_contain_0_target, target), projectInstance.FullPath);
                    return projectInstance;
                }
            }

            _batchBuildLogger?.SetProjectAndLog(projectInstance.FullPath, log);

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
        private static readonly SemaphoreSlim s_buildManagerLock = new(initialCount: 1);

        private static async Task<MSB.Execution.BuildResult> BuildAsync(MSB.Execution.BuildRequestData requestData, CancellationToken cancellationToken)
        {
            // only allow one build to use the default build manager at a time
            using (await s_buildManagerLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                return await BuildAsync(MSB.Execution.BuildManager.DefaultBuildManager, requestData, cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task<MSB.Execution.BuildResult> BuildAsync(MSB.Execution.BuildManager buildManager, MSB.Execution.BuildRequestData requestData, CancellationToken cancellationToken)
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
