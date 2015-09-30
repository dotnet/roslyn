﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.NavInfos;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal class ObjectList : AbstractObjectList<AbstractObjectBrowserLibraryManager>
    {
        private readonly ObjectListKind _kind;
        private readonly ObjectList _parentList;
        private readonly ObjectListItem _parentListItem;
        private readonly uint _flags;
        private readonly AbstractObjectBrowserLibraryManager _manager;
        private readonly ImmutableArray<ObjectListItem> _items;

        public ObjectList(
            ObjectListKind kind,
            uint flags,
            AbstractObjectBrowserLibraryManager manager,
            ImmutableArray<ObjectListItem> items)
            : this(kind, flags, null, null, manager, items)
        {
        }

        public ObjectList(
            ObjectListKind kind,
            uint flags,
            ObjectList parentList,
            ObjectListItem parentListItem,
            AbstractObjectBrowserLibraryManager manager,
            ImmutableArray<ObjectListItem> items)
            : base(manager)
        {
            _kind = kind;
            _flags = flags;
            _parentList = parentList;
            _parentListItem = parentListItem;
            _manager = manager;

            _items = items;

            foreach (var item in _items)
            {
                item.SetParentList(this);
            }
        }

        private bool IsClassView()
        {
            return Helpers.IsClassView(_flags);
        }

        private bool IsFindSymbol()
        {
            return Helpers.IsFindSymbol(_flags);
        }

        private ObjectListItem GetListItem(uint index)
        {
            var listItem = _items[(int)index];
            this.LibraryManager.SetActiveListItem(listItem);
            return listItem;
        }

        private string GetDisplayText(uint index, VSTREETEXTOPTIONS textOptions)
        {
            var listItem = GetListItem(index);

            switch (textOptions)
            {
                case VSTREETEXTOPTIONS.TTO_SORTTEXT:
                case VSTREETEXTOPTIONS.TTO_DISPLAYTEXT:
                    switch (_kind)
                    {
                        case ObjectListKind.BaseTypes:
                        case ObjectListKind.Hierarchy:
                        case ObjectListKind.Members:
                        case ObjectListKind.Namespaces:
                        case ObjectListKind.Projects:
                        case ObjectListKind.References:
                        case ObjectListKind.Types:
                            return listItem.DisplayText;
                    }

                    break;
            }

            return listItem.DisplayText;
        }

        protected override bool CanGoToSource(uint index, VSOBJGOTOSRCTYPE srcType)
        {
            if (srcType == VSOBJGOTOSRCTYPE.GS_DEFINITION)
            {
                var symbolItem = GetListItem(index) as SymbolListItem;
                if (symbolItem != null)
                {
                    return symbolItem.SupportsGoToDefinition;
                }
            }

            return false;
        }

        protected override bool TryGetCapabilities(out uint capabilities)
        {
            capabilities = (uint)_LIB_LISTCAPABILITIES2.LLC_ALLOWELEMENTSEARCH;
            return true;
        }

        private bool TryGetListType(out uint categoryField)
        {
            switch (_kind)
            {
                case ObjectListKind.BaseTypes:
                    categoryField = (uint)_LIB_LISTTYPE.LLT_CLASSES | (uint)_LIB_LISTTYPE.LLT_MEMBERS;
                    return true;

                case ObjectListKind.Hierarchy:
                    var parentKind = this.ParentKind;
                    categoryField = parentKind == ObjectListKind.Types || parentKind == ObjectListKind.BaseTypes
                        ? (uint)_LIB_LISTTYPE.LLT_CLASSES
                        : (uint)_LIB_LISTTYPE.LLT_PACKAGE;

                    return true;

                case ObjectListKind.Members:
                    categoryField = 0;
                    return true;

                case ObjectListKind.Namespaces:
                    categoryField = (uint)_LIB_LISTTYPE.LLT_CLASSES;
                    return true;

                case ObjectListKind.Projects:
                    categoryField = (uint)_LIB_LISTTYPE.LLT_NAMESPACES | (uint)_LIB_LISTTYPE.LLT_CLASSES;

                    if (IsClassView() && this.ParentKind == ObjectListKind.None)
                    {
                        categoryField |= (uint)_LIB_LISTTYPE.LLT_HIERARCHY;
                    }

                    return true;

                case ObjectListKind.References:
                    categoryField = (uint)_LIB_LISTTYPE.LLT_NAMESPACES | (uint)_LIB_LISTTYPE.LLT_CLASSES;
                    return true;

                case ObjectListKind.Types:
                    categoryField = (uint)_LIB_LISTTYPE.LLT_MEMBERS;

                    if ((_flags & (Helpers.LLF_SEARCH_EXPAND_MEMBERS | Helpers.LLF_SEARCH_WITH_EXPANSION)) == 0)
                    {
                        categoryField |= (uint)_LIB_LISTTYPE.LLT_HIERARCHY;
                    }

                    return true;
            }

            categoryField = 0;
            return false;
        }

        private bool TryGetClassAccess(uint index, out uint categoryField)
        {
            var typeListItem = (TypeListItem)GetListItem(index);
            switch (typeListItem.Accessibility)
            {
                case Accessibility.Private:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE;
                    return true;

                case Accessibility.Protected:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PROTECTED;
                    return true;

                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.ProtectedAndInternal:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PACKAGE;
                    return true;

                default:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PUBLIC;
                    return true;
            }
        }

        private bool TryGetClassType(uint index, out uint categoryField)
        {
            var typeListItem = (TypeListItem)GetListItem(index);
            switch (typeListItem.Kind)
            {
                case TypeKind.Interface:
                    categoryField = (uint)_LIBCAT_CLASSTYPE.LCCT_INTERFACE;
                    return true;

                case TypeKind.Struct:
                    categoryField = (uint)_LIBCAT_CLASSTYPE.LCCT_STRUCT;
                    return true;

                case TypeKind.Enum:
                    categoryField = (uint)_LIBCAT_CLASSTYPE.LCCT_ENUM;
                    return true;

                case TypeKind.Delegate:
                    categoryField = (uint)_LIBCAT_CLASSTYPE.LCCT_DELEGATE;
                    return true;

                case TypeKind.Class:
                    categoryField = (uint)_LIBCAT_CLASSTYPE.LCCT_CLASS;
                    return true;

                case TypeKind.Module:
                    categoryField = (uint)_LIBCAT_CLASSTYPE.LCCT_MODULE;
                    return true;

                default:
                    Debug.Fail("Unexpected type kind: " + typeListItem.Kind.ToString());
                    categoryField = 0;
                    return false;
            }
        }

        private bool TryGetMemberInheritance(uint index, out uint categoryField)
        {
            var memberListItem = (MemberListItem)GetListItem(index);
            if (memberListItem.IsInherited)
            {
                categoryField = (uint)_LIBCAT_MEMBERINHERITANCE.LCMI_INHERITED;
            }
            else
            {
                categoryField = (uint)_LIBCAT_MEMBERINHERITANCE.LCMI_IMMEDIATE;
            }

            return true;
        }

        private bool TryGetMemberAccess(uint index, out uint categoryField)
        {
            var memberListItem = (MemberListItem)GetListItem(index);
            switch (memberListItem.Accessibility)
            {
                case Accessibility.Private:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE;
                    return true;

                case Accessibility.Protected:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PROTECTED;
                    return true;

                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.ProtectedAndInternal:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PACKAGE;
                    return true;

                default:
                    categoryField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PUBLIC;
                    return true;
            }
        }

        private bool TryGetMemberType(uint index, out uint categoryField)
        {
            var memberListItem = (MemberListItem)GetListItem(index);
            switch (memberListItem.Kind)
            {
                case MemberKind.Constant:
                    categoryField = (uint)_LIBCAT_MEMBERTYPE.LCMT_CONSTANT;
                    return true;

                case MemberKind.EnumMember:
                    categoryField = (uint)_LIBCAT_MEMBERTYPE.LCMT_ENUMITEM;
                    return true;

                case MemberKind.Event:
                    categoryField = (uint)_LIBCAT_MEMBERTYPE.LCMT_EVENT;
                    return true;

                case MemberKind.Field:
                    categoryField = (uint)_LIBCAT_MEMBERTYPE.LCMT_FIELD;
                    return true;

                case MemberKind.Method:
                    categoryField = (uint)_LIBCAT_MEMBERTYPE.LCMT_METHOD;
                    return true;

                case MemberKind.Operator:
                    categoryField = (uint)_LIBCAT_MEMBERTYPE.LCMT_OPERATOR;
                    return true;

                case MemberKind.Property:
                    categoryField = (uint)_LIBCAT_MEMBERTYPE.LCMT_PROPERTY;
                    return true;

                default:
                    Debug.Fail("Unexpected member kind: " + memberListItem.Kind.ToString());
                    categoryField = 0;
                    return false;
            }
        }

        private bool TryGetPhysicalContainerType(uint index, out uint categoryField)
        {
            var listItem = GetListItem(index);
            switch (listItem.ParentListKind)
            {
                case ObjectListKind.Projects:
                    categoryField = (uint)_LIBCAT_PHYSICALCONTAINERTYPE.LCPT_PROJECT;
                    return true;

                case ObjectListKind.References:
                    categoryField = (uint)_LIBCAT_PHYSICALCONTAINERTYPE.LCPT_PROJECTREFERENCE;
                    return true;
            }

            categoryField = 0;
            return false;
        }

        private bool TryGetVisibility(uint index, out uint categoryField)
        {
            var item = GetListItem(index);
            categoryField = item.IsHidden
                ? (uint)_LIBCAT_VISIBILITY.LCV_HIDDEN
                : (uint)_LIBCAT_VISIBILITY.LCV_VISIBLE;

            return true;
        }

        protected override bool TryGetCategoryField(uint index, int category, out uint categoryField)
        {
            switch (category)
            {
                case (int)LIB_CATEGORY.LC_LISTTYPE:
                    return TryGetListType(out categoryField);

                case (int)_LIB_CATEGORY2.LC_MEMBERINHERITANCE:
                    return TryGetMemberInheritance(index, out categoryField);

                case (int)LIB_CATEGORY.LC_MEMBERACCESS:
                    return TryGetMemberAccess(index, out categoryField);

                case (int)LIB_CATEGORY.LC_MEMBERTYPE:
                    return TryGetMemberType(index, out categoryField);

                case (int)LIB_CATEGORY.LC_CLASSACCESS:
                    return TryGetClassAccess(index, out categoryField);

                case (int)LIB_CATEGORY.LC_CLASSTYPE:
                    return TryGetClassType(index, out categoryField);

                case (int)_LIB_CATEGORY2.LC_HIERARCHYTYPE:
                    if (_kind == ObjectListKind.Hierarchy)
                    {
                        categoryField = this.ParentKind == ObjectListKind.Projects
                            ? (uint)_LIBCAT_HIERARCHYTYPE.LCHT_PROJECTREFERENCES
                            : (uint)_LIBCAT_HIERARCHYTYPE.LCHT_BASESANDINTERFACES;
                    }
                    else
                    {
                        categoryField = (uint)_LIBCAT_HIERARCHYTYPE.LCHT_UNKNOWN;
                    }

                    return true;

                case (int)LIB_CATEGORY.LC_NODETYPE:
                    categoryField = 0;
                    return false;

                case (int)_LIB_CATEGORY2.LC_PHYSICALCONTAINERTYPE:
                    return TryGetPhysicalContainerType(index, out categoryField);

                case (int)LIB_CATEGORY.LC_VISIBILITY:
                    return TryGetVisibility(index, out categoryField);
            }

            throw new NotImplementedException();
        }

        protected override void GetDisplayData(uint index, ref VSTREEDISPLAYDATA data)
        {
            var item = GetListItem(index);
            data.Image = item.GlyphIndex;
            data.SelectedImage = item.GlyphIndex;

            if (item.IsHidden)
            {
                data.State |= (uint)_VSTREEDISPLAYSTATE.TDS_GRAYTEXT;
            }
        }

        protected override bool GetExpandable(uint index, uint listTypeExcluded)
        {
            switch (_kind)
            {
                case ObjectListKind.Hierarchy:
                case ObjectListKind.Namespaces:
                case ObjectListKind.Projects:
                case ObjectListKind.References:
                    return true;

                case ObjectListKind.BaseTypes:
                case ObjectListKind.Types:
                    return IsExpandableType(index);
            }

            return false;
        }

        private bool IsExpandableType(uint index)
        {
            var typeListItem = GetListItem(index) as TypeListItem;
            if (typeListItem == null)
            {
                return false;
            }

            var compilation = typeListItem.GetCompilation(this.LibraryManager.Workspace);
            if (compilation == null)
            {
                return false;
            }

            var typeSymbol = typeListItem.ResolveTypedSymbol(compilation);

            // We never show base types for System.Object
            if (typeSymbol.SpecialType == SpecialType.System_Object)
            {
                return false;
            }

            if (typeSymbol.TypeKind == TypeKind.Module)
            {
                return false;
            }

            if (typeSymbol.TypeKind == TypeKind.Interface && typeSymbol.Interfaces.IsEmpty)
            {
                return false;
            }

            if (typeSymbol.BaseType == null)
            {
                return false;
            }

            return true;
        }

        protected override uint GetItemCount()
        {
            return (uint)_items.Length;
        }

        protected override IVsSimpleObjectList2 GetList(uint index, uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch)
        {
            var listItem = GetListItem(index);

            // We need to do a little massaging of the list type and parent item in a couple of cases.
            switch (_kind)
            {
                case ObjectListKind.Hierarchy:
                    // LLT_USESCLASSES is for displaying base classes and interfaces
                    // LLF_PROJREF is for displaying project references
                    listType = listType == (uint)_LIB_LISTTYPE.LLT_CLASSES
                        ? (uint)_LIB_LISTTYPE.LLT_USESCLASSES
                        : Helpers.LLT_PROJREF;

                    // Use the parent of this list as the parent of the new list.
                    listItem = listItem.ParentList._parentListItem;

                    break;

                case ObjectListKind.BaseTypes:
                    if (listType == (uint)_LIB_LISTTYPE.LLT_CLASSES)
                    {
                        listType = (uint)_LIB_LISTTYPE.LLT_USESCLASSES;
                    }

                    break;
            }

            var listKind = Helpers.ListTypeToObjectListKind(listType);

            if (Helpers.IsFindSymbol(flags))
            {
                var project = this.LibraryManager.GetProject(listItem.ProjectId);
                if (project == null)
                {
                    return null;
                }

                var lookInReferences = (flags & ((uint)_VSOBSEARCHOPTIONS.VSOBSO_LOOKINREFS | (uint)_VSOBSEARCHOPTIONS2.VSOBSO_LISTREFERENCES)) != 0;

                var projectAndAssemblySet = this.LibraryManager.GetAssemblySet(project, lookInReferences, CancellationToken.None);
                return this.LibraryManager.GetSearchList(listKind, flags, pobSrch, projectAndAssemblySet);
            }

            var compilation = listItem.GetCompilation(this.LibraryManager.Workspace);
            if (compilation == null)
            {
                return null;
            }

            switch (listKind)
            {
                case ObjectListKind.Types:
                    return new ObjectList(ObjectListKind.Types, flags, this, listItem, _manager, this.LibraryManager.GetTypeListItems(listItem, compilation));
                case ObjectListKind.Hierarchy:
                    return new ObjectList(ObjectListKind.Hierarchy, flags, this, listItem, _manager, this.LibraryManager.GetFolderListItems(listItem, compilation));
                case ObjectListKind.Namespaces:
                    return new ObjectList(ObjectListKind.Namespaces, flags, this, listItem, _manager, this.LibraryManager.GetNamespaceListItems(listItem, compilation));
                case ObjectListKind.Members:
                    return new ObjectList(ObjectListKind.Members, flags, this, listItem, _manager, this.LibraryManager.GetMemberListItems(listItem, compilation));
                case ObjectListKind.References:
                    return new ObjectList(ObjectListKind.References, flags, this, listItem, _manager, this.LibraryManager.GetReferenceListItems(listItem, compilation));
                case ObjectListKind.BaseTypes:
                    return new ObjectList(ObjectListKind.BaseTypes, flags, this, listItem, _manager, this.LibraryManager.GetBaseTypeListItems(listItem, compilation));
            }

            throw new NotImplementedException();
        }

        protected override object GetBrowseObject(uint index)
        {
            var symbolListItem = GetListItem(index) as SymbolListItem;
            if (symbolListItem != null)
            {
                return this.LibraryManager.Workspace.GetBrowseObject(symbolListItem);
            }

            return base.GetBrowseObject(index);
        }

        protected override bool SupportsNavInfo
        {
            get { return true; }
        }

        protected override IVsNavInfo GetNavInfo(uint index)
        {
            var listItem = GetListItem(index);
            if (listItem == null)
            {
                return null;
            }

            var projectListItem = listItem as ProjectListItem;
            if (projectListItem != null)
            {
                return this.LibraryManager.GetProjectNavInfo(projectListItem.ProjectId);
            }

            var referenceListItem = listItem as ReferenceListItem;
            if (referenceListItem != null)
            {
                return this.LibraryManager.GetReferenceNavInfo(referenceListItem.MetadataReference);
            }

            var symbolListItem = listItem as SymbolListItem;
            if (symbolListItem != null)
            {
                return this.LibraryManager.GetNavInfo(symbolListItem, useExpandedHierarchy: IsClassView());
            }

            return null;
        }

        protected override IVsNavInfoNode GetNavInfoNode(uint index)
        {
            var listItem = GetListItem(index);

            var name = listItem.DisplayText;
            var type = Helpers.ObjectListKindToListType(_kind);

            if (type == (uint)_LIB_LISTTYPE.LLT_USESCLASSES)
            {
                type = (uint)_LIB_LISTTYPE.LLT_CLASSES;
            }
            else if (type == Helpers.LLT_PROJREF)
            {
                type = (uint)_LIB_LISTTYPE.LLT_PACKAGE;
            }

            return new NavInfoNode(name, type);
        }

        protected override bool TryLocateNavInfoNode(IVsNavInfoNode pNavInfoNode, out uint index)
        {
            var itemCount = GetItemCount();
            index = 0xffffffffu;

            string matchName;
            if (ErrorHandler.Failed(pNavInfoNode.get_Name(out matchName)))
            {
                return false;
            }

            uint type;
            if (ErrorHandler.Failed(pNavInfoNode.get_Type(out type)))
            {
                return false;
            }

            var longestMatchedName = string.Empty;

            for (uint i = 0; i < itemCount; i++)
            {
                var name = GetText(i, VSTREETEXTOPTIONS.TTO_DISPLAYTEXT);

                if (_kind == ObjectListKind.Types ||
                    _kind == ObjectListKind.Namespaces ||
                    _kind == ObjectListKind.Members)
                {
                    if (string.Equals(matchName, name, StringComparison.Ordinal))
                    {
                        index = i;
                        break;
                    }
                }
                else
                {
                    if (string.Equals(matchName, name, StringComparison.Ordinal))
                    {
                        index = i;
                        break;
                    }
                    else if (_kind == ObjectListKind.Projects)
                    {
                        if (matchName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (longestMatchedName.Length < name.Length)
                            {
                                longestMatchedName = name;
                                index = i;
                            }
                        }
                    }
                }
            }

            return index != 0xffffffffu;
        }

        protected override bool SupportsDescription
        {
            get { return true; }
        }

        protected override bool TryFillDescription(uint index, _VSOBJDESCOPTIONS options, IVsObjectBrowserDescription3 description)
        {
            var listItem = GetListItem(index);

            return this.LibraryManager.TryFillDescription(listItem, description, options);
        }

        protected override bool TryGetProperty(uint index, _VSOBJLISTELEMPROPID propertyId, out object pvar)
        {
            pvar = null;

            var listItem = GetListItem(index);
            if (listItem == null)
            {
                return false;
            }

            switch (propertyId)
            {
                case _VSOBJLISTELEMPROPID.VSOBJLISTELEMPROPID_FULLNAME:
                    pvar = listItem.FullNameText;
                    return true;

                case _VSOBJLISTELEMPROPID.VSOBJLISTELEMPROPID_HELPKEYWORD:
                    var symbolListItem = listItem as SymbolListItem;
                    if (symbolListItem != null)
                    {
                        var project = this.LibraryManager.Workspace.CurrentSolution.GetProject(symbolListItem.ProjectId);
                        if (project != null)
                        {
                            var compilation = project
                                .GetCompilationAsync(CancellationToken.None)
                                .WaitAndGetResult(CancellationToken.None);

                            var symbol = symbolListItem.ResolveSymbol(compilation);
                            if (symbol != null)
                            {
                                var helpContextService = project.LanguageServices.GetService<IHelpContextService>();

                                pvar = helpContextService.FormatSymbol(symbol);
                                return true;
                            }
                        }
                    }

                    return false;
            }

            return false;
        }

        protected override bool TryCountSourceItems(uint index, out IVsHierarchy hierarchy, out uint itemid, out uint items)
        {
            hierarchy = null;
            itemid = 0;
            items = 0;

            var listItem = GetListItem(index);
            if (listItem == null)
            {
                return false;
            }

            hierarchy = this.LibraryManager.Workspace.GetHierarchy(listItem.ProjectId);

            if (listItem is ProjectListItem)
            {
                itemid = (uint)VSConstants.VSITEMID.Root;
                items = 1;
                return true;
            }
            else if (listItem is SymbolListItem)
            {
                // TODO: Get itemid

                items = 1;
                return true;
            }

            return false;
        }

        protected override string GetText(uint index, VSTREETEXTOPTIONS tto)
        {
            return GetDisplayText(index, tto);
        }

        protected override string GetTipText(uint index, VSTREETOOLTIPTYPE eTipType)
        {
            return null;
        }

        protected override int GoToSource(uint index, VSOBJGOTOSRCTYPE srcType)
        {
            if (srcType == VSOBJGOTOSRCTYPE.GS_DEFINITION)
            {
                var symbolItem = GetListItem(index) as SymbolListItem;
                if (symbolItem != null && symbolItem.SupportsGoToDefinition)
                {
                    var project = this.LibraryManager.Workspace.CurrentSolution.GetProject(symbolItem.ProjectId);
                    var compilation = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    var symbol = symbolItem.ResolveSymbol(compilation);

                    if (this.LibraryManager.Workspace.TryGoToDefinition(symbol, project, CancellationToken.None))
                    {
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        return VSConstants.S_FALSE;
                    }
                }
            }

            return VSConstants.E_FAIL;
        }

        protected override uint GetUpdateCounter()
        {
            switch (_kind)
            {
                case ObjectListKind.Projects:
                case ObjectListKind.References:
                    return _manager.PackageVersion;
                case ObjectListKind.BaseTypes:
                case ObjectListKind.Namespaces:
                case ObjectListKind.Types:
                    return _manager.ClassVersion;
                case ObjectListKind.Members:
                    return _manager.MembersVersion;
                case ObjectListKind.Hierarchy:
                    return _manager.ClassVersion | _manager.MembersVersion;

                default:
                    Debug.Fail("Unsupported object list kind: " + _kind.ToString());
                    throw new InvalidOperationException();
            }
        }

        protected override bool TryGetContextMenu(uint index, out Guid menuGuid, out int menuId)
        {
            if (GetListItem(index) == null)
            {
                menuGuid = Guid.Empty;
                menuId = 0;
                return false;
            }

            menuGuid = VsMenus.guidSHLMainMenu;

            // from vsshlids.h
            const int IDM_VS_CTXT_CV_PROJECT = 0x0432;
            const int IDM_VS_CTXT_CV_ITEM = 0x0433;
            const int IDM_VS_CTXT_CV_GROUPINGFOLDER = 0x0435;
            const int IDM_VS_CTXT_CV_MEMBER = 0x0438;

            switch (_kind)
            {
                case ObjectListKind.Projects:
                    menuId = IDM_VS_CTXT_CV_PROJECT;
                    break;
                case ObjectListKind.Members:
                    menuId = IDM_VS_CTXT_CV_MEMBER;
                    break;
                case ObjectListKind.Hierarchy:
                    menuId = IDM_VS_CTXT_CV_GROUPINGFOLDER;
                    break;
                default:
                    menuId = IDM_VS_CTXT_CV_ITEM;
                    break;
            }

            return true;
        }

        protected override bool SupportsBrowseContainers
        {
            get { return true; }
        }

        protected override bool TryFindBrowseContainer(VSCOMPONENTSELECTORDATA data, out uint index)
        {
            index = 0;
            var count = GetItemCount();

            for (uint i = 0; i < count; i++)
            {
                var listItem = GetListItem(i);

                if (data.type == VSCOMPONENTTYPE.VSCOMPONENTTYPE_ComPlus)
                {
                    var referenceListItem = listItem as ReferenceListItem;
                    if (referenceListItem == null)
                    {
                        continue;
                    }

                    var metadataReference = referenceListItem.MetadataReference as PortableExecutableReference;
                    if (metadataReference == null)
                    {
                        continue;
                    }

                    if (string.Equals(data.bstrFile, metadataReference.FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        return true;
                    }
                }
                else
                {
                    Debug.Assert(data.type == VSCOMPONENTTYPE.VSCOMPONENTTYPE_Project);

                    var hierarchy = this.LibraryManager.Workspace.GetHierarchy(listItem.ProjectId);
                    if (hierarchy == null)
                    {
                        continue;
                    }

                    var vsSolution = this.LibraryManager.ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
                    if (vsSolution == null)
                    {
                        return false;
                    }

                    string projectRef;
                    if (ErrorHandler.Failed(vsSolution.GetProjrefOfProject(hierarchy, out projectRef)))
                    {
                        return false;
                    }

                    if (data.bstrProjRef == projectRef)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool TryGetBrowseContainerData(uint index, ref VSCOMPONENTSELECTORDATA data)
        {
            var listItem = GetListItem(index);

            var projectListItem = listItem as ProjectListItem;
            if (projectListItem != null)
            {
                var hierarchy = this.LibraryManager.Workspace.GetHierarchy(projectListItem.ProjectId);
                if (hierarchy == null)
                {
                    return false;
                }

                var vsSolution = this.LibraryManager.ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
                if (vsSolution == null)
                {
                    return false;
                }

                string projectRef;
                if (ErrorHandler.Failed(vsSolution.GetProjrefOfProject(hierarchy, out projectRef)))
                {
                    return false;
                }

                var project = this.LibraryManager.Workspace.CurrentSolution.GetProject(projectListItem.ProjectId);
                if (project == null)
                {
                    return false;
                }

                data.bstrFile = project.FilePath;
                data.bstrProjRef = projectRef;
                data.bstrTitle = projectListItem.DisplayText;
                data.type = VSCOMPONENTTYPE.VSCOMPONENTTYPE_Project;
            }
            else
            {
                var referenceListItem = listItem as ReferenceListItem;
                if (referenceListItem == null)
                {
                    return false;
                }

                var metadataReference = referenceListItem.MetadataReference as PortableExecutableReference;
                if (metadataReference == null)
                {
                    return false;
                }

                var compilation = referenceListItem.GetCompilation(this.LibraryManager.Workspace);
                if (compilation == null)
                {
                    return false;
                }

                var assemblySymbol = referenceListItem.GetAssembly(compilation);
                if (assemblySymbol == null)
                {
                    return false;
                }

                data.bstrFile = metadataReference.FilePath;
                data.type = VSCOMPONENTTYPE.VSCOMPONENTTYPE_ComPlus;

                var identity = assemblySymbol.Identity;

                data.wFileMajorVersion = (ushort)identity.Version.Major;
                data.wFileMinorVersion = (ushort)identity.Version.Minor;
                data.wFileBuildNumber = (ushort)identity.Version.Build;
                data.wFileRevisionNumber = (ushort)identity.Version.Revision;
            }

            return true;
        }

        public ObjectListKind Kind
        {
            get { return _kind; }
        }

        public ObjectListKind ParentKind
        {
            get
            {
                return _parentList != null
                    ? _parentList.Kind
                    : ObjectListKind.None;
            }
        }

        public ObjectListItem ParentListItem
        {
            get { return _parentListItem; }
        }
    }
}
