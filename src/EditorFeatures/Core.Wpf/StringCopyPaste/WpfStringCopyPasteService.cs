// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
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
            var dataObject = Clipboard.GetDataObject();
            if (dataObject != null && dataObject.GetDataPresent(typeof(string)))
            {
                var data = dataObject.GetData(DataFormats.UnicodeText) as string ??
                           dataObject.GetData(DataFormats.Text) as string;
                if (data != null)
                {
                    // Ok, there's viable text on the clipboard.  Try to create a new dataobject, copy everything from
                    // the current data object to it, and add roslyn's data.
                    var copy = new DataObjectWrapper(dataObject, sequenceNumber);
                    Clipboard.SetDataObject(copy);
                    return true;
                }
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

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();

        private class DataObjectWrapper : IDataObject
        {
            private readonly IDataObject _dataObject;
            private int _roslynSequenceNumber;

            public DataObjectWrapper(IDataObject dataObject, int roslynSequenceNumber)
            {
                _dataObject = dataObject;
                _roslynSequenceNumber = roslynSequenceNumber;
            }

            public object GetData(string format)
            {
                return format == RoslynCopyPasteSequenceNumber
                    ? _roslynSequenceNumber
                    : _dataObject.GetData(format);
            }

            public object GetData(Type format)
            {
                return _dataObject.GetData(format);
            }

            public object GetData(string format, bool autoConvert)
            {
                return format == RoslynCopyPasteSequenceNumber
                    ? _roslynSequenceNumber
                    : _dataObject.GetData(format, autoConvert);
            }

            public bool GetDataPresent(string format)
            {
                return format == RoslynCopyPasteSequenceNumber || _dataObject.GetDataPresent(format);
            }

            public bool GetDataPresent(Type format)
            {
                return _dataObject.GetDataPresent(format);
            }

            public bool GetDataPresent(string format, bool autoConvert)
            {
                return format == RoslynCopyPasteSequenceNumber || _dataObject.GetDataPresent(format, autoConvert);
            }

            public string[] GetFormats()
            {
                var result = _dataObject.GetFormats().ToList();
                result.Add(RoslynCopyPasteSequenceNumber);
                return result.ToArray();
            }

            public string[] GetFormats(bool autoConvert)
            {
                var result = _dataObject.GetFormats(autoConvert).ToList();
                result.Add(RoslynCopyPasteSequenceNumber);
                return result.ToArray();

            }

            public void SetData(object data)
            {
                _dataObject.SetData(data);
            }

            public void SetData(string format, object data)
            {
                if (format == RoslynCopyPasteSequenceNumber)
                    _roslynSequenceNumber = data is int v ? v : FailureValue;
                else
                    _dataObject.SetData(format, data);
            }

            public void SetData(Type format, object data)
            {
                _dataObject.SetData(format, data);
            }

            public void SetData(string format, object data, bool autoConvert)
            {
                if (format == RoslynCopyPasteSequenceNumber)
                    _roslynSequenceNumber = data is int v ? v : FailureValue;
                else
                    _dataObject.SetData(format, data, autoConvert);

            }
        }
    }
}
