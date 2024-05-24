// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal partial class ErrorListInProcess
{
    public Task ShowErrorListAsync(CancellationToken cancellationToken)
        => ShowErrorListAsync(ErrorSource.Build | ErrorSource.Other, minimumSeverity: __VSERRORCATEGORY.EC_WARNING, cancellationToken);

    public Task ShowBuildErrorsAsync(CancellationToken cancellationToken)
        => ShowErrorListAsync(ErrorSource.Build, minimumSeverity: __VSERRORCATEGORY.EC_WARNING, cancellationToken);

    public async Task ShowErrorListAsync(ErrorSource errorSource, __VSERRORCATEGORY minimumSeverity, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var errorList = await GetRequiredGlobalServiceAsync<SVsErrorList, IErrorList>(cancellationToken);
        ((IVsErrorList)errorList).BringToFront();
        errorList.AreBuildErrorSourceEntriesShown = errorSource.HasFlag(ErrorSource.Build);
        errorList.AreOtherErrorSourceEntriesShown = errorSource.HasFlag(ErrorSource.Other);
        errorList.AreErrorsShown = minimumSeverity >= __VSERRORCATEGORY.EC_ERROR;
        errorList.AreWarningsShown = minimumSeverity >= __VSERRORCATEGORY.EC_WARNING;
        errorList.AreMessagesShown = minimumSeverity >= __VSERRORCATEGORY.EC_MESSAGE;
    }

    public Task<ImmutableArray<string>> GetErrorsAsync(CancellationToken cancellationToken)
        => GetErrorsAsync(ErrorSource.Build | ErrorSource.Other, minimumSeverity: __VSERRORCATEGORY.EC_WARNING, cancellationToken);

    public Task<ImmutableArray<string>> GetBuildErrorsAsync(CancellationToken cancellationToken)
        => GetErrorsAsync(ErrorSource.Build, minimumSeverity: __VSERRORCATEGORY.EC_WARNING, cancellationToken);

    public async Task<ImmutableArray<string>> GetErrorsAsync(ErrorSource errorSource, __VSERRORCATEGORY minimumSeverity, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var errorItems = await GetErrorItemsAsync(errorSource, minimumSeverity, cancellationToken);
        var list = errorItems.Select(GetMessage).ToList();

        return list
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public async Task<string> NavigateToErrorListItemAsync(int item, bool isPreview, bool shouldActivate, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var errorList = await GetRequiredGlobalServiceAsync<SVsErrorList, IErrorList>(cancellationToken);
        ErrorSource errorSource = 0;
        if (errorList.AreBuildErrorSourceEntriesShown)
            errorSource |= ErrorSource.Build;

        if (errorList.AreOtherErrorSourceEntriesShown)
            errorSource |= ErrorSource.Other;

        var minimumSeverity =
            errorList.AreMessagesShown ? __VSERRORCATEGORY.EC_MESSAGE :
            errorList.AreWarningsShown ? __VSERRORCATEGORY.EC_WARNING :
            __VSERRORCATEGORY.EC_ERROR;

        var items = await GetErrorItemsAsync(errorSource, minimumSeverity, cancellationToken);

        items[item].NavigateTo(isPreview, shouldActivate);
        return GetMessage(items[item]);
    }

    private async Task<ImmutableArray<ITableEntryHandle>> GetErrorItemsAsync(ErrorSource errorSource, __VSERRORCATEGORY minimumSeverity, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var errorList = await GetRequiredGlobalServiceAsync<SVsErrorList, IErrorList>(cancellationToken);
        var args = await errorList.TableControl.ForceUpdateAsync().WithCancellation(cancellationToken);
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

                return true;
            })
            .ToImmutableArray();
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
