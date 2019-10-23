// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Reflection;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class IIndentationManagerService
    {
        public const string MefContractName = "Microsoft.VisualStudio.Text.Editor.IIndentationManagerService";

        private readonly object _indentationManagerService;
        private readonly MethodInfo _useSpacesForWhitespaceMethod;
        private readonly MethodInfo _getTabSizeMethod;
        private readonly MethodInfo _getIndentSizeMethod;

        public IIndentationManagerService(object indentationManagerService)
        {
            _indentationManagerService = indentationManagerService;

            var @interface = _indentationManagerService.GetType().GetInterface(MefContractName);
            _useSpacesForWhitespaceMethod = @interface.GetMethod("UseSpacesForWhitespace", new Type[] { typeof(ITextBuffer), typeof(bool) });
            _getTabSizeMethod = @interface.GetMethod("GetTabSize", new Type[] { typeof(ITextBuffer), typeof(bool) });
            _getIndentSizeMethod = @interface.GetMethod("GetIndentSize", new Type[] { typeof(ITextBuffer), typeof(bool) });
        }

        public static IIndentationManagerService? FromDefaultImport(object? indentationManagerService)
        {
            if (indentationManagerService != null)
            {
                return new IIndentationManagerService(indentationManagerService);
            }
            else
            {
                return null;
            }
        }

        public void GetIndentation(ITextBuffer textBuffer, bool explicitFormat, out bool convertTabsToSpaces, out int tabSize, out int indentSize)
        {
            convertTabsToSpaces = UseSpacesForWhitespace(textBuffer, explicitFormat);
            tabSize = GetTabSize(textBuffer, explicitFormat);
            indentSize = GetIndentSize(textBuffer, explicitFormat);
        }

        public bool UseSpacesForWhitespace(ITextBuffer textBuffer, bool explicitFormat)
        {
            return (bool)_useSpacesForWhitespaceMethod.Invoke(_indentationManagerService, new object[] { textBuffer, explicitFormat });
        }

        public int GetTabSize(ITextBuffer textBuffer, bool explicitFormat)
        {
            return (int)_getTabSizeMethod.Invoke(_indentationManagerService, new object[] { textBuffer, explicitFormat });
        }

        public int GetIndentSize(ITextBuffer textBuffer, bool explicitFormat)
        {
            return (int)_getIndentSizeMethod.Invoke(_indentationManagerService, new object[] { textBuffer, explicitFormat });
        }
    }
}
