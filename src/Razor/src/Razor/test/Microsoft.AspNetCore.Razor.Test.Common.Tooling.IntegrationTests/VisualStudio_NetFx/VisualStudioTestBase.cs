// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;

public abstract class VisualStudioTestBase(ITestOutputHelper testOutput) : ToolingParserTestBase(testOutput)
{
}
