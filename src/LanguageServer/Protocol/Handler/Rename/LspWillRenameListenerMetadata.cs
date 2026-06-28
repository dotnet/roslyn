// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// MEF metadata class used to find <see cref="ILspWillRenameListener"/> exports.
/// </summary>
internal sealed class LspWillRenameListenerMetadata(IDictionary<string, object> data)
{
    public string Glob { get; } = (string)data[nameof(ExportLspWillRenameListenerAttribute.Glob)];
}
