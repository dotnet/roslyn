// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml;

/// <summary>
/// The currently supported set of XAML LSP Server capabilities
/// </summary>
internal sealed class XamlCapabilityRegistrations
{
    private static readonly string s_id = Guid.NewGuid().ToString();
    private static readonly DocumentFilter[] s_documentFilter = new DocumentFilter[]
    {
        new DocumentFilter()
        {
            Language = StringConstants.XamlLanguageName,
            Pattern = "**/*.xaml"
        },
    };

    [Export]
    public Registration DidOpenRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentDidOpenName,
            RegisterOptions = new TextDocumentRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
            }
        };

    [Export]
    public Registration DidChangeRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentDidChangeName,
            RegisterOptions = new TextDocumentChangeRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
                SyncKind = TextDocumentSyncKind.Incremental
            }
        };

    [Export]
    public Registration DidCloseRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentDidCloseName,
            RegisterOptions = new TextDocumentRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
            }
        };

    [Export]
    public Registration CompletionRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentCompletionName,
            RegisterOptions = new CompletionRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
                ResolveProvider = true,
                TriggerCharacters = new string[] { "<", " ", ":", ".", "=", "\"", "'", "{", ",", "(" },
                AllCommitCharacters = Completion.CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray()
            }
        };

    [Export]
    public Registration HoverDidChangeRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentHoverName,
            RegisterOptions = new HoverRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
            }
        };

    [Export]
    public Registration FoldingRangeDidChangeRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentFoldingRangeName,
            RegisterOptions = new FoldingRangeRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
            }
        };

    [Export]
    public Registration DocumentFormattingDidChangeRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentFormattingName,
            RegisterOptions = new DocumentFormattingRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
            }
        };

    [Export]
    public Registration DocumentRangeFormattingDidChangeRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentRangeFormattingName,
            RegisterOptions = new DocumentRangeFormattingRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
            }
        };

    [Export]
    public Registration DocumentOnTypeFormattingRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentOnTypeFormattingName,
            RegisterOptions = new DocumentOnTypeFormattingRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
                FirstTriggerCharacter = ">",
                MoreTriggerCharacter = new string[] { " " }
            }
        };

    [Export]
    public Registration DefinitionRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentDefinitionName,
            RegisterOptions = new DefinitionRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
            }
        };

    [Export]
    public Registration DiagnosticRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentDiagnosticName,
            RegisterOptions = new DiagnosticRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
                WorkspaceDiagnostics = true
            }
        };

    [Export]
    public Registration CodeActionRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.TextDocumentCodeActionName,
            RegisterOptions = new CodeActionRegistrationOptions
            {
                DocumentSelector = s_documentFilter,
                CodeActionKinds = new CodeActionKind[] { CodeActionKind.QuickFix, CodeActionKind.SourceOrganizeImports },
                ResolveProvider = true
            }
        };

    // TODO: Dynamically register this for the custom client
    //[Export]
    //public Registration AutoInsertRegistration { get; } =
    //    new()
    //    {
    //        Id = s_id,
    //        Method = VSInternalMethods.OnAutoInsertName,
    //        RegisterOptions = new VSInternalDocumentOnAutoInsertRegistrationOptions
    //        {
    //            DocumentSelector = s_documentFilter,
    //            TriggerCharacters = new string[] { "=", "/" }
    //        }
    //    };

    [Export]
    public Registration ExecuteCommandRegistration { get; } =
        new()
        {
            Id = s_id,
            Method = Methods.WorkspaceExecuteCommandName,
            RegisterOptions = new ExecuteCommandRegistrationOptions
            {
                Commands = new string[] { StringConstants.CreateEventHandlerCommand }
            }
        };
}
