﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Implements a custom version of the standard 'window/showMessageRequest' to display a toast on the client.
/// The standard version requires us to wait for a response and then do something on the server with it.
/// That can be useful in certain cases, but a lot of the time we just want to show a toast with buttons that map to client side commands.
/// This request allows us to do just that.
/// </summary>
internal static class ShowToastNotification
{
    private const string ShowToastNotificationName = "window/_roslyn_showToast";

    public static readonly LSP.Command ShowCSharpLogsCommand = new()
    {
        Title = LanguageServerResources.Show_csharp_logs,
        CommandIdentifier = "csharp.showOutputWindow"
    };

    public static async Task ShowToastNotificationAsync(LSP.MessageType messageType, string message, CancellationToken cancellationToken, params LSP.Command[] commands)
    {
        Contract.ThrowIfNull(LanguageServerHost.Instance, "We don't have an LSP channel yet to send this request through.");
        var languageServerManager = LanguageServerHost.Instance.GetRequiredLspService<IClientLanguageServerManager>();
        var toastParams = new ShowToastNotificationParams(messageType, message, commands);
        await languageServerManager.SendNotificationAsync(ShowToastNotificationName, toastParams, cancellationToken);
    }

    [DataContract]
    private record ShowToastNotificationParams(
        [property: DataMember(Name = "messageType")] LSP.MessageType MessageType,
        [property: DataMember(Name = "message")] string Message,
        [property: DataMember(Name = "commands")] LSP.Command[] Commands);
}
