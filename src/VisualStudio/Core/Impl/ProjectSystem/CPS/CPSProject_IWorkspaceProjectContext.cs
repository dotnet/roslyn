// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : AbstractProject, IWorkspaceProjectContext
    {
        /// <summary>
        /// Holds the task with continuations to sequentially execute all the foreground affinitized actions on the foreground task scheduler.
        /// More specifically, all the notifications to workspace hosts are executed on the foreground thread. However, the project system might make project state changes
        /// and request notifications to workspace hosts on background thread. So we queue up all the notifications for project state changes onto this task and execute them on the foreground thread.
        /// </summary>
        private Task _foregroundTaskQueue = Task.CompletedTask;

        /// <summary>
        /// Controls access to task queue
        /// </summary>
        private readonly object _queueGate = new object();

        private void ExecuteForegroundAction(Action action)
        {
            if (IsForeground())
            {
                action();
            }
            else
            {
                lock (_queueGate)
                {
                    _foregroundTaskQueue = _foregroundTaskQueue.SafeContinueWith(
                        _ =>
                        {
                            // since execution is now technically asynchronous
                            // only execute action if project is not disconnected and currently being tracked.
                            if (!_disconnected && this.ProjectTracker.ContainsProject(this))
                            {
                                action();
                            }
                        },
                        ForegroundTaskScheduler);
                }
            }
        }

        #region Project properties
        string IWorkspaceProjectContext.DisplayName
        {
            get
            {
                return base.DisplayName;
            }
            set
            {
                ExecuteForegroundAction(() => UpdateProjectDisplayName(value));
            }
        }

        string IWorkspaceProjectContext.ProjectFilePath
        {
            get
            {
                return base.ProjectFilePath;
            }
            set
            {
                ExecuteForegroundAction(() => UpdateProjectFilePath(value));
            }
        }

        Guid IWorkspaceProjectContext.Guid
        {
            get
            {
                return base.Guid;
            }

            set
            {
                base.Guid = value;
            }
        }

        bool IWorkspaceProjectContext.LastDesignTimeBuildSucceeded
        {
            get
            {
                return LastDesignTimeBuildSucceeded;
            }
            set
            {
                ExecuteForegroundAction(() => SetIntellisenseBuildResultAndNotifyWorkspaceHosts(value));
            }
        }

        string IWorkspaceProjectContext.BinOutputPath
        {
            get
            {
                return base.BinOutputPath;
            }
            set
            {
                ExecuteForegroundAction(() => NormalizeAndSetBinOutputPathAndRelatedData(value));
            }
        }

        #endregion

        #region Options
        public void SetOptions(string commandLineForOptions)
        {
            ExecuteForegroundAction(() =>
            {
                var commandLineArguments = SetArgumentsAndUpdateOptions(commandLineForOptions);
                if (commandLineArguments != null)
                {
                    // some languages (e.g., F#) don't expose a command line parser and this might be `null`
                    SetRuleSetFile(commandLineArguments.RuleSetPath);
                    PostSetOptions(commandLineArguments);
                }
            });
        }

        private void PostSetOptions(CommandLineArguments commandLineArguments)
        {
            ExecuteForegroundAction(() =>
            {
                // Invoke SetOutputPathAndRelatedData to update the project obj output path.
                if (commandLineArguments.OutputFileName != null && commandLineArguments.OutputDirectory != null)
                {
                    var objOutputPath = PathUtilities.CombinePathsUnchecked(commandLineArguments.OutputDirectory, commandLineArguments.OutputFileName);
                    SetObjOutputPathAndRelatedData(objOutputPath);
                }
            });
        }
        #endregion

        #region References
        public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
        {
            ExecuteForegroundAction(() =>
            {
                referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
                AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(referencePath, properties);
            });
        }

        public new void RemoveMetadataReference(string referencePath)
        {
            ExecuteForegroundAction(() =>
            {
                referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
                base.RemoveMetadataReference(referencePath);
            });
        }

        public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
        {
            ExecuteForegroundAction(() =>
            {
                var abstractProject = GetAbstractProject(project);
                AddProjectReference(new ProjectReference(abstractProject.Id, properties.Aliases, properties.EmbedInteropTypes));
            });
        }

        public void RemoveProjectReference(IWorkspaceProjectContext project)
        {
            ExecuteForegroundAction(() =>
            {
                var referencedProject = GetAbstractProject(project);
                var projectReference = GetCurrentProjectReferences().Single(p => p.ProjectId == referencedProject.Id);
                RemoveProjectReference(projectReference);
            });
        }

        private AbstractProject GetAbstractProject(IWorkspaceProjectContext project)
        {
            var abstractProject = project as AbstractProject;
            if (abstractProject == null)
            {
                throw new ArgumentException("Unsupported project kind", nameof(project));
            }

            return abstractProject;
        }
        #endregion

        #region Files
        public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            ExecuteForegroundAction(() =>
            {
                AddFile(filePath, sourceCodeKind, _ => isInCurrentContext, _ => folderNames.ToImmutableArrayOrEmpty());
            });
        }

        public void RemoveSourceFile(string filePath)
        {
            ExecuteForegroundAction(() =>
            {
                RemoveFile(filePath);
            });
        }

        public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
        {
            ExecuteForegroundAction(() =>
            {
                AddAdditionalFile(filePath, getIsInCurrentContext: _ => isInCurrentContext);
            });
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            Disconnect();
        }
        #endregion
    }
}
