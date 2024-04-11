// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Mapping from ProjectId to values of type T. Set of possible ProjectID keys are determined at construction.
/// Population of values are done through calls to GetOrAdd.
/// </summary>
/// <typeparam name="T"></typeparam>
internal struct ProjectIdMapping<T>
    where T : class
{
    public static ProjectIdMapping<T> Empty = new([], FrozenDictionary<ProjectId, int>.Empty);

    /// <summary>
    /// Mapping from ProjectId to index in _projects
    /// </summary>
    private readonly FrozenDictionary<ProjectId, int> _projectIdToIndex;

    /// <summary>
    /// Values are created on demand by calls to <see cref="GetOrAdd{TArg}(ProjectId?, Func{ProjectId, TArg, T}, TArg)" />
    /// </summary>
    private readonly T?[] _projects;

    /// <summary>
    /// Domain of ProjectIds for which values may be associated. Set equivalent to <see cref="_projectIdToIndex"/>'s keys.
    /// Compared to input in <see cref="WithProjectIds(IReadOnlyList{ProjectId})"/> to determine if
    /// the existing frozen dictionary can be reused.
    /// </summary>
    private readonly IReadOnlyList<ProjectId> _projectIds;

    private ProjectIdMapping(IReadOnlyList<ProjectId> projectIds, FrozenDictionary<ProjectId, int> projectIdToIndex)
    {
        _projectIds = projectIds;
        _projectIdToIndex = projectIdToIndex;
        _projects = new T?[_projectIds.Count];
    }

    /// <summary>
    /// Returns a new ProjectIdMapping whose keys are limited to the specified project ids.
    /// </summary>
    public readonly ProjectIdMapping<T> WithProjectIds(IReadOnlyList<ProjectId> projectIds)
    {
        var projectIdToIndex = _projectIdToIndex;

        // If the old project ids are not equivalent to the set passed in, then we will
        // build a frozen dictionary from the new project ids.
        var canReuseIndexes = AreEquivalent(projectIds, _projectIds);
        if (!canReuseIndexes)
        {
            using var _ = PooledDictionary<ProjectId, int>.GetInstance(out var dict);

            for (int i = 0, n = projectIds.Count; i < n; i++)
            {
                var projectId = projectIds[i];
                dict[projectId] = i;
            }

            projectIdToIndex = dict.ToFrozenDictionary();
        }

        return new(projectIds, projectIdToIndex);

        static bool AreEquivalent(IReadOnlyList<ProjectId> originalIds, IReadOnlyList<ProjectId> newIds)
        {
            if (object.ReferenceEquals(originalIds, newIds))
                return true;

            if (originalIds.Count != newIds.Count)
                return false;

            return originalIds.SequenceEqual(newIds);
        }
    }

    /// <summary>
    /// Gets or adds a value into the mapping
    /// </summary>
    /// <param name="projectId">The project id key to use</param>
    /// <param name="createValue">Callback yielding the new value for this project id</param>
    /// <param name="arg">Additional argument to pass into callback</param>
    /// <returns>Null if the key is null or not present in the set of project ids this object was 
    /// constructed with. Otherwise, returns the existing or new value associated with the
    /// project id.</returns>
    public T? GetOrAdd<TArg>(ProjectId? projectId, Func<ProjectId, TArg, T> createValue, TArg arg)
    {
        if (projectId is null || !_projectIdToIndex.TryGetValue(projectId, out var projectIndex))
            return null;

        var project = _projects[projectIndex];
        if (project == null)
        {
            // Create the actual project. Note that it's ok if this isn't the project actually used
            // if someone updates _projects at this index before we do.
            project = createValue(projectId, arg);

            if (null != Interlocked.CompareExchange(ref _projects[projectIndex], project, null))
            {
                // If someone else injected themselves into the array at this index before us,
                //  use the project they created.
                project = _projects[projectIndex];
            }
        }

        return project;
    }
}
