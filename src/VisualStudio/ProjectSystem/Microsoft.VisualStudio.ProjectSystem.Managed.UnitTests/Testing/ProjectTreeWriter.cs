// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Designers;

namespace Microsoft.VisualStudio.Testing
{
    internal static class ProjectTreeWriter
    {
        public static void WriteTo(TextWriter writer, IProjectTree tree)
        {
            Requires.NotNull(writer, nameof(writer));
            Requires.NotNull(tree, nameof(tree));

            WriteTo(writer, tree, 0);
        }

        private static void WriteTo(TextWriter writer, IProjectTree tree, int indentLevel)
        {
            string capabilities = string.Join(" ", tree.Capabilities);

            for (int i = 0; i < indentLevel; i++)
            {
                writer.Write(" ");
            }

            writer.Write($"{tree.Caption} (capabilities: {{{capabilities}}})");

            foreach (IProjectTree child in tree.Children)
            {
                writer.WriteLine();
                WriteTo(writer, child, indentLevel + 4);
            }
        }
    }
}
