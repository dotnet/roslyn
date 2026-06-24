// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

/// <summary>
/// Thread-safe wrapper around <see cref="IMenuCommandService"/> that serializes
/// all command registrations through a lock so the underlying service's lack of thread
/// safety isn't a problem. It can still be used on any thread then, as long as nobody else
/// is using it directly.
/// </summary>
internal sealed class ThreadSafeMenuCommandService
{
    private readonly IMenuCommandService _menuCommandService;
    private readonly object _gate = new();

    public ThreadSafeMenuCommandService(IMenuCommandService menuCommandService)
    {
        _menuCommandService = menuCommandService;
    }

    /// <summary>
    /// Adds an <see cref="OleMenuCommand"/> with both an invoke and a before-query-status handler.
    /// </summary>
    public OleMenuCommand AddCommand(
        Guid commandGroup,
        int commandId,
        EventHandler invokeHandler,
        EventHandler beforeQueryStatus)
    {
        var commandIdWithGroupId = new CommandID(commandGroup, commandId);
        var command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, commandIdWithGroupId);
        lock (_gate)
        {
            _menuCommandService.AddCommand(command);
        }

        return command;
    }

    /// <summary>
    /// Adds a simple <see cref="MenuCommand"/> with only an invoke handler.
    /// </summary>
    public MenuCommand AddCommand(Guid commandGroup, int commandId, EventHandler invokeHandler)
    {
        var commandIdWithGroupId = new CommandID(commandGroup, commandId);
        var command = new MenuCommand(invokeHandler, commandIdWithGroupId);
        lock (_gate)
        {
            _menuCommandService.AddCommand(command);
        }

        return command;
    }
}
