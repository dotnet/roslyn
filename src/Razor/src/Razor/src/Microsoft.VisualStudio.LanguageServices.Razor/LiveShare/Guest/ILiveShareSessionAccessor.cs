// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.LiveShare;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

internal interface ILiveShareSessionAccessor
{
    CollaborationSession? Session { get; }

    [MemberNotNullWhen(true, nameof(Session))]
    bool IsGuestSessionActive { get; }
}
