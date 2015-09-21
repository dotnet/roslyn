// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.Testing
{
    // Parses a string into a project tree
    //
    // <format> ::= 
    //      <path> 
    //      <path> "|" <path>
    // 
    // <path> ::=
    //      [caption]
    //      [caption] "\" [caption]
    //
    //
    // For example:
    //
    // Folder1\File|
    // Folder1\Folder2|
    // Folder1\Folder3|
    internal static partial class ProjectTreeProvider
    {
        public static IProjectTree CreateRoot()
        {
            return Parse("");
        }

        public static IProjectTree Parse(string value)
        {
            MutableProjectTree root = new MutableProjectTree() { Caption = "Root" };
            root.Capabilities.Add(ProjectTreeCapabilities.ProjectRoot);

            ParseChildren(root, value);

            return root; 
        }

        private static void ParseChildren(MutableProjectTree root, string value)
        {
            MutableProjectTree current = root;

            StringBuilder caption = new StringBuilder();
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\r':
                    case '\n':
                        continue;

                    case '\\':
                        // Component separator
                        current = ParseSubTree(current, caption);
                        continue;

                    case '|':
                        // Path separator
                        ParseSubTree(current, caption);
                        current = root;
                        continue;

                    default:
                        caption.Append(c);
                        continue;
                }
            }

            if (caption.Length > 0)
                ParseSubTree(current, caption);
        }

        private static MutableProjectTree ParseSubTree(MutableProjectTree parent, StringBuilder caption)
        {   
            if (caption.Length == 0)
                caption.Append("<Unnamed>");

            MutableProjectTree child = FindOrAddChild(parent, caption.ToString());

            caption.Clear();

            return child;
        }

        private static MutableProjectTree FindOrAddChild(MutableProjectTree parent, string caption)
        {
            foreach (MutableProjectTree otherChild in parent.Children)
            {
                if (StringComparers.Paths.Equals(otherChild.Caption, caption))
                    return otherChild;
            }

            MutableProjectTree child = new MutableProjectTree();
            child.Caption = caption;
            parent.Children.Add(child);

            if (!parent.IsProjectRoot() && !parent.IsFolder)
                parent.Capabilities.Add(ProjectTreeCapabilities.Folder);

            return child;
        }
    }
}
