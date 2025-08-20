﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal interface IExternalCSharpResxFileSelectionService
{
    Task<(string? selectedFilePath, double confidenceScore, string reasoning, string suggestedKey, bool isQuotaExceeded)> 
        SelectBestResxFileAsync(
            CopilotResxFileSelectionRequestWrapper request, 
            CancellationToken cancellationToken);
}
