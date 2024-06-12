// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using Logger = Microsoft.CodeAnalysis.Internal.Log.Logger;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class ProtocolConversions
    {
        private const string CSharpMarkdownLanguageName = "csharp";
        private const string VisualBasicMarkdownLanguageName = "vb";
        private const string SourceGeneratedDocumentBaseUri = "source-generated:///";

#pragma warning disable RS0030 // Do not use banned APIs
        private static readonly Uri s_sourceGeneratedDocumentBaseUri = new(SourceGeneratedDocumentBaseUri, UriKind.Absolute);
#pragma warning restore

        private static readonly char[] s_dirSeparators = new[] { PathUtilities.DirectorySeparatorChar, PathUtilities.AltDirectorySeparatorChar };

        private static readonly Regex s_markdownEscapeRegex = new(@"([\\`\*_\{\}\[\]\(\)#+\-\.!])", RegexOptions.Compiled);

        // NOTE: While the spec allows it, don't use Function and Method, as both VS and VS Code display them the same
        // way which can confuse users

        /// <summary>
        /// Mapping from tags to lsp completion item kinds.  The value lists the potential lsp kinds from
        /// least-preferred to most preferred.  More preferred kinds will be chosen if the client states they support
        /// it.  This mapping allows values including extensions to the kinds defined by VS (but not in the core LSP
        /// spec).
        /// </summary>
        public static readonly ImmutableDictionary<string, ImmutableArray<LSP.CompletionItemKind>> RoslynTagToCompletionItemKinds = new Dictionary<string, ImmutableArray<LSP.CompletionItemKind>>()
        {
            { WellKnownTags.Public, ImmutableArray.Create(LSP.CompletionItemKind.Keyword) },
            { WellKnownTags.Protected, ImmutableArray.Create(LSP.CompletionItemKind.Keyword) },
            { WellKnownTags.Private, ImmutableArray.Create(LSP.CompletionItemKind.Keyword) },
            { WellKnownTags.Internal, ImmutableArray.Create(LSP.CompletionItemKind.Keyword) },
            { WellKnownTags.File, ImmutableArray.Create(LSP.CompletionItemKind.File) },
            { WellKnownTags.Project, ImmutableArray.Create(LSP.CompletionItemKind.File) },
            { WellKnownTags.Folder, ImmutableArray.Create(LSP.CompletionItemKind.Folder) },
            { WellKnownTags.Assembly, ImmutableArray.Create(LSP.CompletionItemKind.File) },
            { WellKnownTags.Class, ImmutableArray.Create(LSP.CompletionItemKind.Class) },
            { WellKnownTags.Constant, ImmutableArray.Create(LSP.CompletionItemKind.Constant) },
            { WellKnownTags.Delegate, ImmutableArray.Create(LSP.CompletionItemKind.Class, LSP.CompletionItemKind.Delegate) },
            { WellKnownTags.Enum, ImmutableArray.Create(LSP.CompletionItemKind.Enum) },
            { WellKnownTags.EnumMember, ImmutableArray.Create(LSP.CompletionItemKind.EnumMember) },
            { WellKnownTags.Event, ImmutableArray.Create(LSP.CompletionItemKind.Event) },
            { WellKnownTags.ExtensionMethod, ImmutableArray.Create(LSP.CompletionItemKind.Method, LSP.CompletionItemKind.ExtensionMethod) },
            { WellKnownTags.Field, ImmutableArray.Create(LSP.CompletionItemKind.Field) },
            { WellKnownTags.Interface, ImmutableArray.Create(LSP.CompletionItemKind.Interface) },
            { WellKnownTags.Intrinsic, ImmutableArray.Create(LSP.CompletionItemKind.Text) },
            { WellKnownTags.Keyword, ImmutableArray.Create(LSP.CompletionItemKind.Keyword) },
            { WellKnownTags.Label, ImmutableArray.Create(LSP.CompletionItemKind.Text) },
            { WellKnownTags.Local, ImmutableArray.Create(LSP.CompletionItemKind.Variable) },
            { WellKnownTags.Namespace, ImmutableArray.Create(LSP.CompletionItemKind.Module, LSP.CompletionItemKind.Namespace) },
            { WellKnownTags.Method, ImmutableArray.Create(LSP.CompletionItemKind.Method) },
            { WellKnownTags.Module, ImmutableArray.Create(LSP.CompletionItemKind.Module) },
            { WellKnownTags.Operator, ImmutableArray.Create(LSP.CompletionItemKind.Operator) },
            { WellKnownTags.Parameter, ImmutableArray.Create(LSP.CompletionItemKind.Value) },
            { WellKnownTags.Property, ImmutableArray.Create(LSP.CompletionItemKind.Property) },
            { WellKnownTags.RangeVariable, ImmutableArray.Create(LSP.CompletionItemKind.Variable) },
            { WellKnownTags.Reference, ImmutableArray.Create(LSP.CompletionItemKind.Reference) },
            { WellKnownTags.Structure, ImmutableArray.Create(LSP.CompletionItemKind.Struct) },
            { WellKnownTags.TypeParameter, ImmutableArray.Create(LSP.CompletionItemKind.TypeParameter) },
            { WellKnownTags.Snippet, ImmutableArray.Create(LSP.CompletionItemKind.Snippet) },
            { WellKnownTags.Error, ImmutableArray.Create(LSP.CompletionItemKind.Text) },
            { WellKnownTags.Warning, ImmutableArray.Create(LSP.CompletionItemKind.Text) },
            { WellKnownTags.StatusInformation, ImmutableArray.Create(LSP.CompletionItemKind.Text) },
            { WellKnownTags.AddReference, ImmutableArray.Create(LSP.CompletionItemKind.Text) },
            { WellKnownTags.NuGet, ImmutableArray.Create(LSP.CompletionItemKind.Text) }
        }.ToImmutableDictionary();

        // TO-DO: More LSP.CompletionTriggerKind mappings are required to properly map to Roslyn CompletionTriggerKinds.
        // https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1178726
        public static async Task<Completion.CompletionTrigger> LSPToRoslynCompletionTriggerAsync(
            LSP.CompletionContext? context,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            if (context is null)
            {
                // Some LSP clients don't support sending extra context, so all we can do is invoke
                return Completion.CompletionTrigger.Invoke;
            }
            else if (context.TriggerKind is LSP.CompletionTriggerKind.Invoked or LSP.CompletionTriggerKind.TriggerForIncompleteCompletions)
            {
                if (context is not LSP.VSInternalCompletionContext vsCompletionContext)
                {
                    return Completion.CompletionTrigger.Invoke;
                }

                switch (vsCompletionContext.InvokeKind)
                {
                    case LSP.VSInternalCompletionInvokeKind.Explicit:
                        return Completion.CompletionTrigger.Invoke;

                    case LSP.VSInternalCompletionInvokeKind.Typing:
                        var insertionChar = await GetInsertionCharacterAsync(document, position, cancellationToken).ConfigureAwait(false);
                        return Completion.CompletionTrigger.CreateInsertionTrigger(insertionChar);

                    case LSP.VSInternalCompletionInvokeKind.Deletion:
                        Contract.ThrowIfNull(context.TriggerCharacter);
                        Contract.ThrowIfFalse(char.TryParse(context.TriggerCharacter, out var triggerChar));
                        return Completion.CompletionTrigger.CreateDeletionTrigger(triggerChar);

                    default:
                        // LSP added an InvokeKind that we need to support.
                        Logger.Log(FunctionId.LSPCompletion_MissingLSPCompletionInvokeKind);
                        return Completion.CompletionTrigger.Invoke;
                }
            }
            else if (context.TriggerKind is LSP.CompletionTriggerKind.TriggerCharacter)
            {
                Contract.ThrowIfNull(context.TriggerCharacter);
                Contract.ThrowIfFalse(char.TryParse(context.TriggerCharacter, out var triggerChar));
                return Completion.CompletionTrigger.CreateInsertionTrigger(triggerChar);
            }
            else
            {
                // LSP added a TriggerKind that we need to support.
                Logger.Log(FunctionId.LSPCompletion_MissingLSPCompletionTriggerKind);
                return Completion.CompletionTrigger.Invoke;
            }

            // Local functions
            static async Task<char> GetInsertionCharacterAsync(Document document, int position, CancellationToken cancellationToken)
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                // We use 'position - 1' here since we want to find the character that was just inserted.
                Contract.ThrowIfTrue(position < 1);
                var triggerCharacter = text[position - 1];
                return triggerCharacter;
            }
        }

        public static string GetDocumentFilePathFromUri(Uri uri)
            => uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;

        /// <summary>
        /// Converts an absolute local file path or an absolute URL string to <see cref="Uri"/>.
        /// </summary>
        /// <exception cref="UriFormatException">
        /// The <paramref name="absolutePath"/> can't be represented as <see cref="Uri"/>.
        /// For example, UNC paths with invalid characters in server name.
        /// </exception>
        public static Uri CreateAbsoluteUri(string absolutePath)
        {
            var uriString = IsAscii(absolutePath) ? absolutePath : GetAbsoluteUriString(absolutePath);
            try
            {
#pragma warning disable RS0030 // Do not use banned APIs
                return new(uriString, UriKind.Absolute);
#pragma warning restore

            }
            catch (UriFormatException e)
            {
                // The standard URI format exception does not include the failing path, however
                // in pretty much all cases we need to know the URI string (and original string) in order to fix the issue.
                throw new UriFormatException($"Failed create URI from '{uriString}'; original string: '{absolutePath}'", e);
            }
        }

        // Implements workaround for https://github.com/dotnet/runtime/issues/89538:
        internal static string GetAbsoluteUriString(string absolutePath)
        {
            if (!PathUtilities.IsAbsolute(absolutePath))
            {
                return absolutePath;
            }

            var parts = absolutePath.Split(s_dirSeparators);

            if (PathUtilities.IsUnixLikePlatform)
            {
                // Unix path: first part is empty, all parts should be escaped
                return "file://" + string.Join("/", parts.Select(EscapeUriPart));
            }

            if (parts is ["", "", var serverName, ..])
            {
                // UNC path: first non-empty part is server name and shouldn't be escaped
                return "file://" + serverName + "/" + string.Join("/", parts.Skip(3).Select(EscapeUriPart));
            }

            // Drive-rooted path: first part is "C:" and shouldn't be escaped
            return "file:///" + parts[0] + "/" + string.Join("/", parts.Skip(1).Select(EscapeUriPart));

#pragma warning disable SYSLIB0013 // Type or member is obsolete
            static string EscapeUriPart(string stringToEscape)
                => Uri.EscapeUriString(stringToEscape).Replace("#", "%23");
#pragma warning restore
        }

        public static Uri CreateUriFromSourceGeneratedFilePath(string filePath)
        {
            Debug.Assert(!PathUtilities.IsAbsolute(filePath));

            // Fast path for common cases:
            if (IsAscii(filePath))
            {
#pragma warning disable RS0030 // Do not use banned APIs
                return new Uri(s_sourceGeneratedDocumentBaseUri, filePath);
#pragma warning restore
            }

            // Workaround for https://github.com/dotnet/runtime/issues/89538:

            var parts = filePath.Split(s_dirSeparators);
            var url = SourceGeneratedDocumentBaseUri + string.Join("/", parts.Select(Uri.EscapeDataString));

#pragma warning disable RS0030 // Do not use banned APIs
            return new Uri(url, UriKind.Absolute);
#pragma warning restore
        }

        private static bool IsAscii(char c)
            => (uint)c <= '\x007f';

        private static bool IsAscii(string filePath)
        {
            for (var i = 0; i < filePath.Length; i++)
            {
                if (!IsAscii(filePath[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static LSP.TextDocumentPositionParams PositionToTextDocumentPositionParams(int position, SourceText text, Document document)
        {
            return new LSP.TextDocumentPositionParams()
            {
                TextDocument = DocumentToTextDocumentIdentifier(document),
                Position = LinePositionToPosition(text.Lines.GetLinePosition(position))
            };
        }

        public static LSP.TextDocumentIdentifier DocumentToTextDocumentIdentifier(Document document)
            => new LSP.TextDocumentIdentifier { Uri = document.GetURI() };

        public static LSP.VersionedTextDocumentIdentifier DocumentToVersionedTextDocumentIdentifier(Document document)
            => new LSP.VersionedTextDocumentIdentifier { Uri = document.GetURI() };

        public static LinePosition PositionToLinePosition(LSP.Position position)
            => new LinePosition(position.Line, position.Character);
        public static LinePositionSpan RangeToLinePositionSpan(LSP.Range range)
            => new(PositionToLinePosition(range.Start), PositionToLinePosition(range.End));

        public static TextSpan RangeToTextSpan(LSP.Range range, SourceText text)
        {
            var linePositionSpan = RangeToLinePositionSpan(range);

            try
            {
                try
                {
                    return text.Lines.GetTextSpan(linePositionSpan);
                }
                catch (ArgumentException ex)
                {
                    // Create a custom error for this so we can examine the data we're getting.
                    throw new ArgumentException($"Range={RangeToString(range)}. text.Length={text.Length}. text.Lines.Count={text.Lines.Count}", ex);
                }
            }
            // Temporary exception reporting to investigate https://github.com/dotnet/roslyn/issues/66258.
            catch (Exception e) when (FatalError.ReportAndPropagate(e))
            {
                throw;
            }

            static string RangeToString(LSP.Range range)
                => $"{{ Start={PositionToString(range.Start)}, End={PositionToString(range.End)} }}";

            static string PositionToString(LSP.Position position)
                => $"{{ Line={position.Line}, Character={position.Character} }}";
        }

        public static LSP.TextEdit TextChangeToTextEdit(TextChange textChange, SourceText oldText)
        {
            Contract.ThrowIfNull(textChange.NewText);
            return new LSP.TextEdit
            {
                NewText = textChange.NewText,
                Range = TextSpanToRange(textChange.Span, oldText)
            };
        }

        public static TextChange TextEditToTextChange(LSP.TextEdit edit, SourceText oldText)
            => new TextChange(RangeToTextSpan(edit.Range, oldText), edit.NewText);

        public static TextChange ContentChangeEventToTextChange(LSP.TextDocumentContentChangeEvent changeEvent, SourceText text)
            => new TextChange(RangeToTextSpan(changeEvent.Range, text), changeEvent.Text);

        public static LSP.Position LinePositionToPosition(LinePosition linePosition)
            => new LSP.Position { Line = linePosition.Line, Character = linePosition.Character };

        public static LSP.Range LinePositionToRange(LinePositionSpan linePositionSpan)
            => new LSP.Range { Start = LinePositionToPosition(linePositionSpan.Start), End = LinePositionToPosition(linePositionSpan.End) };

        public static LSP.Range TextSpanToRange(TextSpan textSpan, SourceText text)
        {
            var linePosSpan = text.Lines.GetLinePositionSpan(textSpan);
            return LinePositionToRange(linePosSpan);
        }

        public static Task<LSP.Location?> DocumentSpanToLocationAsync(DocumentSpan documentSpan, CancellationToken cancellationToken)
            => TextSpanToLocationAsync(documentSpan.Document, documentSpan.SourceSpan, isStale: false, cancellationToken);

        public static async Task<LSP.VSInternalLocation?> DocumentSpanToLocationWithTextAsync(
            DocumentSpan documentSpan, ClassifiedTextElement text, CancellationToken cancellationToken)
        {
            var location = await TextSpanToLocationAsync(
                documentSpan.Document, documentSpan.SourceSpan, isStale: false, cancellationToken).ConfigureAwait(false);

            return location == null ? null : new LSP.VSInternalLocation
            {
                Uri = location.Uri,
                Range = location.Range,
                Text = text
            };
        }

        /// <summary>
        /// Compute all the <see cref="LSP.TextDocumentEdit"/> for the input list of changed documents.
        /// Additionally maps the locations of the changed documents if necessary.
        /// </summary>
        public static async Task<LSP.TextDocumentEdit[]> ChangedDocumentsToTextDocumentEditsAsync<T>(IEnumerable<DocumentId> changedDocuments, Func<DocumentId, T> getNewDocumentFunc,
                Func<DocumentId, T> getOldDocumentFunc, IDocumentTextDifferencingService? textDiffService, CancellationToken cancellationToken) where T : TextDocument
        {
            using var _ = ArrayBuilder<(Uri Uri, LSP.TextEdit TextEdit)>.GetInstance(out var uriToTextEdits);

            foreach (var docId in changedDocuments)
            {
                var newDocument = getNewDocumentFunc(docId);
                var oldDocument = getOldDocumentFunc(docId);

                var oldText = await oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                ImmutableArray<TextChange> textChanges;

                // Normal documents have a unique service for calculating minimal text edits. If we used the standard 'GetTextChanges'
                // method instead, we would get a change that spans the entire document, which we ideally want to avoid.
                if (newDocument is Document newDoc && oldDocument is Document oldDoc)
                {
                    Contract.ThrowIfNull(textDiffService);
                    textChanges = await textDiffService.GetTextChangesAsync(oldDoc, newDoc, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                    textChanges = newText.GetTextChanges(oldText).ToImmutableArray();
                }

                // Map all the text changes' spans for this document.
                var mappedResults = await GetMappedSpanResultAsync(oldDocument, textChanges.Select(tc => tc.Span).ToImmutableArray(), cancellationToken).ConfigureAwait(false);
                if (mappedResults == null)
                {
                    // There's no span mapping available, just create text edits from the original text changes.
                    foreach (var textChange in textChanges)
                    {
                        uriToTextEdits.Add((oldDocument.GetURI(), TextChangeToTextEdit(textChange, oldText)));
                    }
                }
                else
                {
                    // We have mapping results, so create text edits from the mapped text change spans.
                    for (var i = 0; i < textChanges.Length; i++)
                    {
                        var mappedSpan = mappedResults.Value[i];
                        var textChange = textChanges[i];
                        if (!mappedSpan.IsDefault)
                        {
                            uriToTextEdits.Add((CreateAbsoluteUri(mappedSpan.FilePath), new LSP.TextEdit
                            {
                                Range = MappedSpanResultToRange(mappedSpan),
                                NewText = textChange.NewText ?? string.Empty
                            }));
                        }
                    }
                }
            }

            var documentEdits = uriToTextEdits.GroupBy(uriAndEdit => uriAndEdit.Uri, uriAndEdit => uriAndEdit.TextEdit, (uri, edits) => new LSP.TextDocumentEdit
            {
                TextDocument = new LSP.OptionalVersionedTextDocumentIdentifier { Uri = uri },
                Edits = edits.ToArray(),
            }).ToArray();

            return documentEdits;
        }

        public static Task<LSP.Location?> TextSpanToLocationAsync(
            Document document,
            TextSpan textSpan,
            bool isStale,
            CancellationToken cancellationToken)
        {
            return TextSpanToLocationAsync(document, textSpan, isStale, context: null, cancellationToken);
        }

        public static async Task<LSP.Location?> TextSpanToLocationAsync(
            Document document,
            TextSpan textSpan,
            bool isStale,
            RequestContext? context,
            CancellationToken cancellationToken)
        {
            Debug.Assert(document.FilePath != null);

            var result = await GetMappedSpanResultAsync(document, ImmutableArray.Create(textSpan), cancellationToken).ConfigureAwait(false);
            if (result == null)
                return await ConvertTextSpanToLocation(document, textSpan, isStale, cancellationToken).ConfigureAwait(false);

            var mappedSpan = result.Value.Single();
            if (mappedSpan.IsDefault)
                return await ConvertTextSpanToLocation(document, textSpan, isStale, cancellationToken).ConfigureAwait(false);

            Uri? uri = null;
            try
            {
                if (PathUtilities.IsAbsolute(mappedSpan.FilePath))
                    uri = CreateAbsoluteUri(mappedSpan.FilePath);
            }
            catch (UriFormatException)
            {
            }

            if (uri == null)
            {
                context?.TraceInformation($"Could not convert '{mappedSpan.FilePath}' to uri");
                return null;
            }

            return new LSP.Location
            {
                Uri = uri,
                Range = MappedSpanResultToRange(mappedSpan)
            };

            static async Task<LSP.Location?> ConvertTextSpanToLocation(
                Document document,
                TextSpan span,
                bool isStale,
                CancellationToken cancellationToken)
            {
                Debug.Assert(document.FilePath != null);
                var uri = CreateAbsoluteUri(document.FilePath);

                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                if (isStale)
                {
                    // in the case of a stale item, the span may be out of bounds of the document. Cap
                    // us to the end of the document as that's where we're going to navigate the user
                    // to.
                    span = TextSpan.FromBounds(
                        Math.Min(text.Length, span.Start),
                        Math.Min(text.Length, span.End));
                }

                return ConvertTextSpanWithTextToLocation(span, text, uri);
            }

            static LSP.Location ConvertTextSpanWithTextToLocation(TextSpan span, SourceText text, Uri documentUri)
            {
                var location = new LSP.Location
                {
                    Uri = documentUri,
                    Range = TextSpanToRange(span, text),
                };

                return location;
            }
        }

        public static LSP.CodeDescription? HelpLinkToCodeDescription(Uri? uri)
            => (uri != null) ? new LSP.CodeDescription { Href = uri } : null;

        public static LSP.SymbolKind NavigateToKindToSymbolKind(string kind)
        {
            if (Enum.TryParse<LSP.SymbolKind>(kind, out var symbolKind))
            {
                return symbolKind;
            }

            // TODO - Define conversion from NavigateToItemKind to LSP Symbol kind
            switch (kind)
            {
                case NavigateToItemKind.EnumItem:
                    return LSP.SymbolKind.EnumMember;
                case NavigateToItemKind.Structure:
                    return LSP.SymbolKind.Struct;
                case NavigateToItemKind.Delegate:
                    return LSP.SymbolKind.Function;
                default:
                    return LSP.SymbolKind.Object;
            }
        }

        public static LSP.DocumentHighlightKind HighlightSpanKindToDocumentHighlightKind(HighlightSpanKind kind)
        {
            switch (kind)
            {
                case HighlightSpanKind.Reference:
                    return LSP.DocumentHighlightKind.Read;
                case HighlightSpanKind.WrittenReference:
                    return LSP.DocumentHighlightKind.Write;
                default:
                    return LSP.DocumentHighlightKind.Text;
            }
        }

        public static Glyph SymbolKindToGlyph(LSP.SymbolKind kind)
        {
            switch (kind)
            {
                case LSP.SymbolKind.File:
                    return Glyph.CSharpFile;
                case LSP.SymbolKind.Module:
                    return Glyph.ModulePublic;
                case LSP.SymbolKind.Namespace:
                    return Glyph.Namespace;
                case LSP.SymbolKind.Package:
                    return Glyph.Assembly;
                case LSP.SymbolKind.Class:
                    return Glyph.ClassPublic;
                case LSP.SymbolKind.Method:
                    return Glyph.MethodPublic;
                case LSP.SymbolKind.Property:
                    return Glyph.PropertyPublic;
                case LSP.SymbolKind.Field:
                    return Glyph.FieldPublic;
                case LSP.SymbolKind.Constructor:
                    return Glyph.MethodPublic;
                case LSP.SymbolKind.Enum:
                    return Glyph.EnumPublic;
                case LSP.SymbolKind.Interface:
                    return Glyph.InterfacePublic;
                case LSP.SymbolKind.Function:
                    return Glyph.DelegatePublic;
                case LSP.SymbolKind.Variable:
                    return Glyph.Local;
                case LSP.SymbolKind.Constant:
                case LSP.SymbolKind.Number:
                    return Glyph.ConstantPublic;
                case LSP.SymbolKind.String:
                case LSP.SymbolKind.Boolean:
                case LSP.SymbolKind.Array:
                case LSP.SymbolKind.Object:
                case LSP.SymbolKind.Key:
                case LSP.SymbolKind.Null:
                    return Glyph.Local;
                case LSP.SymbolKind.EnumMember:
                    return Glyph.EnumMemberPublic;
                case LSP.SymbolKind.Struct:
                    return Glyph.StructurePublic;
                case LSP.SymbolKind.Event:
                    return Glyph.EventPublic;
                case LSP.SymbolKind.Operator:
                    return Glyph.Operator;
                case LSP.SymbolKind.TypeParameter:
                    return Glyph.TypeParameter;
                default:
                    return Glyph.None;
            }
        }

        public static LSP.SymbolKind GlyphToSymbolKind(Glyph glyph)
        {
            // Glyph kinds have accessibility modifiers in their name, e.g. ClassPrivate.
            // Remove the accessibility modifier and try to convert to LSP symbol kind.
            var glyphString = glyph.ToString().Replace(nameof(Accessibility.Public), string.Empty)
                                              .Replace(nameof(Accessibility.Protected), string.Empty)
                                              .Replace(nameof(Accessibility.Private), string.Empty)
                                              .Replace(nameof(Accessibility.Internal), string.Empty);

            if (Enum.TryParse<LSP.SymbolKind>(glyphString, out var symbolKind))
            {
                return symbolKind;
            }

            switch (glyph)
            {
                case Glyph.Assembly:
                case Glyph.BasicProject:
                case Glyph.CSharpProject:
                case Glyph.NuGet:
                    return LSP.SymbolKind.Package;
                case Glyph.BasicFile:
                case Glyph.CSharpFile:
                    return LSP.SymbolKind.File;
                case Glyph.DelegatePublic:
                case Glyph.DelegateProtected:
                case Glyph.DelegatePrivate:
                case Glyph.DelegateInternal:
                case Glyph.ExtensionMethodPublic:
                case Glyph.ExtensionMethodProtected:
                case Glyph.ExtensionMethodPrivate:
                case Glyph.ExtensionMethodInternal:
                    return LSP.SymbolKind.Method;
                case Glyph.Local:
                case Glyph.Parameter:
                case Glyph.RangeVariable:
                case Glyph.Reference:
                    return LSP.SymbolKind.Variable;
                case Glyph.StructurePublic:
                case Glyph.StructureProtected:
                case Glyph.StructurePrivate:
                case Glyph.StructureInternal:
                    return LSP.SymbolKind.Struct;
                default:
                    return LSP.SymbolKind.Object;
            }
        }

        public static Glyph CompletionItemKindToGlyph(LSP.CompletionItemKind kind)
        {
            switch (kind)
            {
                case LSP.CompletionItemKind.Text:
                    return Glyph.None;
                case LSP.CompletionItemKind.Method:
                case LSP.CompletionItemKind.Constructor:
                case LSP.CompletionItemKind.Function:    // We don't use Function, but map it just in case. It has the same icon as Method in VS and VS Code
                    return Glyph.MethodPublic;
                case LSP.CompletionItemKind.Field:
                    return Glyph.FieldPublic;
                case LSP.CompletionItemKind.Variable:
                case LSP.CompletionItemKind.Unit:
                case LSP.CompletionItemKind.Value:
                    return Glyph.Local;
                case LSP.CompletionItemKind.Class:
                    return Glyph.ClassPublic;
                case LSP.CompletionItemKind.Interface:
                    return Glyph.InterfacePublic;
                case LSP.CompletionItemKind.Module:
                    return Glyph.ModulePublic;
                case LSP.CompletionItemKind.Property:
                    return Glyph.PropertyPublic;
                case LSP.CompletionItemKind.Enum:
                    return Glyph.EnumPublic;
                case LSP.CompletionItemKind.Keyword:
                    return Glyph.Keyword;
                case LSP.CompletionItemKind.Snippet:
                    return Glyph.Snippet;
                case LSP.CompletionItemKind.Color:
                    return Glyph.None;
                case LSP.CompletionItemKind.File:
                    return Glyph.CSharpFile;
                case LSP.CompletionItemKind.Reference:
                    return Glyph.Reference;
                case LSP.CompletionItemKind.Folder:
                    return Glyph.OpenFolder;
                case LSP.CompletionItemKind.EnumMember:
                    return Glyph.EnumMemberPublic;
                case LSP.CompletionItemKind.Constant:
                    return Glyph.ConstantPublic;
                case LSP.CompletionItemKind.Struct:
                    return Glyph.StructurePublic;
                case LSP.CompletionItemKind.Event:
                    return Glyph.EventPublic;
                case LSP.CompletionItemKind.Operator:
                    return Glyph.Operator;
                case LSP.CompletionItemKind.TypeParameter:
                    return Glyph.TypeParameter;
                default:
                    return Glyph.None;
            }
        }

        // The mappings here are roughly based off of SymbolUsageInfoExtensions.ToSymbolReferenceKinds.
        public static LSP.VSInternalReferenceKind[] SymbolUsageInfoToReferenceKinds(SymbolUsageInfo symbolUsageInfo)
        {
            using var _ = ArrayBuilder<LSP.VSInternalReferenceKind>.GetInstance(out var referenceKinds);
            if (symbolUsageInfo.ValueUsageInfoOpt.HasValue)
            {
                var usageInfo = symbolUsageInfo.ValueUsageInfoOpt.Value;
                if (usageInfo.IsReadFrom())
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Read);
                }

                if (usageInfo.IsWrittenTo())
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Write);
                }

                if (usageInfo.IsReference())
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Reference);
                }

                if (usageInfo.IsNameOnly())
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Name);
                }
            }

            if (symbolUsageInfo.TypeOrNamespaceUsageInfoOpt.HasValue)
            {
                var usageInfo = symbolUsageInfo.TypeOrNamespaceUsageInfoOpt.Value;
                if ((usageInfo & TypeOrNamespaceUsageInfo.Qualified) != 0)
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Qualified);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.TypeArgument) != 0)
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.TypeArgument);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.TypeConstraint) != 0)
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.TypeConstraint);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.Base) != 0)
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.BaseType);
                }

                // Preserving the same mapping logic that SymbolUsageInfoExtensions.ToSymbolReferenceKinds uses
                if ((usageInfo & TypeOrNamespaceUsageInfo.ObjectCreation) != 0)
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Constructor);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.Import) != 0)
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Import);
                }

                // Preserving the same mapping logic that SymbolUsageInfoExtensions.ToSymbolReferenceKinds uses
                if ((usageInfo & TypeOrNamespaceUsageInfo.NamespaceDeclaration) != 0)
                {
                    referenceKinds.Add(LSP.VSInternalReferenceKind.Declaration);
                }
            }

            return referenceKinds.ToArray();
        }

        public static string ProjectIdToProjectContextId(ProjectId id)
        {
            return id.Id + "|" + id.DebugName;
        }

        public static ProjectId ProjectContextToProjectId(LSP.VSProjectContext projectContext)
        {
            var delimiter = projectContext.Id.IndexOf('|');

            return ProjectId.CreateFromSerialized(
                Guid.Parse(projectContext.Id[..delimiter]),
                debugName: projectContext.Id[(delimiter + 1)..]);
        }

        public static LSP.VSProjectContext ProjectToProjectContext(Project project)
        {
            var projectContext = new LSP.VSProjectContext
            {
                Id = ProjectIdToProjectContextId(project.Id),
                Label = project.Name
            };

            if (project.Language == LanguageNames.CSharp)
            {
                projectContext.Kind = LSP.VSProjectKind.CSharp;
            }
            else if (project.Language == LanguageNames.VisualBasic)
            {
                projectContext.Kind = LSP.VSProjectKind.VisualBasic;
            }

            return projectContext;
        }

        public static async Task<SyntaxFormattingOptions> GetFormattingOptionsAsync(
            LSP.FormattingOptions? options,
            Document document,
            IGlobalOptionService globalOptions,
            CancellationToken cancellationToken)
        {
            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(globalOptions, cancellationToken).ConfigureAwait(false);

            if (options != null)
            {
                // LSP doesn't currently support indent size as an option. However, except in special
                // circumstances, indent size is usually equivalent to tab size, so we'll just set it.
                formattingOptions = formattingOptions with
                {
                    LineFormatting = new()
                    {
                        UseTabs = !options.InsertSpaces,
                        TabSize = options.TabSize,
                        IndentationSize = options.TabSize,
                        NewLine = formattingOptions.NewLine
                    }
                };
            }

            return formattingOptions;
        }

        public static LSP.MarkupContent GetDocumentationMarkupContent(ImmutableArray<TaggedText> tags, Document document, bool featureSupportsMarkdown)
            => GetDocumentationMarkupContent(tags, document.Project.Language, featureSupportsMarkdown);

        public static LSP.MarkupContent GetDocumentationMarkupContent(ImmutableArray<TaggedText> tags, string language, bool featureSupportsMarkdown)
        {
            if (!featureSupportsMarkdown)
            {
                return new LSP.MarkupContent
                {
                    Kind = LSP.MarkupKind.PlainText,
                    Value = tags.GetFullText(),
                };
            }

            var builder = new StringBuilder();
            var isInCodeBlock = false;
            foreach (var taggedText in tags)
            {
                switch (taggedText.Tag)
                {
                    case TextTags.CodeBlockStart:
                        var codeBlockLanguageName = GetCodeBlockLanguageName(language);
                        builder.Append($"```{codeBlockLanguageName}{Environment.NewLine}");
                        builder.Append(taggedText.Text);
                        isInCodeBlock = true;
                        break;
                    case TextTags.CodeBlockEnd:
                        builder.Append($"{Environment.NewLine}```{Environment.NewLine}");
                        builder.Append(taggedText.Text);
                        isInCodeBlock = false;
                        break;
                    case TextTags.LineBreak:
                        // A line ending with double space and a new line indicates to markdown
                        // to render a single-spaced line break.
                        builder.Append("  ");
                        builder.Append(Environment.NewLine);
                        break;
                    default:
                        var styledText = GetStyledText(taggedText, isInCodeBlock);
                        builder.Append(styledText);
                        break;
                }
            }

            return new LSP.MarkupContent
            {
                Kind = LSP.MarkupKind.Markdown,
                Value = builder.ToString(),
            };

            static string GetCodeBlockLanguageName(string language)
            {
                return language switch
                {
                    (LanguageNames.CSharp) => CSharpMarkdownLanguageName,
                    (LanguageNames.VisualBasic) => VisualBasicMarkdownLanguageName,
                    _ => throw new InvalidOperationException($"{language} is not supported"),
                };
            }

            static string GetStyledText(TaggedText taggedText, bool isInCodeBlock)
            {
                var isCode = isInCodeBlock || taggedText.Style is TaggedTextStyle.Code;
                var text = isCode ? taggedText.Text : s_markdownEscapeRegex.Replace(taggedText.Text, @"\$1");

                // For non-cref links, the URI is present in both the hint and target.
                if (!string.IsNullOrEmpty(taggedText.NavigationHint) && taggedText.NavigationHint == taggedText.NavigationTarget)
                    return $"[{text}]({taggedText.NavigationHint})";

                // Markdown ignores spaces at the start of lines outside of code blocks, 
                // so we replace regular spaces with non-breaking spaces to ensure structural space is retained.
                // We want to use regular spaces everywhere else to allow the client to wrap long text.
                if (!isCode && taggedText.Tag is TextTags.Space or TextTags.ContainerStart)
                    text = text.Replace(" ", "&nbsp;");

                return taggedText.Style switch
                {
                    TaggedTextStyle.None => text,
                    TaggedTextStyle.Strong => $"**{text}**",
                    TaggedTextStyle.Emphasis => $"_{text}_",
                    TaggedTextStyle.Underline => $"<u>{text}</u>",
                    TaggedTextStyle.Code => $"`{text}`",
                    _ => text,
                };
            }
        }

        private static async Task<ImmutableArray<MappedSpanResult>?> GetMappedSpanResultAsync(TextDocument textDocument, ImmutableArray<TextSpan> textSpans, CancellationToken cancellationToken)
        {
            if (textDocument is not Document document)
            {
                return null;
            }

            var spanMappingService = document.Services.GetService<ISpanMappingService>();
            if (spanMappingService == null)
            {
                return null;
            }

            var mappedSpanResult = await spanMappingService.MapSpansAsync(document, textSpans, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfFalse(textSpans.Length == mappedSpanResult.Length,
                $"The number of input spans {textSpans.Length} should match the number of mapped spans returned {mappedSpanResult.Length}");
            return mappedSpanResult;
        }

        private static LSP.Range MappedSpanResultToRange(MappedSpanResult mappedSpanResult)
        {
            return new LSP.Range
            {
                Start = LinePositionToPosition(mappedSpanResult.LinePositionSpan.Start),
                End = LinePositionToPosition(mappedSpanResult.LinePositionSpan.End)
            };
        }
    }
}
