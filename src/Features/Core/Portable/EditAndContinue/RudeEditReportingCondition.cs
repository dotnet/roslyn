// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct RudeEditReportingCondition(ISymbol oldMember, bool reportWhenActive)
{
    public ISymbol OldSymbol { get; } = ValidateMember(oldMember);

    /// <summary>
    /// True to report the diagnostic when <see cref="OldSymbol"/> is active.
    /// </summary>
    public bool ReportWhenSymbolIsActive { get; } = reportWhenActive;

    private static ISymbol ValidateMember(ISymbol member)
    {
        Contract.ThrowIfFalse(member.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Field);
        return member;
    }
}
