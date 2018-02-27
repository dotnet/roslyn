// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class ObjectList : AbstractObjectList<LibraryManager>
    {
        private readonly IList<AbstractTreeItem> _items;

        public ObjectList(IList<AbstractTreeItem> items, LibraryManager manager)
            : base(manager)
        {
            _items = items;
        }

        protected override bool CanGoToSource(uint index, VSOBJGOTOSRCTYPE srcType)
        {
            var item = _items[(int)index];

            switch (srcType)
            {
                case VSOBJGOTOSRCTYPE.GS_ANY:
                    return true;

                case VSOBJGOTOSRCTYPE.GS_DEFINITION:
                    return item.CanGoToDefinition();

                case VSOBJGOTOSRCTYPE.GS_REFERENCE:
                    return item.CanGoToReference();
            }

            return false;
        }

        protected override bool TryGetCategoryField(uint index, int category, out uint categoryField)
        {
            if (category == (int)LIB_CATEGORY.LC_LISTTYPE)
            {
                categoryField = (uint)_LIB_LISTTYPE.LLT_HIERARCHY;
                return true;
            }
            else
            {
                categoryField = 0;
                return false;
            }
        }

        protected override void GetDisplayData(uint index, ref VSTREEDISPLAYDATA data)
        {
            var item = _items[(int)index];
            data.Image = item.GlyphIndex;
            data.SelectedImage = item.GlyphIndex;
            data.State |= (uint)_VSTREEDISPLAYSTATE.TDS_FORCESELECT;

            if (item.UseGrayText)
            {
                data.State |= (uint)_VSTREEDISPLAYSTATE.TDS_GRAYTEXT;
            }

            data.ForceSelectStart = item.DisplaySelectionStart;
            data.ForceSelectLength = item.DisplaySelectionLength;
        }

        protected override bool GetExpandable(uint index, uint listTypeExcluded)
        {
            var item = _items[(int)index];
            return (item?.Children?.Any() == true);
        }

        protected override uint GetItemCount()
        {
            return (uint)_items.Count;
        }

        protected override IVsSimpleObjectList2 GetList(uint index, uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch)
        {
            var item = _items[(int)index];
            return (item?.Children?.Any() == true) ? new ObjectList(item.Children, LibraryManager) : null;
        }

        protected override string GetText(uint index, VSTREETEXTOPTIONS tto)
        {
            var item = _items[(int)index];
            switch (tto)
            {
                case VSTREETEXTOPTIONS.TTO_DISPLAYTEXT:
                    return item.DisplayText;

                case VSTREETEXTOPTIONS.TTO_SORTTEXT:
                    return index.ToString("D5");
            }

            return string.Empty;
        }

        protected override string GetTipText(uint index, VSTREETOOLTIPTYPE eTipType)
        {
            var item = _items[(int)index];
            return item.DisplayText;
        }

        protected override int GoToSource(uint index, VSOBJGOTOSRCTYPE srcType)
        {
            if (index >= _items.Count)
            {
                return VSConstants.E_INVALIDARG;
            }

            var item = _items[(int)index];
            return item.GoToSource();
        }

        protected override uint GetUpdateCounter()
        {
            return 0;
        }
    }
}
