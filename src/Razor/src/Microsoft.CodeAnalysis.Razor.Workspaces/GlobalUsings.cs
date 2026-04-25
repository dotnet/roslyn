// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is shared, but not all of the usings are needed for all files, so Roslyn seems keen to flag them as unused in this file
#pragma warning disable IDE0005 // Using directive is unnecessary.

// Strictly speaking we don't need these aliases in all projects, but keeping things consistent with other projects
// makes for a more pleasant development experience.

// Avoid extern alias in every file that needs to use Range
global using LspCodeLens = Roslyn.LanguageServer.Protocol.CodeLens;
global using LspColorPresentation = Roslyn.LanguageServer.Protocol.ColorPresentation;
global using LspDiagnostic = Roslyn.LanguageServer.Protocol.Diagnostic;
global using LspDiagnosticSeverity = Roslyn.LanguageServer.Protocol.DiagnosticSeverity;
global using LspDocumentHighlight = Roslyn.LanguageServer.Protocol.DocumentHighlight;
global using LspHover = Roslyn.LanguageServer.Protocol.Hover;
global using LspLocation = Roslyn.LanguageServer.Protocol.Location;
global using LspRange = Roslyn.LanguageServer.Protocol.Range;
global using LspSignatureHelp = Roslyn.LanguageServer.Protocol.SignatureHelp;

// Avoid ambiguity errors because of our global using above
global using Range = System.Range;

// We put our extensions on Roslyn's LSP types in the same namespace, for convenience, but of course without the alias,
// so to prevent confusion at not needing a using directive to access types, but needing one for extensions, we just
// global using the our extensions (which of course means they didn't need to be in the same namespace for convenience!)
global using Roslyn.LanguageServer.Protocol;
