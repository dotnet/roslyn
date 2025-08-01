// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.MiscellaneousFiles;

/// <summary>
/// This class runs all the tests in <see cref="AbstractLspMiscellaneousFilesWorkspaceTests"/> against the base implementation.
/// </summary>
public sealed class LspMiscellaneousFilesWorkspaceTests : AbstractLspMiscellaneousFilesWorkspaceTests
{
    public LspMiscellaneousFilesWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
