// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#notebookDocument_synchronization
partial class Methods
{
    // NOTE: these are sorted/grouped in the order used by the spec

    /// <summary>
    /// Method name for 'notebookDocument/didOpen'.
    /// <para>
    /// The open notification is sent from the client to the server when a notebook document is opened. It is only
    /// sent by a client if the server specified a <see cref="NotebookDocumentSyncSelector.Notebook"/> in its
    /// <see cref="NotebookDocumentSyncOptions.NotebookSelector"/> capability.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocument_didOpen">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string NotebookDidOpenName = "notebookDocument/didOpen";

    /// <summary>
    /// Strongly typed message object for 'notebookDocument/didOpen'.
    /// </summary>
    public static readonly LspNotification<DidOpenNotebookDocumentParams> NotebookDidOpen = new(NotebookDidOpenName);

    /// <summary>
    /// Method name for 'notebookDocument/didChange'.
    /// <para>
    /// The change notification is sent from the client to the server when a notebook document changes. It is only
    /// sent by a client if the server specified a <see cref="NotebookDocumentSyncSelector.Notebook"/> in its
    /// <see cref="NotebookDocumentSyncOptions.NotebookSelector"/> capability.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocument_didChange">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string NotebookDidChangeName = "notebookDocument/didChange";

    /// <summary>
    /// Strongly typed message object for 'notebookDocument/didChange'.
    /// </summary>
    public static readonly LspNotification<DidOpenNotebookDocumentParams> NotebookDidChange = new(NotebookDidChangeName);

    /// <summary>
    /// Method name for 'notebookDocument/didSave'.
    /// <para>
    /// The change notification is sent from the client to the server when a notebook document is saved. It is only
    /// sent by a client if the server specified a <see cref="NotebookDocumentSyncSelector.Notebook"/> in its
    /// <see cref="NotebookDocumentSyncOptions.NotebookSelector"/> capability.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocument_didSave">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string NotebookDidSaveName = "notebookDocument/didSave";

    /// <summary>
    /// Strongly typed message object for 'notebookDocument/didSave'.
    /// </summary>
    public static readonly LspNotification<DidSaveNotebookDocumentParams> NotebookDidSave = new(NotebookDidSaveName);

    /// <summary>
    /// Method name for 'notebookDocument/didClose'.
    /// <para>
    /// The change notification is sent from the client to the server when a notebook document is closed. It is only
    /// sent by a client if the server specified a <see cref="NotebookDocumentSyncSelector.Notebook"/> in its
    /// <see cref="NotebookDocumentSyncOptions.NotebookSelector"/> capability.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocument_didClose">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string NotebookDidCloseName = "notebookDocument/didClose";

    /// <summary>
    /// Strongly typed message object for 'notebookDocument/didClose'.
    /// </summary>
    public static readonly LspNotification<DidCloseNotebookDocumentParams> NotebookDidClose = new(NotebookDidCloseName);
}
