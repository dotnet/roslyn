// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_synchronization
partial class Methods
{
    // NOTE: these are sorted/grouped in the order used by the spec

    /// <summary>
    /// Method name for 'textDocument/didOpen'.
    /// <para>
    /// The document open notification is sent from the client to the server to signal newly opened text documents.
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_didOpen">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentDidOpenName = "textDocument/didOpen";

    /// <summary>
    /// Strongly typed message object for 'textDocument/didOpen'.
    /// </summary>
    public static readonly LspNotification<DidOpenTextDocumentParams> TextDocumentDidOpen = new(TextDocumentDidOpenName);

    /// <summary>
    /// Method name for 'textDocument/didChange'.
    /// <para>
    /// The document change notification is sent from the client to the server to signal changes to a text document.
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_didChange">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentDidChangeName = "textDocument/didChange";

    /// <summary>
    /// Strongly typed message object for 'textDocument/didChange'.
    /// </summary>
    public static readonly LspNotification<DidChangeTextDocumentParams> TextDocumentDidChange = new(TextDocumentDidChangeName);

    /// <summary>
    /// Method name for 'textDocument/willSave'.
    /// <para>
    /// The document will save notification is sent from the client to the server before the document is actually saved.
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_willSave">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentWillSaveName = "textDocument/willSave";

    /// <summary>
    /// Strongly typed message object for 'textDocument/willSave'.
    /// </summary>
    public static readonly LspNotification<WillSaveTextDocumentParams> TextDocumentWillSave = new(TextDocumentWillSaveName);

    /// <summary>
    /// Method name for 'textDocument/willSaveWaitUntil'.
    /// <para>
    /// The document will save wait until request is sent from the client to the server before the document is actually saved.
    /// The request can return an array of TextEdits which will be applied to the text document before it is saved.
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_willSaveWaitUntil">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentWillSaveWaitUntilName = "textDocument/willSaveWaitUntil";

    /// <summary>
    /// Strongly typed message object for 'textDocument/willSaveWaitUntil'.
    /// </summary>
    public static readonly LspRequest<WillSaveTextDocumentParams, TextEdit[]?> TextDocumentWillSaveWaitUntil = new(TextDocumentWillSaveWaitUntilName);

    /// <summary>
    /// Method name for 'textDocument/didSave'.
    /// <para>
    /// The document save notification is sent from the client to the server when the document was saved in the client.
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_didSave">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentDidSaveName = "textDocument/didSave";

    /// <summary>
    /// Strongly typed message object for 'textDocument/didSave'.
    /// </summary>
    public static readonly LspNotification<DidSaveTextDocumentParams> TextDocumentDidSave = new(TextDocumentDidSaveName);

    /// <summary>
    /// Method name for 'textDocument/didClose'.
    /// <para>
    /// The document close notification is sent from the client to the server when the document
    /// got closed in the client. The document’s master now exists where the document’s Uri
    /// points to (e.g. if the document’s Uri is a file Uri the master now exists on disk)
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_didClose">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentDidCloseName = "textDocument/didClose";

    /// <summary>
    /// Strongly typed message object for 'textDocument/didSave'.
    /// </summary>
    public static readonly LspNotification<DidCloseTextDocumentParams> TextDocumentDidClose = new(TextDocumentDidCloseName);
}
