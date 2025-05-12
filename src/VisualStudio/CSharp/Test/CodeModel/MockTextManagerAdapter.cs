// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel;

/// <summary>
/// A test-only implementation of ITextManagerAdapter used for testing CodeModel.
/// </summary>
internal sealed partial class MockTextManagerAdapter : ITextManagerAdapter
{
    public EnvDTE.TextPoint CreateTextPoint(FileCodeModel fileCodeModel, VirtualTreePoint point)
    {
        return new TextPoint(point);
    }
}
