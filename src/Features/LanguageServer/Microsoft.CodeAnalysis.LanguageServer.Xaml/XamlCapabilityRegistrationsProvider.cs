// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml;

/// <summary>
/// The currently supported set of XAML LSP Server capabilities
/// </summary>
[Export(typeof(ICapabilityRegistrationsProvider)), Shared]
internal sealed class XamlCapabilityRegistrationsProvider : ICapabilityRegistrationsProvider
{
    private readonly string _id = Guid.NewGuid().ToString();
    private readonly DocumentFilter[] _documentFilter = new DocumentFilter[]
    {
        new DocumentFilter()
        {
            Language = StringConstants.XamlLanguageName,
            Pattern = "**/*.xaml"
        },
    };

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public XamlCapabilityRegistrationsProvider()
    {
    }

    public ImmutableArray<Registration> GetRegistrations()
    {
        return new Registration[]
        {
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentDidOpenName,
                RegisterOptions = new TextDocumentRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentDidChangeName,
                RegisterOptions = new TextDocumentChangeRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                    SyncKind = TextDocumentSyncKind.Incremental
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentDidCloseName,
                RegisterOptions = new TextDocumentRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentCompletionName,
                RegisterOptions = new CompletionRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                    ResolveProvider = true,
                    TriggerCharacters = new string[] { "<", " ", ":", ".", "=", "\"", "'", "{", ",", "(" },
                    AllCommitCharacters = Completion.CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray()
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentHoverName,
                RegisterOptions = new HoverRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentFoldingRangeName,
                RegisterOptions = new FoldingRangeRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentFormattingName,
                RegisterOptions = new DocumentFormattingRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentRangeFormattingName,
                RegisterOptions = new DocumentRangeFormattingRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentOnTypeFormattingName,
                RegisterOptions = new DocumentOnTypeFormattingRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                    FirstTriggerCharacter = ">",
                    MoreTriggerCharacter = new string[] { " " }
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentDefinitionName,
                RegisterOptions = new DefinitionRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                    WorkspaceDiagnostics = true
                }
            },
            new Registration
            {
                Id = _id,
                Method = Methods.TextDocumentCodeActionName,
                RegisterOptions = new CodeActionRegistrationOptions
                {
                    DocumentSelector = _documentFilter,
                    CodeActionKinds = new CodeActionKind[] { CodeActionKind.QuickFix, CodeActionKind.SourceOrganizeImports },
                    ResolveProvider = true
                }
            },
            //// TODO: Dynamically register this for the custom client
            ////new Registration
            ////{
            ////    Id = _id,
            ////    Method = VSInternalMethods.OnAutoInsertName,
            ////    RegisterOptions = new VSInternalDocumentOnAutoInsertRegistrationOptions
            ////    {
            ////        DocumentSelector = _documentFilter,
            ////        TriggerCharacters = new string[] { "=", "/" }
            ////    }
            ////},
            new Registration
            {
                Id = _id,
                Method = Methods.WorkspaceExecuteCommandName,
                RegisterOptions = new ExecuteCommandRegistrationOptions
                {
                    Commands = new string[] { StringConstants.CreateEventHandlerCommand }
                }
            },
        }.AsImmutable();
    }
}
