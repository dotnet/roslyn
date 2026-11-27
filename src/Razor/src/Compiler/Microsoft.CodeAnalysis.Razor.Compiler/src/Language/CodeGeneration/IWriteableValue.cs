// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

/// <summary>
///  A type that can write itself to a <see cref="CodeWriter"/>.
/// </summary>
internal interface IWriteableValue
{
    void WriteTo(CodeWriter writer);
}
