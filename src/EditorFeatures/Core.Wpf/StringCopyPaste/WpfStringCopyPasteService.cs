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
        private const string RoslynCopyPasteSequenceNumber = nameof(RoslynCopyPasteSequenceNumber);
        private const int FailureValue = -1;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WpfStringCopyPasteService()
        {
        }

        public bool TrySetClipboardSequenceNumber(int sequenceNumber)
        {
            try
            {
                var dataObject = Clipboard.GetDataObject();

                var copy = new DataObject(RoslynCopyPasteSequenceNumber, sequenceNumber);

                foreach (var format in dataObject.GetFormats())
                {
                    if (dataObject.GetDataPresent(format))
                        copy.SetData(format, dataObject.GetData(format));
                }

                Clipboard.SetDataObject(copy);
                return true;
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
            }

            return false;
        }

        public bool TryGetClipboardSequenceNumber(out int sequenceNumber)
        {
            try
            {
                var dataObject = Clipboard.GetDataObject();
                if (dataObject.GetDataPresent(RoslynCopyPasteSequenceNumber))
                {
                    var value = dataObject.GetData(RoslynCopyPasteSequenceNumber);
                    if (value is int intVal && intVal != FailureValue)
                    {
                        sequenceNumber = intVal;
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
    }
}
