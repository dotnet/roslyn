// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class DocumentFormattingOptionsStorage
{
    public static ValueTask<DocumentFormattingOptions> GetDocumentFormattingOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetDocumentFormattingOptionsAsync(globalOptions.GetDocumentFormattingOptions(document.Project.Language), cancellationToken);

#pragma warning disable IDE0060 // Unused parameters to match common pattern
    public static DocumentFormattingOptions GetDocumentFormattingOptions(this IGlobalOptionService globalOptions, string language)
        => new(
           // FileHeaderTemplate not stored in global options (does not have a storage other than editorconfig)
           // InsertFinalNewLine not stored in global options (does not have a storage other than editorconfig)
           );
#pragma warning restore
}

