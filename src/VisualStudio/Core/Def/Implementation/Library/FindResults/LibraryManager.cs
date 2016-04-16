// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    [Guid(Guids.RoslynLibraryIdString)]
    internal partial class LibraryManager : AbstractLibraryManager
    {
        public LibraryManager(IServiceProvider serviceProvider)
            : base(Guids.RoslynLibraryId, serviceProvider)
        {
        }

        public override uint GetLibraryFlags()
        {
            return (uint)_LIB_FLAGS2.LF_SUPPORTSLISTREFERENCES;
        }

        protected override IVsSimpleObjectList2 GetList(uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch)
        {
            switch (listType)
            {
                case (uint)_LIB_LISTTYPE.LLT_HIERARCHY:
                    if (IsSymbolObjectList((_LIB_LISTFLAGS)flags, pobSrch))
                    {
                        return ((NavInfo)pobSrch[0].pIVsNavInfo).CreateObjectList();
                    }

                    break;
            }

            return null;
        }

        private bool IsSymbolObjectList(_LIB_LISTFLAGS flags, VSOBSEARCHCRITERIA2[] pobSrch)
        {
            return
                (flags & _LIB_LISTFLAGS.LLF_USESEARCHFILTER) != 0 &&
                pobSrch != null &&
                pobSrch.Length == 1 &&
                (pobSrch[0].grfOptions & (uint)_VSOBSEARCHOPTIONS2.VSOBSO_LISTREFERENCES) != 0 &&
                pobSrch[0].pIVsNavInfo is NavInfo;
        }

        protected override uint GetSupportedCategoryFields(uint category)
        {
            switch (category)
            {
                case (uint)LIB_CATEGORY.LC_LISTTYPE:
                    return (uint)_LIB_LISTTYPE.LLT_HIERARCHY;
            }

            return 0;
        }

        protected override uint GetUpdateCounter()
        {
            return 0;
        }

        private void PresentObjectList(string title, ObjectList objectList)
        {
            var navInfo = new NavInfo(objectList);
            var findSymbol = (IVsFindSymbol)this.ServiceProvider.GetService(typeof(SVsObjectSearch));
            var searchCriteria = new VSOBSEARCHCRITERIA2()
            {
                dwCustom = 0,
                eSrchType = VSOBSEARCHTYPE.SO_ENTIREWORD,
                grfOptions = (uint)_VSOBSEARCHOPTIONS2.VSOBSO_LISTREFERENCES | (uint)_VSOBSEARCHOPTIONS.VSOBSO_CASESENSITIVE,
                pIVsNavInfo = navInfo,
                szName = title,
            };

            var criteria = new[] { searchCriteria };
            var hresult = findSymbol.DoSearch(Guids.RoslynLibraryId, criteria);

            ErrorHandler.ThrowOnFailure(hresult);
        }

        private bool IsValidSourceLocation(Location location, Solution solution)
        {
            if (!location.IsInSource)
            {
                return false;
            }

            var document = solution.GetDocument(location.SourceTree);
            return IsValidSourceLocation(document, location.SourceSpan);
        }

        private bool IsValidSourceLocation(Document document, TextSpan sourceSpan)
        {
            if (document == null)
            {
                return false;
            }

            var solution = document.Project.Solution;
            var documentNavigationService = solution.Workspace.Services.GetService<IDocumentNavigationService>();
            return documentNavigationService.CanNavigateToSpan(solution.Workspace, document.Id, sourceSpan);
        }
    }
}
