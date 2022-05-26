// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// A shim interface over the TextManager APIs needed to isolate unit tests.
    /// </summary>
    internal interface ITextManagerAdapter
    {
        EnvDTE.TextPoint CreateTextPoint(FileCodeModel fileCodeModel, VirtualTreePoint point);
    }
}
