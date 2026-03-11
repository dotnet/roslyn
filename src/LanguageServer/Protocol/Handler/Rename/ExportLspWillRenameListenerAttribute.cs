// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal class ExportLspWillRenameListenerAttribute(string glob) : ExportAttribute(typeof(ILspWillRenameListener)), ILspWillRenameListenerMetadata
{
    public string Glob { get; } = glob;
}
