// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LiveShare;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

[Export(typeof(ILiveShareSessionAccessor))]
internal class LiveShareSessionAccessor : ILiveShareSessionAccessor
{
    private CollaborationSession? _currentSession;
    private bool _guestSessionIsActive;

    // We have a separate IsGuestSessionActive to avoid loading LiveShare dlls unnecessarily.
    public bool IsGuestSessionActive => _guestSessionIsActive;
    public CollaborationSession? Session => _currentSession;

    public void SetSession(CollaborationSession? session)
    {
        _guestSessionIsActive = session is not null;
        _currentSession = session;
    }
}
