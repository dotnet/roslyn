// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.StringCopyPaste
{
    [ExportWorkspaceService(typeof(IStringCopyPasteService), ServiceLayer.Host), Shared]
    internal class WpfStringCopyPasteService : IStringCopyPasteService
    {
        // Value from:
        // https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Editor/Text/Impl/EditorOperations/EditorOperations.cs&version=GBmain&line=84&lineEnd=85&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
        private const string ClipboardLineBasedCutCopyTag = "VisualStudioEditorOperationsLineCutCopyClipboardTag";

        private const string RoslynFormat = nameof(RoslynFormat);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WpfStringCopyPasteService()
        {
        }

        private static string GetFormat(string key)
            => $"{RoslynFormat}-{key}";

        public bool? LastCopyWasLineCopy
        {
            get
            {
                try
                {
                    var dataObject = Clipboard.GetDataObject();

                    return dataObject.GetDataPresent(ClipboardLineBasedCutCopyTag) &&
                        dataObject.GetData(ClipboardLineBasedCutCopyTag) is bool value && value;
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
                {
                }

                return null;
            }
        }

        public bool TrySetClipboardData(string key, string data)
        {
            try
            {
                var dataObject = Clipboard.GetDataObject();

                var copy = new DataObject();

                foreach (var format in dataObject.GetFormats())
                {
                    if (dataObject.GetDataPresent(format))
                        copy.SetData(format, dataObject.GetData(format));
                }

                copy.SetData(GetFormat(key), data);

                Clipboard.SetDataObject(copy);
                return true;
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
            }

            return false;
        }

        public string? TryGetClipboardData(string key)
        {
            try
            {
                var dataObject = Clipboard.GetDataObject();
                var format = GetFormat(key);
                if (dataObject.GetDataPresent(format))
                {
                    return dataObject.GetData(format) as string;
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
            }

            return null;
        }
    }
}
