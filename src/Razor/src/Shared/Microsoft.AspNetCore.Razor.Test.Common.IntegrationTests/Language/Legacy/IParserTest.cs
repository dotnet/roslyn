// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

/// <summary>
/// Marker interface for <see cref="InitializeTestFileAttribute"/> to know what tests are parser tests across both
/// Compiler and Tooling layers
/// </summary>
internal interface IParserTest
{
}
