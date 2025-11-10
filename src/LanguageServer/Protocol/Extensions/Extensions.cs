// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Core.Imaging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal static partial class Extensions
{
    public static DocumentUri GetURI(this TextDocument document)
    {
        Contract.ThrowIfNull(document.FilePath);
        return document is SourceGeneratedDocument sourceGeneratedDocument
            ? SourceGeneratedDocumentUri.Create(sourceGeneratedDocument.Identity)
            : ProtocolConversions.CreateAbsoluteDocumentUri(document.FilePath);
    }

    /// <summary>
    /// Generate the Uri of a document by replace the name in file path using the document's name.
    /// Used to generate the correct Uri when rename a document, because calling <seealso cref="Document.WithName(string)"/> doesn't update the file path.
    /// </summary>
    public static DocumentUri GetUriForRenamedDocument(this TextDocument document)
    {
        Contract.ThrowIfNull(document.FilePath);
        Contract.ThrowIfNull(document.Name);
        Contract.ThrowIfTrue(document is SourceGeneratedDocument);
        var directoryName = Path.GetDirectoryName(document.FilePath);

        Contract.ThrowIfNull(directoryName);
        var path = Path.Combine(directoryName, document.Name);
        return ProtocolConversions.CreateAbsoluteDocumentUri(path);
    }

    public static DocumentUri CreateUriForDocumentWithoutFilePath(this TextDocument document)
    {
        Contract.ThrowIfNull(document.Name);
        Contract.ThrowIfNull(document.Project.FilePath);

        var projectDirectoryName = Path.GetDirectoryName(document.Project.FilePath);
        Contract.ThrowIfNull(projectDirectoryName);
        var path = Path.Combine([projectDirectoryName, .. document.Folders, document.Name]);
        return ProtocolConversions.CreateAbsoluteDocumentUri(path);
    }

    public static Uri GetRequiredParsedUri(this DocumentUri documentUri)
    {
        Contract.ThrowIfNull(documentUri.ParsedUri, $"URI {documentUri} could not be parsed");
        return documentUri.ParsedUri;
    }

    /// <summary>
    /// Get all regular and additional <see cref="TextDocument"/>s for the given <paramref name="documentUri"/>.
    /// This will not return source generated documents.
    /// </summary>
    public static ImmutableArray<TextDocument> GetTextDocuments(this Solution solution, DocumentUri documentUri)
    {
        var documentIds = GetDocumentIds(solution, documentUri);

        var documents = documentIds
            .Select(solution.GetTextDocument)
            .WhereNotNull()
            .ToImmutableArray();
        return documents;
    }

    public static ImmutableArray<DocumentId> GetDocumentIds(this Solution solution, DocumentUri documentUri)
    {
        if (documentUri.ParsedUri is null)
        {
            // If we were given an unparse-able URI, just search for documents with the full URI string.
            // For example the miscellaneous workspace stores these kinds of documents with the full URI string.
            return solution.GetDocumentIdsWithFilePath(documentUri.UriString);
        }

        // If this is not our special scheme for generated documents, then we can just look for documents with that file path.
        if (documentUri.ParsedUri.Scheme != SourceGeneratedDocumentUri.Scheme)
            return solution.GetDocumentIdsWithFilePath(ProtocolConversions.GetDocumentFilePathFromUri(documentUri.ParsedUri));

        // We can get a null documentId if we were unable to find the project associated with the
        // generated document - this can happen if say a project is unloaded.  There may be LSP requests
        // already in-flight which may ask for a generated document from that project.  So we return null
        var documentId = SourceGeneratedDocumentUri.DeserializeIdentity(solution, documentUri.ParsedUri)?.DocumentId;

        return documentId is not null ? [documentId] : [];
    }

    /// <summary>
    /// Finds the document for a TextDocumentIdentifier, potentially returning a source-generated file.
    /// </summary>
    public static async ValueTask<Document?> GetDocumentAsync(this Solution solution, TextDocumentIdentifier documentIdentifier, CancellationToken cancellationToken)
    {
        var textDocument = await solution.GetTextDocumentAsync(documentIdentifier, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfTrue(textDocument is not null && textDocument is not Document, $"{textDocument!.Id} is not a Document");
        return textDocument as Document;
    }

    /// <summary>
    /// Finds the TextDocument for a TextDocumentIdentifier, potentially returning a source-generated file.
    /// </summary>
    public static async ValueTask<TextDocument?> GetTextDocumentAsync(this Solution solution, TextDocumentIdentifier documentIdentifier, CancellationToken cancellationToken)
    {
        var documents = await solution.GetTextDocumentsAsync(documentIdentifier.DocumentUri, cancellationToken).ConfigureAwait(false);
        return documents.Length == 0
            ? null
            : documents.FindDocumentInProjectContext(documentIdentifier, (sln, id) => sln.GetRequiredTextDocument(id));
    }

    public static async ValueTask<ImmutableArray<TextDocument>> GetTextDocumentsAsync(this Solution solution, DocumentUri documentUri, CancellationToken cancellationToken)
    {
        // If it's the URI scheme for source generated files, delegate to our other helper, otherwise we can handle anything else here.
        if (documentUri.ParsedUri?.Scheme == SourceGeneratedDocumentUri.Scheme)
        {
            // In the case of a URI scheme for source generated files, we generate a different URI for each project, thus this URI cannot be linked into multiple projects;
            // this means we can safely call .SingleOrDefault() and not worry about calling FindDocumentInProjectContext.
            var documentId = solution.GetDocumentIds(documentUri).SingleOrDefault();
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            return document is not null ? [document] : [];
        }

        return solution.GetTextDocuments(documentUri);
    }

    private static T FindItemInProjectContext<T>(
        ImmutableArray<T> items,
        TextDocumentIdentifier itemIdentifier,
        Func<T, ProjectId> projectIdGetter,
        Func<T> defaultGetter)
    {
        if (items.Length > 1)
        {
            // We have more than one document; try to find the one that matches the right context
            if (itemIdentifier is VSTextDocumentIdentifier vsDocumentIdentifier && vsDocumentIdentifier.ProjectContext != null)
            {
                var projectId = ProtocolConversions.ProjectContextToProjectId(vsDocumentIdentifier.ProjectContext);
                var matchingItem = items.FirstOrDefault(d => projectIdGetter(d) == projectId);

                if (matchingItem != null)
                {
                    return matchingItem;
                }
            }
            else
            {
                return defaultGetter();
            }
        }

        // We either have only one item or have multiple, but none of them  matched our context. In the
        // latter case, we'll just return the first one arbitrarily since this might just be some temporary mis-sync
        // of client and server state.
        return items[0];
    }

    public static T FindDocumentInProjectContext<T>(this ImmutableArray<T> documents, TextDocumentIdentifier documentIdentifier, Func<Solution, DocumentId, T> documentGetter) where T : TextDocument
    {
        return FindItemInProjectContext(documents, documentIdentifier, projectIdGetter: (item) => item.Project.Id, defaultGetter: () =>
        {
            // We were not passed a project context.  This can happen when the LSP powered NavBar is not enabled.
            // This branch should be removed when we're using the LSP based navbar in all scenarios.

            var solution = documents.First().Project.Solution;
            // Lookup which of the linked documents is currently active in the workspace.
            var documentIdInCurrentContext = solution.Workspace.GetDocumentIdInCurrentContext(documents.First().Id);
            return documentGetter(solution, documentIdInCurrentContext);
        });
    }

    public static Project? GetProject(this Solution solution, TextDocumentIdentifier projectIdentifier)
    {
        // We need to parse the URI (scheme, file path) to be able to lookup the URI in the solution.
        if (projectIdentifier.DocumentUri.ParsedUri is null)
        {
            return null;
        }

        var projects = solution.Projects.WhereAsArray(project => project.FilePath == projectIdentifier.DocumentUri.ParsedUri.LocalPath);
        return !projects.Any()
            ? null
            : FindItemInProjectContext(projects, projectIdentifier, projectIdGetter: (item) => item.Id, defaultGetter: () => projects[0]);
    }

    public static TextDocument? GetAdditionalDocument(this Solution solution, TextDocumentIdentifier documentIdentifier)
    {
        var documentIds = GetDocumentIds(solution, documentIdentifier.DocumentUri);

        // We don't call GetRequiredAdditionalDocument as the id could be referring to a regular document.
        var additionalDocuments = documentIds.Select(solution.GetAdditionalDocument).WhereNotNull().ToImmutableArray();
        return !additionalDocuments.Any()
            ? null
            : additionalDocuments.FindDocumentInProjectContext(documentIdentifier, (sln, id) => sln.GetRequiredAdditionalDocument(id));
    }

    public static async Task<int> GetPositionFromLinePositionAsync(this TextDocument document, LinePosition linePosition, CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        return text.Lines.GetPosition(linePosition);
    }

    public static bool HasVisualStudioLspCapability(this ClientCapabilities? clientCapabilities)
    {
        if (clientCapabilities is VSInternalClientCapabilities vsClientCapabilities)
        {
            return vsClientCapabilities.SupportsVisualStudioExtensions;
        }

        return false;
    }

    public static bool HasCompletionListDataCapability(this ClientCapabilities clientCapabilities)
    {
        if (!TryGetVSCompletionListSetting(clientCapabilities, out var completionListSetting))
        {
            return false;
        }

        return completionListSetting.Data;
    }

    public static bool HasCompletionListCommitCharactersCapability(this ClientCapabilities clientCapabilities)
    {
        if (!TryGetVSCompletionListSetting(clientCapabilities, out var completionListSetting))
        {
            return false;
        }

        return completionListSetting.CommitCharacters;
    }

    public static string GetMarkdownLanguageName(this Document document)
    {
        switch (document.Project.Language)
        {
            case LanguageNames.CSharp:
                return "csharp";
            case LanguageNames.VisualBasic:
                return "vb";
            case LanguageNames.FSharp:
                return "fsharp";
            case InternalLanguageNames.TypeScript:
                return "typescript";
            default:
                throw new ArgumentException(string.Format("Document project language {0} is not valid", document.Project.Language));
        }
    }

    public static ClassifiedTextElement GetClassifiedText(this DefinitionItem definition)
        => new(definition.DisplayParts.Select(part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));

    private static bool TryGetVSCompletionListSetting(ClientCapabilities clientCapabilities, [NotNullWhen(returnValue: true)] out VSInternalCompletionListSetting? completionListSetting)
    {
        if (clientCapabilities is not VSInternalClientCapabilities vsClientCapabilities)
        {
            completionListSetting = null;
            return false;
        }

        var textDocumentCapability = vsClientCapabilities.TextDocument;
        if (textDocumentCapability == null)
        {
            completionListSetting = null;
            return false;
        }

        if (textDocumentCapability.Completion is not VSInternalCompletionSetting vsCompletionSetting)
        {
            completionListSetting = null;
            return false;
        }

        completionListSetting = vsCompletionSetting.CompletionList;
        if (completionListSetting == null)
        {
            return false;
        }

        return true;
    }

    public static int CompareTo(this Position p1, Position p2)
    {
        if (p1.Line > p2.Line)
            return 1;
        else if (p1.Line < p2.Line)
            return -1;

        if (p1.Character > p2.Character)
            return 1;
        else if (p1.Character < p2.Character)
            return -1;

        return 0;
    }

    public static VSImageId ToVSImageId(this Glyph glyph)
    {
        var (guid, id) = glyph.GetVsImageData();

        return new() { Guid = guid, Id = id };
    }

    public static ImageId ToLSPImageId(this Glyph glyph)
    {
        var (guid, id) = glyph.GetVsImageData();

        return new(guid, id);
    }

    public static ImageElement ToLSPElement(this QuickInfoGlyphElement element)
        => new(element.Glyph.ToLSPImageId());

    public static ClassifiedTextRun ToLSPRun(this QuickInfoClassifiedTextRun run)
        => new(run.ClassificationTypeName, run.Text, (ClassifiedTextRunStyle)run.Style, markerTagType: null, run.NavigationAction, run.Tooltip);

    public static ClassifiedTextElement ToLSPElement(this QuickInfoClassifiedTextElement element)
        => new(element.Runs.Select(ToLSPRun));

    public static ContainerElement ToLSPElement(this QuickInfoContainerElement element)
        => new((ContainerElementStyle)element.Style, element.Elements.Select(ToLSPElement));

    private static object ToLSPElement(QuickInfoElement value)
    {
        return value switch
        {
            QuickInfoGlyphElement element => element.ToLSPElement(),
            QuickInfoContainerElement element => element.ToLSPElement(),
            QuickInfoClassifiedTextElement element => element.ToLSPElement(),

            _ => value
        };
    }

    /// <summary>
    /// Retrieves the <see cref="Guid"/> and id that can represent a particular <see cref="Glyph"/>
    /// in the Visual Studio client.
    /// </summary>
    /// <param name="glyph"></param>
    /// <exception cref="ArgumentException"></exception>
    public static (Guid guid, int id) GetVsImageData(this Glyph glyph)
    {
        return glyph switch
        {
            Glyph.None => default,

            Glyph.Assembly => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Assembly),

            Glyph.BasicFile => (KnownImageIds.ImageCatalogGuid, KnownImageIds.VBFileNode),
            Glyph.BasicProject => (KnownImageIds.ImageCatalogGuid, KnownImageIds.VBProjectNode),

            Glyph.ClassPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassPublic),
            Glyph.ClassProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassProtected),
            Glyph.ClassPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassPrivate),
            Glyph.ClassInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassInternal),

            Glyph.CSharpFile => (KnownImageIds.ImageCatalogGuid, KnownImageIds.CSFileNode),
            Glyph.CSharpProject => (KnownImageIds.ImageCatalogGuid, KnownImageIds.CSProjectNode),

            Glyph.CompletionWarning => (KnownImageIds.ImageCatalogGuid, KnownImageIds.IntellisenseWarning),

            Glyph.ConstantPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantPublic),
            Glyph.ConstantProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantProtected),
            Glyph.ConstantPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantPrivate),
            Glyph.ConstantInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantInternal),

            Glyph.DelegatePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegatePublic),
            Glyph.DelegateProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegateProtected),
            Glyph.DelegatePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegatePrivate),
            Glyph.DelegateInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegateInternal),

            Glyph.EnumPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationPublic),
            Glyph.EnumProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationProtected),
            Glyph.EnumPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationPrivate),
            Glyph.EnumInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationInternal),

            Glyph.EnumMemberPublic or
            Glyph.EnumMemberProtected or
            Glyph.EnumMemberPrivate or
            Glyph.EnumMemberInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationItemPublic),

            Glyph.Error => (KnownImageIds.ImageCatalogGuid, KnownImageIds.StatusError),

            Glyph.EventPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventPublic),
            Glyph.EventProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventProtected),
            Glyph.EventPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventPrivate),
            Glyph.EventInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventInternal),

            // Extension methods have the same glyph regardless of accessibility.
            Glyph.ExtensionMethodPublic or
            Glyph.ExtensionMethodProtected or
            Glyph.ExtensionMethodPrivate or
            Glyph.ExtensionMethodInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ExtensionMethod),

            Glyph.FieldPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPublic),
            Glyph.FieldProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldProtected),
            Glyph.FieldPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPrivate),
            Glyph.FieldInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldInternal),

            Glyph.InterfacePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfacePublic),
            Glyph.InterfaceProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfaceProtected),
            Glyph.InterfacePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfacePrivate),
            Glyph.InterfaceInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfaceInternal),

            // TODO: Figure out the right thing to return here.
            Glyph.Intrinsic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Type),

            Glyph.Keyword => (KnownImageIds.ImageCatalogGuid, KnownImageIds.IntellisenseKeyword),

            Glyph.Label => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Label),

            Glyph.MethodPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPublic),
            Glyph.MethodProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodProtected),
            Glyph.MethodPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate),
            Glyph.MethodInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodInternal),

            Glyph.ModulePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModulePublic),
            Glyph.ModuleProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModuleProtected),
            Glyph.ModulePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModulePrivate),
            Glyph.ModuleInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModuleInternal),

            Glyph.Namespace => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Namespace),

            Glyph.NuGet => (KnownImageIds.ImageCatalogGuid, KnownImageIds.NuGet),

            Glyph.OpenFolder => (KnownImageIds.ImageCatalogGuid, KnownImageIds.OpenFolder),

            Glyph.OperatorPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.OperatorPublic),
            Glyph.OperatorProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.OperatorProtected),
            Glyph.OperatorPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.OperatorPrivate),
            Glyph.OperatorInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.OperatorInternal),

            Glyph.Parameter or Glyph.Local => (KnownImageIds.ImageCatalogGuid, KnownImageIds.LocalVariable),

            Glyph.PropertyPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPublic),
            Glyph.PropertyProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyProtected),
            Glyph.PropertyPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPrivate),
            Glyph.PropertyInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyInternal),

            Glyph.RangeVariable => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPublic),

            Glyph.Reference => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Reference),

            Glyph.Snippet => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Snippet),

            Glyph.StatusInformation => (KnownImageIds.ImageCatalogGuid, KnownImageIds.StatusInformation),

            Glyph.StructurePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypePublic),
            Glyph.StructureProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypeProtected),
            Glyph.StructurePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypePrivate),
            Glyph.StructureInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypeInternal),

            Glyph.TargetTypeMatch => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MatchType),

            Glyph.TypeParameter => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Type),

            _ => throw new ArgumentException($"Unknown glyph value: {glyph}", nameof(glyph)),
        };
    }
}
