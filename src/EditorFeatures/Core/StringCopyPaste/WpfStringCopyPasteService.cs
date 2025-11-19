// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Runtime.InteropServices;
using System.Threading;

// Use of System.Windows.Forms over System.Windows is intentional here.  S.W.F has logic in its clipboard impl to help
// with common errors.
using System.Windows.Forms;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Microsoft.CodeAnalysis.Editor.StringCopyPaste;

[ExportWorkspaceService(typeof(IStringCopyPasteService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WpfStringCopyPasteService() : IStringCopyPasteService
{
    // Similar to what WinForms does, except that instead of blocking for up to 1s, we only block for up to 250ms.
    // https://github.com/dotnet/winforms/blob/0f76e65878b1a0958175f17c4360b8198f8b36ba/src/System.Windows.Forms/src/System/Windows/Forms/Clipboard.cs#L31
    private const int RetryTimes = 5;
    private const int RetryDelay = 50;

    private const string RoslynFormat = nameof(RoslynFormat);

    private static string GetFormat(string key)
        => $"{RoslynFormat}-{key}";

    public bool TrySetClipboardData(string key, string data)
    {
        const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;

        try
        {
            var dataObject = GetDataObject();
            if (dataObject is null)
                return false;

            var copy = new DataObject();

            foreach (var format in dataObject.GetFormats())
            {
                if (dataObject.GetDataPresent(format))
                    copy.SetData(format, dataObject.GetData(format));
            }

            copy.SetData(GetFormat(key), data);

            Clipboard.SetDataObject(copy, copy: false, RetryTimes, RetryDelay);
            return true;
        }
        catch (ExternalException ex) when ((uint)ex.ErrorCode == CLIPBRD_E_CANT_OPEN)
        {
            // Expected exception.  The clipboard is a shared windows resource that can be locked by any other
            // process. If we weren't able to acquire it, then just bail out gracefully.
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
        {
        }

        return false;
    }

    /// <summary>
    /// Similar to <see cref="Clipboard.GetDataObject"/> except that this will only block a max of 250ms, not a full second.
    /// </summary>
    private static IDataObject? GetDataObject()
        => GetDataObject(RetryTimes, RetryDelay);

    /// <summary>
    /// Copied from https://github.com/dotnet/winforms/blob/0f76e65878b1a0958175f17c4360b8198f8b36ba/src/System.Windows.Forms/src/System/Windows/Forms/Clipboard.cs#L139
    /// </summary>
    private static IDataObject? GetDataObject(int retryTimes, int retryDelay)
    {
        IComDataObject? dataObject = null;
        int hr;
        var retry = retryTimes;
        do
        {
            hr = OleGetClipboard(ref dataObject);
            if (!ErrorHandler.Succeeded(hr))
            {
                if (retry == 0)
                    return null;

                retry--;
                Thread.Sleep(millisecondsTimeout: retryDelay);
            }
        }
        while (hr != 0);

        if (dataObject is not null)
        {
            if (dataObject is IDataObject ido && !Marshal.IsComObject(dataObject))
            {
                return ido;
            }

            return new DataObject(dataObject);
        }

        return null;
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    public static extern int OleGetClipboard(ref IComDataObject? data);

    public string? TryGetClipboardData(string key)
    {
        try
        {
            var dataObject = GetDataObject();
            if (dataObject is null)
                return null;

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
