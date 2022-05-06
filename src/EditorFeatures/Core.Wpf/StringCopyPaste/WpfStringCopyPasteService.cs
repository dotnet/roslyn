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
        private const string RoslynFormat = nameof(RoslynFormat);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WpfStringCopyPasteService()
        {
        }

        private static string GetFormat(string key)
            => $"{RoslynFormat}-{key}";

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
