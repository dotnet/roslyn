// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Formatting.Rules;

/// <summary>
/// Options for <see cref="AdjustNewLinesOperation"/>.
///
/// <list type="bullet">
///   <item>
///     <term><see cref="PreserveLines"/></term>
///     <description>the operation will leave lineBreaks as it is if original lineBreaks are equal or greater than given lineBreaks</description>
///   </item>
///   <item>
///     <term><see cref="ForceLines"/></term>
///     <description>the operation will force existing lineBreaks to the given lineBreaks</description>
///   </item>
/// </list>
/// </summary>
internal enum AdjustNewLinesOption
{
    PreserveLines,
    ForceLines,
    ForceLinesIfOnSingleLine,
}
