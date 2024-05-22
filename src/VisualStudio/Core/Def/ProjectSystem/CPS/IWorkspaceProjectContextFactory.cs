// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem;

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
    /// <param name="id">Project guid</param>
    /// <param name="uniqueName">
    /// Unique across the entire solution for the life of the solution. 
    /// This will be unique across regardless of whether projects are added or renamed 
    /// to match this project's original name.
    /// </param>
    /// <param name="data">Provides access to msbuild evaluation data for the project.</param>
    /// <param name="hostObject">The IVsHierarchy for the project; this is used to track linked files across multiple projects when determining contexts.</param>
    /// <exception cref="InvalidOperationException">A required property or item is not present in <see cref="EvaluationData"/> or has invalid value.</exception>
    Task<IWorkspaceProjectContext> CreateProjectContextAsync(Guid id, string uniqueName, string languageName, EvaluationData data, object? hostObject, CancellationToken cancellationToken);

    /// <summary>
    /// Names of msbuild properties whose values <see cref="CreateProjectContextAsync(Guid, string, string, EvaluationData, object?, CancellationToken)"/> will receive via <see cref="EvaluationData"/>.
    /// </summary>
    ImmutableArray<string> EvaluationPropertyNames { get; }

    /// <summary>
    /// Names of msbuild items whose values <see cref="CreateProjectContextAsync(Guid, string, string, EvaluationData, object?, CancellationToken)"/> will receive via <see cref="EvaluationData"/>.
    /// </summary>
    ImmutableArray<string> EvaluationItemNames { get; }
}

internal abstract class EvaluationData
{
    /// <summary>
    /// Returns the value of property of the specified <paramref name="name"/>.
    /// </summary>
    /// <returns>
    /// Returns empty string if the property is not set.
    /// </returns>
    /// <exception cref="InvalidProjectDataException">
    /// The <paramref name="name"/> is not listed in <see cref="IWorkspaceProjectContextFactory.EvaluationPropertyNames"/>
    /// </exception>
    public abstract string GetPropertyValue(string name);

    /// <summary>
    /// Returns the values of items of the specified <paramref name="name"/>.
    /// </summary>
    /// <returns>
    /// Returns empty array if the items are not set.
    /// </returns>
    /// <exception cref="InvalidProjectDataException">
    /// The <paramref name="name"/> is not listed in <see cref="IWorkspaceProjectContextFactory.EvaluationItemNames"/>
    /// </exception>
    public virtual ImmutableArray<string> GetItemValues(string name)
        => ImmutableArray<string>.Empty;

    public string GetRequiredPropertyValue(string name)
    {
        var value = GetPropertyValue(name);

        if (value.IsEmpty())
            throw new InvalidProjectDataException(name, value, $"Property '{name}' is required.");

        return value;
    }

    public string GetRequiredPropertyAbsolutePathValue(string name)
    {
        var value = GetPropertyValue(name);

        if (!PathUtilities.IsAbsolute(value))
            throw new InvalidProjectDataException(name, value, $"Property '{name}' is required to be an absolute path, but the value is '{value}'.");

        return value;
    }
}

internal sealed class InvalidProjectDataException : Exception
{
    public string Name { get; }
    public string Value { get; }

    public InvalidProjectDataException(string name, string value, string message)
        : base(message)
    {
        Name = name;
        Value = value;
    }
}
