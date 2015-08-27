// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents an ITEMID in an IVsHierarchy
    /// </summary>
    internal struct HierarchyId
    {
        private readonly uint _id;

        public static readonly HierarchyId Selection = new HierarchyId(VSConstants.VSITEMID_SELECTION);
        public static readonly HierarchyId Root = new HierarchyId(VSConstants.VSITEMID_ROOT);
        public static readonly HierarchyId Nil = new HierarchyId(VSConstants.VSITEMID_NIL);
        public static readonly HierarchyId Empty = new HierarchyId(0);

        public HierarchyId(uint id)
        {
            _id = id;
        }

        public uint Id
        {
            get { return _id; }
        }

        public bool IsRoot
        {
            get { return _id == Root.Id; }
        }

        public bool IsSelection
        {
            get { return _id == Selection.Id; }
        }

        public bool IsEmpty
        {
            get { return _id == Empty.Id; }
        }

        public bool IsNilOrEmpty
        {
            get { return IsNil || IsEmpty; }
        }

        public bool IsNil
        {
            get { return _id == Nil.Id; }
        }

        public static implicit operator uint(HierarchyId id)
        {
            return id.Id;
        }

        public static implicit operator HierarchyId(uint id)
        {
            return new HierarchyId(id);
        }
    }

}
