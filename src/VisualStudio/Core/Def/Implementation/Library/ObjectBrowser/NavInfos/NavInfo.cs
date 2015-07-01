// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.NavInfos
{
    internal class NavInfo : IVsNavInfo, IVsNavInfo2
    {
        private readonly NavInfoList _navInfo;
        private readonly NavInfoList _canonicalNavInfo;
        private readonly __SymbolToolLanguage _preferredLanguage;

        private ImmutableArray<IVsNavInfoNode> _canonicalNodes;
        private ImmutableArray<IVsNavInfoNode> _objectBrowserFlatNodes;
        private ImmutableArray<IVsNavInfoNode> _classViewFlatNodes;

        public NavInfo(
            Guid libraryGuid,
            __SymbolToolLanguage preferredLanguage,
            string libraryName,
            string metadataProjectItem = null,
            string namespaceName = null,
            string className = null,
            string memberName = null)
        {
            metadataProjectItem = metadataProjectItem ?? string.Empty;
            namespaceName = namespaceName ?? string.Empty;
            className = className ?? string.Empty;
            memberName = memberName ?? string.Empty;

            _preferredLanguage = preferredLanguage;

            _navInfo = new NavInfoList(libraryGuid, libraryName, metadataProjectItem, namespaceName, className, memberName, expandNames: false);
            _canonicalNavInfo = new NavInfoList(libraryGuid, libraryName, metadataProjectItem, namespaceName, className, memberName, expandNames: true);
        }

        public int EnumCanonicalNodes(out IVsEnumNavInfoNodes ppEnum)
        {
            return GetEnum(_canonicalNavInfo, false, true, ref _canonicalNodes, out ppEnum);
        }

        public int EnumPresentationNodes(uint dwFlags, out IVsEnumNavInfoNodes ppEnum)
        {
            if (dwFlags == (uint)_LIB_LISTFLAGS.LLF_NONE)
            {
                // NavInfo for Object Browser
                return GetEnum(_navInfo, true, false, ref _objectBrowserFlatNodes, out ppEnum);
            }
            else
            {
                // NavInfo for Class View
                return GetEnum(_navInfo, false, false, ref _classViewFlatNodes, out ppEnum);
            }
        }

        private int GetEnum(NavInfoList navInfoList, bool isObjectBrowser, bool isCanonical, ref ImmutableArray<IVsNavInfoNode> nodeList, out IVsEnumNavInfoNodes ppEnum)
        {
            if (nodeList == null)
            {
                var builder = ImmutableArray.CreateBuilder<IVsNavInfoNode>();
                FillNodeList(navInfoList, isObjectBrowser, isCanonical, builder);
                nodeList = builder.ToImmutable();
            }

            ppEnum = new EnumNavInfoNodes(nodeList);
            return VSConstants.S_OK;
        }

        private void FillNodeList(NavInfoList navInfoList, bool isObjectBrowser, bool isCanonical, ImmutableArray<IVsNavInfoNode>.Builder builder)
        {
            var index = 0;

            // In some cases, Class View presentation NavInfo objects will have extra nodes (LLT_PACKAGE & LLT_HIERARCHY) up front.
            // When this NavInfo is consumed by Object Browser (for 'Browse to Definition'), we need to skip first two nodes
            if (isObjectBrowser && !isCanonical)
            {
                if (navInfoList.Count >= 2 && navInfoList[1].ListType == (uint)_LIB_LISTTYPE.LLT_HIERARCHY)
                {
                    index = 2;
                }
            }

            while (index < navInfoList.Count)
            {
                if (!isCanonical || navInfoList[index].ListType != (uint)_LIB_LISTTYPE.LLT_HIERARCHY)
                {
                    builder.Add(navInfoList[index]);
                }

                index++;
            }
        }

        public int GetLibGuid(out Guid pGuid)
        {
            pGuid = _navInfo.LibraryGuid;
            return VSConstants.S_OK;
        }

        public void GetPreferredLanguage(out uint pLanguage)
        {
            pLanguage = (uint)_preferredLanguage;
        }

        public int GetSymbolType(out uint pdwType)
        {
            pdwType = _navInfo.Type;
            return VSConstants.S_OK;
        }
    }
}
