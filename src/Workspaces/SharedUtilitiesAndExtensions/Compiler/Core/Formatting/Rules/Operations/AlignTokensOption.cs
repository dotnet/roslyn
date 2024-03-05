// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Formatting.Rules;

/// <summary>
/// option to control <see cref="AlignTokensOperation"/> behavior
/// </summary>
internal enum AlignTokensOption
{
    AlignIndentationOfTokensToBaseToken,
    AlignIndentationOfTokensToFirstTokenOfBaseTokenLine
}
