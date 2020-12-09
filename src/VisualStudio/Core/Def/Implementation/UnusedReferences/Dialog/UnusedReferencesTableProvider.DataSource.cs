// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog
{
    internal partial class UnusedReferencesTableProvider
    {
        internal class UnusedReferencesDataSource : ITableDataSource
        {
            public string SourceTypeIdentifier => nameof(UnusedReferencesDataSource);

            public string Identifier => nameof(UnusedReferencesDataSource);

            public string? DisplayName => null;

            private ImmutableList<SinkManager> _managers = ImmutableList<SinkManager>.Empty;
            private ImmutableArray<UnusedReferencesEntry> _currentEntries;

            public IDisposable Subscribe(ITableDataSink sink)
            {
                return new SinkManager(this, sink);
            }

            public void ReplaceData(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates)
            {
                _currentEntries = referenceUpdates.Select(update => new UnusedReferencesEntry(project, update)).ToImmutableArray();
                var oldManagers = Volatile.Read(ref _managers);
                for (int i = 0; (i < oldManagers.Count); ++i)
                {
                    oldManagers[i].Sink.AddEntries(_currentEntries);
                }
            }

            internal void AddSinkManager(SinkManager manager)
            {
                var oldManagers = Volatile.Read(ref _managers);
                while (true)
                {
                    var newManagers = oldManagers.Add(manager);
                    var results = Interlocked.CompareExchange(ref _managers, newManagers, oldManagers);
                    if (results == oldManagers)
                        return;

                    oldManagers = results;
                }
            }

            internal void RemoveSinkManager(SinkManager manager)
            {
                var oldManagers = Volatile.Read(ref _managers);
                while (true)
                {
                    var newManagers = oldManagers.Remove(manager);
                    var results = Interlocked.CompareExchange(ref _managers, newManagers, oldManagers);
                    if (results == oldManagers)
                        return;

                    oldManagers = results;
                }
            }

            internal sealed class SinkManager : IDisposable
            {
                internal readonly UnusedReferencesDataSource UnusedReferencesDataSource;
                internal readonly ITableDataSink Sink;

                internal SinkManager(UnusedReferencesDataSource unusedReferencesDataSource, ITableDataSink sink)
                {
                    UnusedReferencesDataSource = Requires.NotNull(unusedReferencesDataSource, nameof(unusedReferencesDataSource));
                    Sink = sink;

                    UnusedReferencesDataSource.AddSinkManager(this);
                }

                public void Dispose()
                {
                    // Called when the person who subscribed to the data source disposes of the cookie (== this object) they were given.
                    UnusedReferencesDataSource.RemoveSinkManager(this);
                }
            }

            internal class UnusedReferencesEntry : ITableEntry
            {
                public Project Project { get; }
                public ReferenceUpdate ReferenceUpdate { get; }

                public object Identity => ReferenceUpdate;

                public UnusedReferencesEntry(Project project, ReferenceUpdate referenceUpdate)
                {
                    Project = project;
                    ReferenceUpdate = referenceUpdate;
                }

                public bool CanSetValue(string keyName)
                {
                    return keyName == UnusedReferencesTableKeyNames.UpdateAction;
                }

                public bool TryGetValue(string keyName, out object? content)
                {
                    switch (keyName)
                    {
                        case UnusedReferencesTableKeyNames.SolutionName:
                            content = Path.GetFileName(Project.Solution.FilePath);
                            return content != null;
                        case UnusedReferencesTableKeyNames.ProjectName:
                            content = Project.Name;
                            return content != null;
                        case UnusedReferencesTableKeyNames.Language:
                            content = Project.Language;
                            return content != null;
                        case UnusedReferencesTableKeyNames.ReferenceType:
                            content = ReferenceUpdate.ReferenceInfo.ReferenceType;
                            return true;
                        case UnusedReferencesTableKeyNames.ReferenceName:
                            content = ReferenceUpdate.ReferenceInfo.ItemSpecification;
                            return content != null;
                        case UnusedReferencesTableKeyNames.UpdateAction:
                            content = ReferenceUpdate.Action;
                            return true;
                    }

                    content = null;
                    return false;
                }

                public bool TrySetValue(string keyName, object content)
                {
                    if (keyName != UnusedReferencesTableKeyNames.UpdateAction || content is not UpdateAction action)
                    {
                        return false;
                    }

                    ReferenceUpdate.Action = action;
                    return true;
                }
            }
        }
    }
}
