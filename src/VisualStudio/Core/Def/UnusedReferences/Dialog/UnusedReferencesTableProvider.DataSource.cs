// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog;

internal partial class UnusedReferencesTableProvider
{
    internal class UnusedReferencesDataSource : ITableDataSource
    {
        public const string Name = nameof(UnusedReferencesDataSource);

        public string SourceTypeIdentifier => Name;
        public string Identifier => Name;
        public string? DisplayName => null;

        private ImmutableList<SinkManager> _managers = ImmutableList<SinkManager>.Empty;
        private ImmutableArray<UnusedReferencesEntry> _currentEntries = ImmutableArray<UnusedReferencesEntry>.Empty;

        public IDisposable Subscribe(ITableDataSink sink)
        {
            return new SinkManager(this, sink);
        }

        public void AddTableData(Solution solution, string projectFilePath, ImmutableArray<ReferenceUpdate> referenceUpdates)
        {
            var solutionName = Path.GetFileName(solution.FilePath);
            var project = solution.Projects.First(project => projectFilePath.Equals(project.FilePath, StringComparison.OrdinalIgnoreCase));
            var entries = referenceUpdates
                .Select(update => new UnusedReferencesEntry(solutionName, project.Name, project.Language, update))
                .ToImmutableArray();

            foreach (var manager in _managers)
            {
                manager.Sink.AddEntries(entries);
            }

            _currentEntries = _currentEntries.AddRange(entries);
        }

        public void RemoveAllTableData()
        {
            foreach (var manager in _managers)
            {
                manager.Sink.RemoveAllEntries();
            }

            _currentEntries = ImmutableArray<UnusedReferencesEntry>.Empty;
        }

        internal void AddSinkManager(SinkManager manager)
        {
            _managers = _managers.Add(manager);

            manager.Sink.AddEntries(_currentEntries);
        }

        internal void RemoveSinkManager(SinkManager manager)
        {
            _managers = _managers.Remove(manager);
        }

        internal sealed class SinkManager : IDisposable
        {
            internal readonly UnusedReferencesDataSource UnusedReferencesDataSource;
            internal readonly ITableDataSink Sink;

            internal SinkManager(UnusedReferencesDataSource unusedReferencesDataSource, ITableDataSink sink)
            {
                UnusedReferencesDataSource = unusedReferencesDataSource;
                Sink = sink;

                UnusedReferencesDataSource.AddSinkManager(this);
            }

            public void Dispose()
            {
                UnusedReferencesDataSource.RemoveSinkManager(this);
            }
        }

        internal class UnusedReferencesEntry : ITableEntry
        {
            public string SolutionName { get; }
            public string ProjectName { get; }
            public string Language { get; }
            public ReferenceUpdate ReferenceUpdate { get; }

            public object Identity => ReferenceUpdate;

            public UnusedReferencesEntry(string solutionName, string projectName, string language, ReferenceUpdate referenceUpdate)
            {
                SolutionName = solutionName;
                ProjectName = projectName;
                Language = language;
                ReferenceUpdate = referenceUpdate;
            }

            public bool TryGetValue(string keyName, out object? content)
            {
                content = null;

                switch (keyName)
                {
                    case UnusedReferencesTableKeyNames.SolutionName:
                        content = SolutionName;
                        break;
                    case UnusedReferencesTableKeyNames.ProjectName:
                        content = ProjectName;
                        break;
                    case UnusedReferencesTableKeyNames.Language:
                        content = Language;
                        break;
                    case UnusedReferencesTableKeyNames.ReferenceType:
                        content = ReferenceUpdate.ReferenceInfo.ReferenceType;
                        break;
                    case UnusedReferencesTableKeyNames.ReferenceName:
                        // For Project and Assembly references, use the file name instead of overwhelming the user with the full path.
                        content = ReferenceUpdate.ReferenceInfo.ReferenceType != ReferenceType.Package
                            ? Path.GetFileName(ReferenceUpdate.ReferenceInfo.ItemSpecification)
                            : ReferenceUpdate.ReferenceInfo.ItemSpecification;
                        break;
                    case UnusedReferencesTableKeyNames.UpdateAction:
                        content = ReferenceUpdate.Action;
                        break;
                }

                return content != null;
            }

            public bool CanSetValue(string keyName)
            {
                return keyName == UnusedReferencesTableKeyNames.UpdateAction;
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
