// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.Testing
{
    partial class ProjectTreeParser
    {
        private class MutableProjectTree : IProjectTree
        {
            public MutableProjectTree()
            {
                Children = new Collection<MutableProjectTree>();
                Capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Visible = true;
            }

            public Collection<MutableProjectTree> Children
            {
                get;
            }

            public string Caption
            {
                get;
                set;
            }

            public HashSet<string> Capabilities
            {
                get;
            }

            public bool IsFolder
            {
                get { return Capabilities.Contains(ProjectTreeCapabilities.Folder); }
            }

            public string FilePath
            {
                get;
                set;
            }

            public bool Visible
            {
                get;
                set;
            }

            public MutableProjectTree Parent
            {
                get;
                set;
            }

            IProjectTree IProjectTree.Parent
            {
                get { return Parent; }
            }

            IRule IProjectTree.BrowseObjectProperties
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            IImmutableSet<string> IProjectTree.Capabilities
            {
                get { return ImmutableHashSet.CreateRange(Capabilities); }
            }

            IReadOnlyList<IProjectTree> IProjectTree.Children
            {
                get { return Children; }
            }

            ProjectImageMoniker IProjectTree.ExpandedIcon
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ProjectImageMoniker Icon
            {
                get;
                set;
            }

            IntPtr IProjectTree.Identity
            {
                get
                {
                    throw new NotImplementedException();
                }
            }


            IProjectTree IProjectTree.Root
            {
                get
                {
                    var root = this;
                    while (root.Parent != null)
                    {
                        root = root.Parent;
                    }

                    return root;
                }
            }


            public IProjectTree AddCapability(string capability)
            {
                if (!Capabilities.Contains(capability))
                    Capabilities.Add(capability);

                return this;
            }

            int IProjectTree.Size
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            IProjectItemTree IProjectTree.Add(IProjectItemTree subtree)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Add(IProjectTree subtree)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.AddCapability(IEnumerable<string> capabilities)
            {
                throw new NotImplementedException();
            }

            IEnumerable<IProjectTreeDiff> IProjectTree.ChangesSince(IProjectTree priorVersion)
            {
                throw new NotImplementedException();
            }

            bool IProjectTree.Contains(IntPtr nodeId)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Find(IntPtr nodeId)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Remove()
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Remove(IProjectTree subtree)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.RemoveCapability(IEnumerable<string> capabilities)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.RemoveCapability(string capability)
            {
                throw new NotImplementedException();
            }

            IProjectItemTree IProjectTree.Replace(IProjectItemTree subtree)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Replace(IProjectTree subtree)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetBrowseObjectProperties(IRule browseObjectProperties)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetCapabilities(IEnumerable<string> capabilities)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetCaption(string caption)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetExpandedIcon(ProjectImageMoniker expandedIcon)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetIcon(ProjectImageMoniker icon)
            {
                Icon = icon;

                return this;
            }

            IProjectItemTree IProjectTree.SetItem(IProjectPropertiesContext context, IPropertySheet propertySheet, bool isLinked)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetProperties(string caption, string filePath, IRule browseObjectProperties, ProjectImageMoniker icon, ProjectImageMoniker expandedIcon, bool? visible, IEnumerable<string> capabilities, IProjectPropertiesContext context, IPropertySheet propertySheet, bool? isLinked, bool resetFilePath, bool resetBrowseObjectProperties, bool resetIcon, bool resetExpandedIcon)
            {
                if (caption != null)
                    Caption = caption;

                if (FilePath != null)
                    FilePath = filePath;

                if (visible != null)
                    Visible = visible.Value;

                Capabilities.Clear();
                
                foreach (string capability in capabilities)
                {
                    Capabilities.Add(capability);
                }

                return this;
            }

            IProjectTree IProjectTree.SetVisible(bool visible)
            {
                throw new NotImplementedException();
            }

            bool IProjectTree.TryFind(IntPtr nodeId, out IProjectTree subtree)
            {
                throw new NotImplementedException();
            }

            bool IProjectTree.TryFindImmediateChild(string caption, out IProjectTree subtree)
            {
                throw new NotImplementedException();
            }
        }
    }
}
