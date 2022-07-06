// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Factory to create a project context for a new Workspace project that can be initialized on a background thread.
    /// </summary>
    internal interface IWorkspaceProjectContextFactory
    {
        /// <summary>
        /// Creates and initializes a new Workspace project and returns a <see
        /// cref="IWorkspaceProjectContext"/> to lazily initialize the properties and items for the
        /// project.  This method guarantees that either the project is added (and the returned task
        /// completes) or cancellation is observed and no project is added.
        /// </summary>
        /// <param name="languageName">Project language.</param>
        /// <param name="projectUniqueName">Unique name for the project.</param>
        /// <param name="projectFilePath">Full path to the project file for the project.</param>
        /// <param name="projectGuid">Project guid.</param>
        /// <param name="hierarchy">The IVsHierarchy for the project; this is used to track linked files across multiple projects when determining contexts.</param>
        /// <param name="binOutputPath">Initial project binary output path.</param>
        [Obsolete]
        Task<IWorkspaceProjectContext> CreateProjectContextAsync(
            string languageName,
            string projectUniqueName,
            string projectFilePath,
            Guid projectGuid,
            object? hierarchy,
            string? binOutputPath,
            string? assemblyName,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates and initializes a new project and returns a <see
        /// cref="IWorkspaceProjectContext"/> to lazily initialize the properties and items for the
        /// project.  This method guarantees that either the project is added (and the returned task
        /// completes) or cancellation is observed and no project is added.
        /// </summary>
        /// <param name="data">Providers access to msbuild evaluation data for the project.</param>
        /// <param name="hierarchy">The IVsHierarchy for the project; this is used to track linked files across multiple projects when determining contexts.</param>
        Task<IWorkspaceProjectContext> CreateProjectContextAsync(EvaluationData data, object? hierarchy, CancellationToken cancellationToken);

        /// <summary>
        /// Names of msbuild properties whose values that <see cref="CreateProjectContextAsync(EvaluationData, object?, CancellationToken)"/> will receive via <see cref="EvaluationData"/>.
        /// </summary>
        ImmutableArray<string> EvaluationPropertyNames { get; }

        /// <summary>
        /// Types of msbuild items whose values and metadata that <see cref="CreateProjectContextAsync(EvaluationData, object?, CancellationToken)"/> will receive via <see cref="EvaluationData"/>.
        /// </summary>
        ImmutableArray<string> EvaluationItemTypes { get; }
    }

    internal abstract class EvaluationData
    {
        public abstract Guid ProjectGuid { get; }

        /// <summary>
        /// Unique across the entire solution for the life of the solution. 
        /// Includes full path of the project, the GUID of project and the name of the config.
        /// 
        /// This will be unique across regardless of whether projects are added or renamed 
        /// to match this project's original name. We include file path to make debugging easier.
        /// 
        /// For example:
        ///      C:\Project\Project.csproj (Debug;AnyCPU {72B509BD-C502-4707-ADFD-E2D43867CF45})
        ///      C:\Project\MultiTarget.csproj (Debug;AnyCPU;net45 {72B509BD-C502-4707-ADFD-E2D43867CF45})
        /// </summary>
        public abstract string ProjectUniqueName { get; }

        public abstract string LanguageName { get; }

        /// <summary>
        /// Returns the value of property of the specified <paramref name="name"/>.
        /// </summary>
        /// <returns>
        /// Returns null if the property is not set or the name is not listed in <see cref="IWorkspaceProjectContextFactory.EvaluationPropertyNames"/>.
        /// </returns>
        public abstract string? GetPropertyValue(string name);

        /// <summary>
        /// Returns all items of the specified <paramref name="itemType"/>.
        /// </summary>
        /// <returns>
        /// Returns empty if no items of the specified type are defined or the type is not listed in <see cref="IWorkspaceProjectContextFactory.EvaluationItemTypes"/>.
        /// </returns>
        public abstract IEnumerable<EvaluationItem> GetItems(string itemType);

        public string GetRequiredPropertyValue(string name)
        {
            var value = GetPropertyValue(name);

            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Property '{name}' is required.");

            return value!;
        }
    }

    internal abstract class EvaluationItemMetadata
    {
        public abstract string? GetMetadataValue(string name);
    }

    internal readonly record struct EvaluationItem(string Value, EvaluationItemMetadata? Metadata);
}
