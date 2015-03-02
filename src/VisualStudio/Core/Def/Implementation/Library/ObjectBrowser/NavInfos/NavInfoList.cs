// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.NavInfos
{
    internal class NavInfoList
    {
        private readonly Guid _libraryGuid;
        private readonly string _libraryName;
        private readonly ImmutableArray<NavInfoNode> _nodes;

        public NavInfoList(Guid libraryGuid, string libraryName, string referencedProjectName, string namespaceName, string className, string memberName, bool expandNames)
        {
            _libraryGuid = libraryGuid;
            _libraryName = libraryName;

            var builder = ImmutableArray.CreateBuilder<NavInfoNode>();

            // This is a special node which gets added if the created NavInfo object is for a referenced type in CV.
            if (!string.IsNullOrEmpty(referencedProjectName))
            {
                AddNodes(builder, referencedProjectName, (uint)_LIB_LISTTYPE.LLT_PACKAGE, null);
                AddNodes(builder, ServicesVSResources.Library_ProjectReferences, (uint)_LIB_LISTTYPE.LLT_HIERARCHY, null);
            }

            AddNodes(builder, libraryName, (uint)_LIB_LISTTYPE.LLT_PACKAGE, null);

            var separator = expandNames ? "." : null;

            AddNodes(builder, namespaceName, (uint)_LIB_LISTTYPE.LLT_NAMESPACES, separator);
            AddNodes(builder, className, (uint)_LIB_LISTTYPE.LLT_CLASSES, separator);
            AddNodes(builder, memberName, (uint)_LIB_LISTTYPE.LLT_MEMBERS, null);

            _nodes = builder.ToImmutable();
        }

        private void AddNodes(ImmutableArray<NavInfoNode>.Builder builder, string name, uint type, string separator)
        {
            if (name == null)
            {
                return;
            }

            if (separator != null)
            {
                var start = 0;
                var separatorPos = name.IndexOf(separator, start, StringComparison.Ordinal);

                while (separatorPos >= 0)
                {
                    AddNode(builder, name.Substring(start, separatorPos - start), type);
                    start = separatorPos + separator.Length;
                    separatorPos = name.IndexOf(separator, start, StringComparison.Ordinal);
                }

                if (start < name.Length)
                {
                    AddNode(builder, name.Substring(start), type);
                }
            }
            else
            {
                AddNode(builder, name, type);
            }
        }

        private void AddNode(ImmutableArray<NavInfoNode>.Builder builder, string name, uint type)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            builder.Add(new NavInfoNode(name, type));
        }

        public Guid LibraryGuid
        {
            get { return _libraryGuid; }
        }

        public string Name
        {
            get
            {
                var node = _nodes.LastOrDefault();

                return node != null
                    ? node.Name
                    : null;
            }
        }

        public uint Type
        {
            get
            {
                var node = _nodes.LastOrDefault();

                return node != null
                    ? node.ListType
                    : 0;
            }
        }

        public int Count
        {
            get { return _nodes.Length; }
        }

        public NavInfoNode this[int index]
        {
            get { return _nodes[index]; }
        }
    }
}
