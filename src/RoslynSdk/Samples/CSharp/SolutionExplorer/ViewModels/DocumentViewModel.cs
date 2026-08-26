// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class DocumentViewModel : HierarchyItemViewModel
    {
        public DocumentId DocumentId { get; }

        public DocumentViewModel(Workspace workspace, DocumentId documentId)
            : base(workspace, isExpanded: false)
        {
            DocumentId = documentId;
        }

        private Document GetDocument()
            => Workspace.CurrentSolution.GetDocument(DocumentId);

        protected override string GetDisplayName()
            => GetDocument().Name;

        public string Language
            => GetDocument().Project.Language;

        public async Task<string> GetSourceTextAsync()
        {
            Microsoft.CodeAnalysis.Text.SourceText text = await GetDocument().GetTextAsync();
            return text.ToString();
        }

        public override int CompareTo(HierarchyItemViewModel other)
        {
            if (other is FolderViewModel || other is ReferencesFolderViewModel)
            {
                return 1;
            }

            return base.CompareTo(other);
        }
    }
}
