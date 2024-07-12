// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.BraceCompletion;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal class ExportBraceCompletionServiceAttribute(string language) : ExportAttribute(typeof(IBraceCompletionService))
{
    public string Language { get; } = language ?? throw new ArgumentNullException(nameof(language));
}
