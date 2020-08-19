// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class ProtocolConversions
    {
        public static readonly Dictionary<string, LSP.CompletionItemKind> RoslynTagToCompletionItemKind = new Dictionary<string, LSP.CompletionItemKind>()
        {
            { WellKnownTags.Public, LSP.CompletionItemKind.Keyword },
            { WellKnownTags.Protected, LSP.CompletionItemKind.Keyword },
            { WellKnownTags.Private, LSP.CompletionItemKind.Keyword },
            { WellKnownTags.Internal, LSP.CompletionItemKind.Keyword },
            { WellKnownTags.File, LSP.CompletionItemKind.File },
            { WellKnownTags.Project, LSP.CompletionItemKind.File },
            { WellKnownTags.Folder, LSP.CompletionItemKind.Folder },
            { WellKnownTags.Assembly, LSP.CompletionItemKind.File },
            { WellKnownTags.Class, LSP.CompletionItemKind.Class },
            { WellKnownTags.Constant, LSP.CompletionItemKind.Constant },
            { WellKnownTags.Delegate, LSP.CompletionItemKind.Function },
            { WellKnownTags.Enum, LSP.CompletionItemKind.Enum },
            { WellKnownTags.EnumMember, LSP.CompletionItemKind.EnumMember },
            { WellKnownTags.Event, LSP.CompletionItemKind.Event },
            { WellKnownTags.ExtensionMethod, LSP.CompletionItemKind.Method },
            { WellKnownTags.Field, LSP.CompletionItemKind.Field },
            { WellKnownTags.Interface, LSP.CompletionItemKind.Interface },
            { WellKnownTags.Intrinsic, LSP.CompletionItemKind.Text },
            { WellKnownTags.Keyword, LSP.CompletionItemKind.Keyword },
            { WellKnownTags.Label, LSP.CompletionItemKind.Text },
            { WellKnownTags.Local, LSP.CompletionItemKind.Variable },
            { WellKnownTags.Namespace, LSP.CompletionItemKind.Text },
            { WellKnownTags.Method, LSP.CompletionItemKind.Method },
            { WellKnownTags.Module, LSP.CompletionItemKind.Module },
            { WellKnownTags.Operator, LSP.CompletionItemKind.Operator },
            { WellKnownTags.Parameter, LSP.CompletionItemKind.Value },
            { WellKnownTags.Property, LSP.CompletionItemKind.Property },
            { WellKnownTags.RangeVariable, LSP.CompletionItemKind.Variable },
            { WellKnownTags.Reference, LSP.CompletionItemKind.Reference },
            { WellKnownTags.Structure, LSP.CompletionItemKind.Struct },
            { WellKnownTags.TypeParameter, LSP.CompletionItemKind.TypeParameter },
            { WellKnownTags.Snippet, LSP.CompletionItemKind.Snippet },
            { WellKnownTags.Error, LSP.CompletionItemKind.Text },
            { WellKnownTags.Warning, LSP.CompletionItemKind.Text },
            { WellKnownTags.StatusInformation, LSP.CompletionItemKind.Text },
            { WellKnownTags.AddReference, LSP.CompletionItemKind.Text },
            { WellKnownTags.NuGet, LSP.CompletionItemKind.Text }
        };

        public static Uri GetUriFromFilePath(string? filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return new Uri(filePath, UriKind.Absolute);
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
            => new LSP.TextDocumentIdentifier() { Uri = document.GetURI() };

        public static LinePosition PositionToLinePosition(LSP.Position position)
            => new LinePosition(position.Line, position.Character);

        public static LinePositionSpan RangeToLinePositionSpan(LSP.Range range)
            => new LinePositionSpan(PositionToLinePosition(range.Start), PositionToLinePosition(range.End));

        public static TextSpan RangeToTextSpan(LSP.Range range, SourceText text)
        {
            var linePositionSpan = RangeToLinePositionSpan(range);
            return text.Lines.GetTextSpan(linePositionSpan);
        }

        public static LSP.TextEdit TextChangeToTextEdit(TextChange textChange, SourceText text)
        {
            return new LSP.TextEdit
            {
                NewText = textChange.NewText,
                Range = TextSpanToRange(textChange.Span, text)
            };
        }

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
            => TextSpanToLocationAsync(documentSpan.Document, documentSpan.SourceSpan, cancellationToken);

        public static async Task<LSP.LocationWithText?> DocumentSpanToLocationWithTextAsync(DocumentSpan documentSpan, ClassifiedTextElement text, CancellationToken cancellationToken)
        {
            var location = await TextSpanToLocationAsync(documentSpan.Document, documentSpan.SourceSpan, cancellationToken).ConfigureAwait(false);

            return location == null ? null : new LSP.LocationWithText
            {
                Uri = location.Uri,
                Range = location.Range,
                Text = text
            };
        }

        public static async Task<LSP.Location?> TextSpanToLocationAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var spanMappingService = document.Services.GetService<ISpanMappingService>();

            if (spanMappingService == null)
            {
                return await ConvertTextSpanToLocation(document, textSpan, cancellationToken).ConfigureAwait(false);
            }

            var mappedSpanResult = await spanMappingService.MapSpansAsync(document, SpecializedCollections.SingletonEnumerable(textSpan), cancellationToken).ConfigureAwait(false);
            if (mappedSpanResult.IsDefaultOrEmpty)
            {
                return await ConvertTextSpanToLocation(document, textSpan, cancellationToken).ConfigureAwait(false);
            }

            var mappedSpan = mappedSpanResult.Single();
            if (mappedSpan.IsDefault)
            {
                return null;
            }

            return new LSP.Location
            {
                Uri = GetUriFromFilePath(mappedSpan.FilePath),
                Range = MappedSpanResultToRange(mappedSpan)
            };

            static async Task<LSP.Location> ConvertTextSpanToLocation(Document document, TextSpan span, CancellationToken cancellationToken)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                return ConvertTextSpanWithTextToLocation(span, text, document.GetURI());
            }

            static LSP.Range MappedSpanResultToRange(MappedSpanResult mappedSpanResult)
            {
                return new LSP.Range
                {
                    Start = LinePositionToPosition(mappedSpanResult.LinePositionSpan.Start),
                    End = LinePositionToPosition(mappedSpanResult.LinePositionSpan.End)
                };
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

        public static LSP.DiagnosticSeverity DiagnosticSeverityToLspDiagnositcSeverity(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                // TO-DO: Add new LSP diagnostic severity for hidden diagnostics
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1063158
                case DiagnosticSeverity.Hidden:
                    return LSP.DiagnosticSeverity.Hint;
                case DiagnosticSeverity.Info:
                    return LSP.DiagnosticSeverity.Hint;
                case DiagnosticSeverity.Warning:
                    return LSP.DiagnosticSeverity.Warning;
                case DiagnosticSeverity.Error:
                    return LSP.DiagnosticSeverity.Error;
                default:
                    throw ExceptionUtilities.UnexpectedValue(severity);
            }
        }

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
                    return LSP.SymbolKind.Function;
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
                    return Glyph.MethodPublic;
                case LSP.CompletionItemKind.Function:
                    return Glyph.DelegatePublic;
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
        public static LSP.ReferenceKind[] SymbolUsageInfoToReferenceKinds(SymbolUsageInfo symbolUsageInfo)
        {
            var referenceKinds = ArrayBuilder<LSP.ReferenceKind>.GetInstance();
            if (symbolUsageInfo.ValueUsageInfoOpt.HasValue)
            {
                var usageInfo = symbolUsageInfo.ValueUsageInfoOpt.Value;
                if (usageInfo.IsReadFrom())
                {
                    referenceKinds.Add(LSP.ReferenceKind.Read);
                }

                if (usageInfo.IsWrittenTo())
                {
                    referenceKinds.Add(LSP.ReferenceKind.Write);
                }

                if (usageInfo.IsReference())
                {
                    referenceKinds.Add(LSP.ReferenceKind.Reference);
                }

                if (usageInfo.IsNameOnly())
                {
                    referenceKinds.Add(LSP.ReferenceKind.Name);
                }
            }

            if (symbolUsageInfo.TypeOrNamespaceUsageInfoOpt.HasValue)
            {
                var usageInfo = symbolUsageInfo.TypeOrNamespaceUsageInfoOpt.Value;
                if ((usageInfo & TypeOrNamespaceUsageInfo.Qualified) != 0)
                {
                    referenceKinds.Add(LSP.ReferenceKind.Qualified);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.TypeArgument) != 0)
                {
                    referenceKinds.Add(LSP.ReferenceKind.TypeArgument);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.TypeConstraint) != 0)
                {
                    referenceKinds.Add(LSP.ReferenceKind.TypeConstraint);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.Base) != 0)
                {
                    referenceKinds.Add(LSP.ReferenceKind.BaseType);
                }

                // Preserving the same mapping logic that SymbolUsageInfoExtensions.ToSymbolReferenceKinds uses
                if ((usageInfo & TypeOrNamespaceUsageInfo.ObjectCreation) != 0)
                {
                    referenceKinds.Add(LSP.ReferenceKind.Constructor);
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.Import) != 0)
                {
                    referenceKinds.Add(LSP.ReferenceKind.Import);
                }

                // Preserving the same mapping logic that SymbolUsageInfoExtensions.ToSymbolReferenceKinds uses
                if ((usageInfo & TypeOrNamespaceUsageInfo.NamespaceDeclaration) != 0)
                {
                    referenceKinds.Add(LSP.ReferenceKind.Declaration);
                }
            }

            return referenceKinds.ToArrayAndFree();
        }

        public static string ProjectIdToProjectContextId(ProjectId id)
        {
            return id.Id + "|" + id.DebugName;
        }

        public static ProjectId ProjectContextToProjectId(ProjectContext projectContext)
        {
            var delimiter = projectContext.Id.IndexOf('|');

            return ProjectId.CreateFromSerialized(
                Guid.Parse(projectContext.Id.Substring(0, delimiter)),
                debugName: projectContext.Id.Substring(delimiter + 1));
        }
    }
}
