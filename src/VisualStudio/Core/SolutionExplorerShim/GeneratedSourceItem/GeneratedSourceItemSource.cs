// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed class GeneratedSourceItemSource : ProjectItemSource
    {
        private readonly GeneratedSourceFolderItem _folder;
        private BulkObservableCollection<GeneratedSourceItem> _items;

        public GeneratedSourceItemSource(GeneratedSourceFolderItem folder) :
            base(folder, folder.Workspace, folder.ProjectId)
        {
            _folder = folder;
        }

        protected override void Update()
        {
            if (_items == null)
            {
                // The set of GeneratedSourceItems hasn't been realized yet. Just signal that HasItems
                // may have changed.

                NotifyPropertyChanged(nameof(HasItems));
                return;
            }

            var project = _folder.GetProject();
            if (project != null)
            {
                var documents = GetDocuments(project);
                if (!documents.SequenceEqual(_items.Select(i => i.Document), DocumentInfoComparer.Instance))
                {
                    _items.BeginBulkOperation();
                    _items.Clear();
                    _items.AddRange(documents.Select(d => new GeneratedSourceItem(d)));
                    _items.EndBulkOperation();

                    NotifyPropertyChanged(nameof(HasItems));
                }
            }
        }

        private static IReadOnlyCollection<DocumentInfo> GetDocuments(Project project)
        {
            var projectState = project.State;
            var builder = ArrayBuilder<DocumentInfo>.GetInstance();
            foreach (var id in projectState.DocumentIds)
            {
                var info = projectState.GetDocumentState(id).Info;
                if (info.IsGenerated)
                {
                    builder.Add(info);
                }
            }
            return builder.ToArray();
        }

        public override bool HasItems
        {
            get
            {
                if (_items != null)
                {
                    return _items.Count > 0;
                }

                var project = _folder.GetProject();
                return (project != null);
            }
        }

        public override IEnumerable Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new BulkObservableCollection<GeneratedSourceItem>();
                    var project = _folder.GetProject();
                    if (project != null)
                    {
                        var documents = GetDocuments(project);
                        _items.AddRange(documents.Select(d => new GeneratedSourceItem(d)));
                    }
                }
                return _items;
            }
        }

        private sealed class GeneratedSourceItem : BaseItem
        {
            internal readonly DocumentInfo Document;

            public GeneratedSourceItem(DocumentInfo document)
                : base(document.Name)
            {
                Document = document;
            }

            public override ImageMoniker IconMoniker
            {
                get { return KnownMonikers.CSClassFile; } // TODO: Language-specific
            }

            public override ImageMoniker ExpandedIconMoniker
            {
                get { return IconMoniker; }
            }
        }

        private sealed class DocumentInfoComparer : IEqualityComparer<DocumentInfo>
        {
            internal static readonly DocumentInfoComparer Instance = new DocumentInfoComparer();

            public bool Equals(DocumentInfo x, DocumentInfo y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(DocumentInfo obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}
