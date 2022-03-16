// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.StringCopyPaste
{
    [ExportWorkspaceService(typeof(IStringCopyPasteService), ServiceLayer.Host), Shared]
    internal class WpfStringCopyPasteService : IStringCopyPasteService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WpfStringCopyPasteService()
        {
        }

        public bool TryGetClipboardSequenceNumber(out int sequenceNumber)
        {
            try
            {
                var dataObject = Clipboard.GetDataObject();
                if (dataObject != null && dataObject.GetDataPresent(typeof(string)))
                {
                    var data = dataObject.GetData(DataFormats.UnicodeText) as string ??
                               dataObject.GetData(DataFormats.Text) as string;
                    if (data != null)
                    {
                        sequenceNumber = (int)GetClipboardSequenceNumber();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
            }

            sequenceNumber = 0;
            return false;
        }

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();
    }
}
