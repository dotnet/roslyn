// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Mirrors the LSP 3.18 spec's TypeScript union type:
// export type TextDocumentContentChangeEvent = TextDocumentContentChangePartial
//     | TextDocumentContentChangeWholeDocument;
// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#textDocumentContentChangePartial
global using TextDocumentContentChangeEvent = Roslyn.LanguageServer.Protocol.SumType<Roslyn.LanguageServer.Protocol.TextDocumentContentChangePartial, Roslyn.LanguageServer.Protocol.TextDocumentContentChangeWholeDocument>;
