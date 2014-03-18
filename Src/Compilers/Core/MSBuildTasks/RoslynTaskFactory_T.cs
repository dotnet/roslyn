// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Base TaskFactory for Vbc and Csc TaskFactories
    /// </summary>
    public abstract class RoslynTaskFactory<TTask> : ITaskFactory
        where TTask : ITask
    {
        private string vsSessionGuid;
        private TaskPropertyInfo[] taskParameters;

        /// <summary>
        /// Gets the type of the task this factory will instantiate.
        /// </summary>
        Type ITaskFactory.TaskType
        {
            get { return typeof(TTask); }
        }

        /// <summary>
        /// Gets the name of the factory.
        /// </summary>
        string ITaskFactory.FactoryName
        {
            get { return typeof(TTask) + "TaskFactory"; }
        }

        /// <summary>
        /// Initializes this factory for instantiating tasks with a particular inline task block.
        /// </summary>
        /// <param name="taskName">Name of the task.</param>
        /// <param name="parameterGroup">The parameter group.</param>
        /// <param name="taskBody">The task body.</param>
        /// <param name="taskFactoryLoggingHost">The task factory logging host.</param>
        /// <returns>A value indicating whether initialization was successful.</returns>
        /// <remarks>
        /// <para>MSBuild engine will call this to initialize the factory. This should initialize the factory enough so that the factory can be asked
        /// whether or not task names can be created by the factory.</para>
        /// <para>
        /// The taskFactoryLoggingHost will log messages in the context of the target where the task is first used.
        /// </para>
        /// </remarks>
        bool ITaskFactory.Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            if (string.Equals(taskName, typeof(TTask).Name, StringComparison.OrdinalIgnoreCase))
            {
                this.taskParameters = parameterGroup.Values.ToArray();
                this.vsSessionGuid = taskBody;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create an instance of the task to be used.
        /// </summary>
        /// <param name="taskFactoryLoggingHost">
        /// The task factory logging host will log messages in the context of the task.
        /// </param>
        /// <returns>
        /// The generated task, or <c>null</c> if the task failed to be created.
        /// </returns>
        ITask ITaskFactory.CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return CreateTask(this.vsSessionGuid);
        }

        /// <summary>
        /// Create an instance of the task to be used.
        /// </summary>
        protected abstract TTask CreateTask(string vsSessionGuid);

        /// <summary>
        /// Cleans up any context or state that may have been built up for a given task.
        /// </summary>
        /// <param name="task">The task to clean up.</param>
        /// <remarks>
        /// For many factories, this method is a no-op.  But some factories may have built up
        /// an AppDomain as part of an individual task instance, and this is their opportunity
        /// to shutdown the AppDomain.
        /// </remarks>
        void ITaskFactory.CleanupTask(ITask task)
        {
            return;
        }

        /// <summary>
        /// Get the descriptions for all the task's parameters.
        /// </summary>
        /// <returns>A non-null array of property descriptions.</returns>
        TaskPropertyInfo[] ITaskFactory.GetTaskParameters()
        {
            PropertyInfo[] infos = typeof(TTask).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var propertyInfos = new TaskPropertyInfo[infos.Length];
            for (int i = 0; i < infos.Length; i++)
            {
                propertyInfos[i] = new TaskPropertyInfo(
                    infos[i].Name,
                    infos[i].PropertyType,
                    infos[i].GetCustomAttributes(typeof(OutputAttribute), false).Length > 0,
                    infos[i].GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0);
            }
            return propertyInfos;
        }
    }
}
