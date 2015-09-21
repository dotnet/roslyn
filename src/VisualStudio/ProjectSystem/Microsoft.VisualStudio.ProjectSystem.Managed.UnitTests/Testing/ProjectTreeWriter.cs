// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.ProjectSystem.Designers;

namespace Microsoft.VisualStudio.Testing
{
    internal class ProjectTreeWriter
    {
        private readonly StringBuilder _builder  = new StringBuilder();
        private readonly bool _tagElements;
        private readonly IProjectTree _parent;        

        public ProjectTreeWriter(IProjectTree tree, bool tagElements = false)
        {
            Requires.NotNull(tree, nameof(tree));

            _parent = tree;
            _tagElements = tagElements;
        }

        public string WriteToString()
        {
            _builder.Clear();

            WriteProjectItem(_parent);

            return _builder.ToString();
        }

        private void WriteProjectItem(IProjectTree tree, int indentLevel = 0)
        {
            WriteIndentLevel(indentLevel);
            WriteCaption(tree);
            WriteProperties(tree);
            WriteFilePath(tree);
            WriteChildren(tree, indentLevel);
        }

        private void WriteChildren(IProjectTree tree, int indentLevel)
        {
            foreach (IProjectTree child in tree.Children)
            {
                _builder.AppendLine();
                WriteProjectItem(child, indentLevel + 1);
            }
        }

        private void WriteIndentLevel(int indentLevel)
        {
            _builder.Append(' ', indentLevel * 4);

            if (_tagElements && indentLevel > 0)
                _builder.Append("[indent]");
        }

        private void WriteCaption(IProjectTree tree)
        {
            _builder.Append(tree.Caption);

            if (_tagElements)
            {
                _builder.Append("[caption]");
            }

            _builder.Append(" ");
        }

        private void WriteProperties(IProjectTree tree)
        {
            _builder.Append('(');

            WriteVisibility(tree);

            _builder.Append(", ");

            WriteCapabilities(tree);

            _builder.Append(')');
        }

        private void WriteFilePath(IProjectTree tree)
        {
            _builder.Append(", FilePath: ");
            _builder.Append('"');
            _builder.Append(tree.FilePath);
            
            if (_tagElements)
            {
                _builder.Append("[filepath]");
            }

            _builder.Append('"');
        }

        private void WriteVisibility(IProjectTree tree)
        {
            _builder.Append("visibility: ");
            
            if (tree.Visible)
            {
                _builder.Append("visible");
            }
            else
            {
                _builder.Append("invisible");
            }
        }

        private void WriteCapabilities(IProjectTree tree)
        {
            _builder.Append("capabilities: ");
            _builder.Append('{');

            bool writtenCapability = false;

            foreach (string capability in tree.Capabilities.OrderBy(c => c, StringComparer.InvariantCultureIgnoreCase))
            {
                if (writtenCapability)
                    _builder.Append(" ");

                writtenCapability = true;
                _builder.Append(capability);

                if (_tagElements)
                {
                    _builder.Append("[capability]");
                }
            }

            _builder.Append('}');
        }
    }
}
