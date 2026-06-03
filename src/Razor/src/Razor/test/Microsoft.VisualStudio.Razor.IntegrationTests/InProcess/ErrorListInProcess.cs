// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Extensibility.Testing;

[TestService]
internal partial class ErrorListInProcess
{
    public async Task<ImmutableArray<string>> WaitForErrorsAsync(string fileName, int expectedCount, CancellationToken cancellationToken)
    {
        var errorSource = ErrorSource.Build | ErrorSource.Other;
        var minimumSeverity = __VSERRORCATEGORY.EC_WARNING;

        var errorItems = await GetErrorItemsAsync(errorSource, minimumSeverity, fileName, expectedCount, cancellationToken);

        var list = errorItems.Select(GetMessage).ToList();

        return list
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private async Task<ImmutableArray<ITableEntryHandle>> GetErrorItemsAsync(
        ErrorSource errorSource,
        __VSERRORCATEGORY minimumSeverity,
        string documentName,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var errorList = await GetRequiredGlobalServiceAsync<SVsErrorList, IErrorList>(cancellationToken);

        using var semaphore = new SemaphoreSlim(1);
        await semaphore.WaitAsync(cancellationToken);

        errorList.TableControl.EntriesChanged += OnEntries_Changed;

        var args = await errorList.TableControl.ForceUpdateAsync().WithCancellation(cancellationToken);

        var filteredEntries = FilterEntries(args, documentName, errorSource, minimumSeverity);

        if (EntriesReady(filteredEntries, expectedCount))
        {
            semaphore.Release();
            errorList.TableControl.EntriesChanged -= OnEntries_Changed;
            return filteredEntries;
        }
        else
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken);
            }
            finally
            {
                errorList.TableControl.EntriesChanged -= OnEntries_Changed;
            }
        }

        args = await errorList.TableControl.ForceUpdateAsync().WithCancellation(cancellationToken);
        filteredEntries = FilterEntries(args, documentName, errorSource, minimumSeverity);

        return filteredEntries;

        void OnEntries_Changed(object sender, EntriesChangedEventArgs e)
        {
            var filteredEntries = FilterEntries(e, documentName, errorSource, minimumSeverity);
            if (EntriesReady(filteredEntries, expectedCount))
            {
                semaphore.Release();
            }
        }

        static bool EntriesReady(ImmutableArray<ITableEntryHandle> entries, int expectedCount)
        {
            return entries.Any() && entries.Length >= expectedCount;
        }

        static ImmutableArray<ITableEntryHandle> FilterEntries(EntriesChangedEventArgs args, string documentName, ErrorSource errorSource, __VSERRORCATEGORY minimumSeverity)
        {
            return args.AllEntries
            .Where(item =>
            {
                if (item.GetCategory() > minimumSeverity)
                {
                    return false;
                }

                if (item.GetErrorSource() is not { } itemErrorSource
                    || !errorSource.HasFlag(itemErrorSource))
                {
                    return false;
                }

                if (!string.Equals(Path.GetFileName(item.GetDocumentName()), documentName))
                {
                    return false;
                }

                return true;
            })
            .ToImmutableArray();

        }
    }

    private static string GetMessage(ITableEntryHandle item)
    {
        var document = Path.GetFileName(item.GetPath() ?? item.GetDocumentName()) ?? "<unknown>";
        var line = item.GetLine() ?? -1;
        var column = item.GetColumn() ?? -1;
        var errorCode = item.GetErrorCode() ?? "<unknown>";
        var text = item.GetText() ?? "<unknown>";
        var severity = item.GetCategory() switch
        {
            __VSERRORCATEGORY.EC_ERROR => "error",
            __VSERRORCATEGORY.EC_WARNING => "warning",
            __VSERRORCATEGORY.EC_MESSAGE => "info",
            var unknown => unknown.ToString(),
        };

        var message = $"{document}({line + 1}, {column + 1}): {severity} {errorCode}: {text}";
        return message;
    }
}
