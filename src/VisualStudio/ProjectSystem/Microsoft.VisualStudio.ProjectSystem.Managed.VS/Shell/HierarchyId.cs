// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    ///     Represents an ITEMID in an IVsHierarchy.
    /// </summary>
    internal struct HierarchyId
    {
        private readonly uint _id;

        /// <summary>
        ///     Represents the root of a project hierarchy and is used to identify the entire hierarchy, as opposed 
        ///     to a single item.
        /// </summary>
        public static readonly HierarchyId Root = new HierarchyId(VSConstants.VSITEMID_ROOT);

        /// <summary>
        ///     Represents the currently selected items, which can include the root of the hierarchy.
        /// </summary>
        public static readonly HierarchyId Selection = new HierarchyId(VSConstants.VSITEMID_SELECTION);

        /// <summary>
        ///     Represents the absence of a project item. This value is used when there is no current selection.
        /// </summary>
        public static readonly HierarchyId Nil = new HierarchyId(VSConstants.VSITEMID_NIL);

        /// <summary>
        ///     Represent an empty item.
        /// </summary>
        public static readonly HierarchyId Empty = new HierarchyId(0);

        public HierarchyId(uint id)
        {
            _id = id;
        }

        /// <summary>
        ///     Returns the underlying ITEMID.
        /// </summary>
        public uint Id
        {
            get { return _id; }
        }

        /// <summary>
        ///     Returns a value indicating if the <see cref="HierarchyId"/> represents the root of a project hierarchy
        ///     and is used to identify the entire hierarchy, as opposed to a single item.
        /// </summary>
        public bool IsRoot
        {
            get { return _id == Root.Id; }
        }

        /// <summary>
        ///     Returns a value indicating if the <see cref="HierarchyId"/> represents the currently selected items, 
        ///     which can include the root of the hierarchy.
        /// </summary>
        public bool IsSelection
        {
            get { return _id == Selection.Id; }
        }

        /// <summary>
        ///    Returns a value indicating if the <see cref="HierarchyId"/> is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return _id == Empty.Id; }
        }

        /// <summary>
        ///    Returns a value indicating if the <see cref="HierarchyId"/> is <see cref="IsNil"/> or 
        ///    <see cref="IsEmpty"/>.
        /// </summary>
        public bool IsNilOrEmpty
        {
            get { return IsNil || IsEmpty; }
        }

        /// <summary>
        ///    Returns a value indicating if the <see cref="HierarchyId"/> represents the absence of a project item. 
        ///    This value is used when there is no current selection.
        /// </summary>
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
