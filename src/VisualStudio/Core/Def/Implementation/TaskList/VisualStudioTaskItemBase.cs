// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    internal class VisualStudioTaskItemBase<T> : IVsErrorItem, IVsTaskItem where T : class, ITaskItem
    {
        protected readonly VSTASKCATEGORY ItemCategory;

        internal readonly T Info;

        protected VisualStudioTaskItemBase(VSTASKCATEGORY category, T taskItem)
        {
            this.ItemCategory = category;
            this.Info = taskItem;
        }

        protected ContainedDocument GetContainedDocumentFromWorkspace()
        {
            var visualStudioWorkspace = this.Info.Workspace as VisualStudioWorkspaceImpl;
            if (visualStudioWorkspace == null)
            {
                return null;
            }

            return visualStudioWorkspace.GetHostDocument(this.Info.DocumentId) as ContainedDocument;
        }

        /// <summary>
        /// Gets the display location of the tasklist item. This is the same as the navigation 
        /// location except for Venus which must have their original
        /// unmapped line numbers mapped through its buffer coordinator.
        /// </summary>
        protected VsTextSpan GetDisplayLocation()
        {
            var containedDocument = GetContainedDocumentFromWorkspace();
            if (containedDocument != null)
            {
                var displayLocation = new VsTextSpan()
                {
                    iStartLine = this.Info.OriginalLine,
                    iStartIndex = this.Info.OriginalColumn,
                    iEndLine = this.Info.OriginalLine,
                    iEndIndex = this.Info.OriginalColumn
                };

                var containedLanguage = containedDocument.ContainedLanguage;
                var bufferCoordinator = containedLanguage.BufferCoordinator;
                var containedLanguageHost = containedLanguage.ContainedLanguageHost;

                var mappedLocation = new VsTextSpan[1];

                if (VSConstants.S_OK == bufferCoordinator.MapSecondaryToPrimarySpan(displayLocation, mappedLocation))
                {
                    return mappedLocation[0];
                }
                else if (containedLanguageHost != null && VSConstants.S_OK == containedLanguageHost.GetNearestVisibleToken(displayLocation, mappedLocation))
                {
                    return mappedLocation[0];
                }
            }

            return GetMappedLocation();
        }

        /// <summary>
        /// Gets the location to be used when navigating to the item. This is the same 
        /// as the display location except for Venus which must use their
        /// original unmapped location as the navigation location so that it can be 
        /// translated correctly during navigation.
        /// </summary>
        protected VsTextSpan GetNavigationLocation()
        {
            var containedDocument = GetContainedDocumentFromWorkspace();
            if (containedDocument != null)
            {
                return new VsTextSpan()
                {
                    iStartLine = this.Info.OriginalLine,
                    iStartIndex = this.Info.OriginalColumn,
                    iEndLine = this.Info.OriginalLine,
                    iEndIndex = this.Info.OriginalColumn
                };
            }

            return GetMappedLocation();
        }

        private VsTextSpan GetMappedLocation()
        {
            if (this.Info.DocumentId != null)
            {
                return new VsTextSpan
                {
                    iStartLine = this.Info.MappedLine,
                    iStartIndex = this.Info.MappedColumn,
                    iEndLine = this.Info.MappedLine,
                    iEndIndex = this.Info.MappedColumn
                };
            }

            return new VsTextSpan();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as VisualStudioTaskItemBase<T>);
        }

        protected bool Equals(VisualStudioTaskItemBase<T> other)
        {
            if (this == other)
            {
                return true;
            }

            if (this.Info == other.Info)
            {
                return true;
            }

            if (this.Info.DocumentId != null && other.Info.DocumentId != null)
            {
                return
                    this.ItemCategory == other.ItemCategory &&
                    this.Info.DocumentId == other.Info.DocumentId &&
                    this.Info.Message == other.Info.Message &&
                    this.Info.OriginalColumn == other.Info.OriginalColumn &&
                    this.Info.OriginalLine == other.Info.OriginalLine &&
                    this.Info.MappedColumn == other.Info.MappedColumn &&
                    this.Info.MappedLine == other.Info.MappedLine &&
                    this.Info.MappedFilePath == other.Info.MappedFilePath;
            }

            return
                    this.ItemCategory == other.ItemCategory &&
                    this.Info.DocumentId == other.Info.DocumentId &&
                    this.Info.Message == other.Info.Message &&
                    this.Info.MappedFilePath == other.Info.MappedFilePath;
        }

        public override int GetHashCode()
        {
            if (this.Info.DocumentId != null)
            {
                return
                    Hash.Combine((int)this.ItemCategory,
                    Hash.Combine(this.Info.DocumentId,
                    Hash.Combine(this.Info.Message,
                    Hash.Combine(this.Info.MappedFilePath,
                    Hash.Combine(this.Info.OriginalColumn,
                    Hash.Combine(this.Info.OriginalLine,
                    Hash.Combine(this.Info.MappedColumn,
                    Hash.Combine(this.Info.MappedLine, 0))))))));
            }

            return
                Hash.Combine((int)this.ItemCategory,
                Hash.Combine(this.Info.DocumentId,
                Hash.Combine(this.Info.Message,
                Hash.Combine(this.Info.MappedFilePath, 0))));
        }

        public virtual int CanDelete(out int pfCanDelete)
        {
            pfCanDelete = 0;
            return VSConstants.S_OK;
        }

        public virtual int GetCategory(out uint pCategory)
        {
            pCategory = (uint)TaskErrorCategory.Error;
            return VSConstants.S_OK;
        }

        public virtual int ImageListIndex(out int pIndex)
        {
            pIndex = (int)_vstaskbitmap.BMP_COMPILE;
            return VSConstants.E_NOTIMPL;
        }

        public virtual int IsReadOnly(VSTASKFIELD field, out int pfReadOnly)
        {
            pfReadOnly = 1;
            return VSConstants.S_OK;
        }

        public virtual int HasHelp(out int pfHasHelp)
        {
            pfHasHelp = 0;
            return VSConstants.S_OK;
        }

        public virtual int GetHierarchy(out IVsHierarchy ppProject)
        {
            ppProject = null;
            return VSConstants.S_OK;
        }

        public virtual int NavigateToHelp()
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int OnDeleteTask()
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int OnFilterTask(int fVisible)
        {
            return VSConstants.E_NOTIMPL;
        }

        protected virtual int GetChecked(out int pfChecked)
        {
            pfChecked = 0;
            return VSConstants.S_OK;
        }

        protected virtual int GetPriority(VSTASKPRIORITY[] ptpPriority)
        {
            if (ptpPriority != null)
            {
                ptpPriority[0] = VSTASKPRIORITY.TP_NORMAL;
            }

            return VSConstants.S_OK;
        }

        protected virtual int PutChecked(int fChecked)
        {
            return VSConstants.E_NOTIMPL;
        }

        protected virtual int PutPriority(VSTASKPRIORITY tpPriority)
        {
            return VSConstants.E_NOTIMPL;
        }

        protected virtual int PutText(string bstrName)
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int SubcategoryIndex(out int pIndex)
        {
            pIndex = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int Category(VSTASKCATEGORY[] pCat)
        {
            if (pCat != null)
            {
                pCat[0] = ItemCategory;
            }

            return VSConstants.S_OK;
        }

        public int Line(out int line)
        {
            if (this.Info.DocumentId == null)
            {
                line = 0;
                return VSConstants.E_NOTIMPL;
            }

            var displayLocation = this.GetDisplayLocation();
            line = displayLocation.iStartLine;
            return VSConstants.S_OK;
        }

        public int Column(out int column)
        {
            if (this.Info.DocumentId == null)
            {
                column = 0;
                return VSConstants.E_NOTIMPL;
            }

            var displayLocation = this.GetDisplayLocation();
            column = displayLocation.iStartIndex;
            return VSConstants.S_OK;
        }

        public int Document(out string documentPath)
        {
            if (this.Info.DocumentId == null)
            {
                documentPath = null;
                return VSConstants.E_NOTIMPL;
            }

            // // TODO (bug 904049): the path may be relative and should to be resolved with OriginalFilePath as its base
            documentPath = this.Info.MappedFilePath;
            return VSConstants.S_OK;
        }

        public int NavigateTo()
        {
            using (Logger.LogBlock(FunctionId.TaskList_NavigateTo, CancellationToken.None))
            {
                if (this.Info.DocumentId == null)
                {
                    // Some items do not have a location in a document
                    return VSConstants.E_NOTIMPL;
                }

                // TODO (bug 904049): We should not navigate to the documentId if diagnosticItem.MappedFilePath is available. 
                // We should find the corresponding document if it exists.

                var workspace = this.Info.Workspace;
                var documentId = this.Info.DocumentId;
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    // document could be already removed from the solution
                    return VSConstants.E_NOTIMPL;
                }

                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
                var navigationLocation = this.GetNavigationLocation();

                navigationService.TryNavigateToLineAndOffset(
                    workspace, documentId, navigationLocation.iStartLine, navigationLocation.iStartIndex);
                return VSConstants.S_OK;
            }
        }

        // use explicit interface to workaround style cop complaints
        int IVsTaskItem.get_Text(out string text)
        {
            text = this.Info.Message;
            return VSConstants.S_OK;
        }

        int IVsTaskItem.get_Checked(out int pfChecked)
        {
            return GetChecked(out pfChecked);
        }

        int IVsTaskItem.get_Priority(VSTASKPRIORITY[] ptpPriority)
        {
            return GetPriority(ptpPriority);
        }

        int IVsTaskItem.put_Checked(int fChecked)
        {
            return PutChecked(fChecked);
        }

        int IVsTaskItem.put_Priority(VSTASKPRIORITY tpPriority)
        {
            return PutPriority(tpPriority);
        }

        int IVsTaskItem.put_Text(string bstrName)
        {
            return PutText(bstrName);
        }
    }
}
