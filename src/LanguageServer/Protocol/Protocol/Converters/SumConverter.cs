// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class SumConverter : JsonConverterFactory
{
    // This cache is defined on the non-generic class so it is shared across all
    // SumConverter<T> instantiations rather than duplicated per generic type argument.
    internal static readonly ConcurrentDictionary<Type, SumTypeInfoCache> SumTypeCache = new();

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(ISumType).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // Keep closed converters statically visible to NGEN rather than constructing generic types at runtime.
#pragma warning disable CS0618 // Obsolete contracts remain part of the wire protocol.
        if (typeToConvert == typeof(SumType<bool, CallHierarchyOptions, CallHierarchyRegistrationOptions>))
            return new SumConverter<SumType<bool, CallHierarchyOptions, CallHierarchyRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, CodeActionOptions>))
            return new SumConverter<SumType<bool, CodeActionOptions>>();
        if (typeToConvert == typeof(SumType<bool, DeclarationOptions, DeclarationRegistrationOptions>))
            return new SumConverter<SumType<bool, DeclarationOptions, DeclarationRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, DefinitionOptions>))
            return new SumConverter<SumType<bool, DefinitionOptions>>();
        if (typeToConvert == typeof(SumType<bool, DocumentColorOptions, DocumentColorRegistrationOptions>))
            return new SumConverter<SumType<bool, DocumentColorOptions, DocumentColorRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, DocumentFormattingOptions>))
            return new SumConverter<SumType<bool, DocumentFormattingOptions>>();
        if (typeToConvert == typeof(SumType<bool, DocumentHighlightOptions>))
            return new SumConverter<SumType<bool, DocumentHighlightOptions>>();
        if (typeToConvert == typeof(SumType<bool, DocumentRangeFormattingOptions>))
            return new SumConverter<SumType<bool, DocumentRangeFormattingOptions>>();
        if (typeToConvert == typeof(SumType<bool, DocumentSymbolOptions>))
            return new SumConverter<SumType<bool, DocumentSymbolOptions>>();
        if (typeToConvert == typeof(SumType<bool, FoldingRangeOptions>))
            return new SumConverter<SumType<bool, FoldingRangeOptions>>();
        if (typeToConvert == typeof(SumType<bool, HoverOptions>))
            return new SumConverter<SumType<bool, HoverOptions>>();
        if (typeToConvert == typeof(SumType<bool, ImplementationOptions, ImplementationRegistrationOptions>))
            return new SumConverter<SumType<bool, ImplementationOptions, ImplementationRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, InlayHintOptions, InlayHintRegistrationOptions>))
            return new SumConverter<SumType<bool, InlayHintOptions, InlayHintRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, InlineCompletionOptions>))
            return new SumConverter<SumType<bool, InlineCompletionOptions>>();
        if (typeToConvert == typeof(SumType<bool, InlineValueOptions, InlineValueRegistrationOptions>))
            return new SumConverter<SumType<bool, InlineValueOptions, InlineValueRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, LinkedEditingRangeOptions>))
            return new SumConverter<SumType<bool, LinkedEditingRangeOptions>>();
        if (typeToConvert == typeof(SumType<bool, MonikerOptions, MonikerRegistrationOptions>))
            return new SumConverter<SumType<bool, MonikerOptions, MonikerRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, object>))
            return new SumConverter<SumType<bool, object>>();
        if (typeToConvert == typeof(SumType<bool, ReferenceOptions>))
            return new SumConverter<SumType<bool, ReferenceOptions>>();
        if (typeToConvert == typeof(SumType<bool, RenameOptions>))
            return new SumConverter<SumType<bool, RenameOptions>>();
        if (typeToConvert == typeof(SumType<bool, SaveOptions>))
            return new SumConverter<SumType<bool, SaveOptions>>();
        if (typeToConvert == typeof(SumType<bool, SelectionRangeOptions, SelectionRangeRegistrationOptions>))
            return new SumConverter<SumType<bool, SelectionRangeOptions, SelectionRangeRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, SemanticTokensRequestsFullSetting>))
            return new SumConverter<SumType<bool, SemanticTokensRequestsFullSetting>>();
        if (typeToConvert == typeof(SumType<bool, TypeDefinitionOptions, TypeDefinitionRegistrationOptions>))
            return new SumConverter<SumType<bool, TypeDefinitionOptions, TypeDefinitionRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, TypeHierarchyOptions, TypeHierarchyRegistrationOptions>))
            return new SumConverter<SumType<bool, TypeHierarchyOptions, TypeHierarchyRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<bool, WorkspaceSymbolOptions>))
            return new SumConverter<SumType<bool, WorkspaceSymbolOptions>>();
        if (typeToConvert == typeof(SumType<Command, CodeAction>))
            return new SumConverter<SumType<Command, CodeAction>>();
        if (typeToConvert == typeof(SumType<CompletionItem[], CompletionList>))
            return new SumConverter<SumType<CompletionItem[], CompletionList>>();
        if (typeToConvert == typeof(SumType<DiagnosticOptions, DiagnosticRegistrationOptions>))
            return new SumConverter<SumType<DiagnosticOptions, DiagnosticRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<DocumentSymbol[], SymbolInformation[]>))
            return new SumConverter<SumType<DocumentSymbol[], SymbolInformation[]>>();
        if (typeToConvert == typeof(SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>))
            return new SumConverter<SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>();
        if (typeToConvert == typeof(SumType<InlineCompletionItem[], InlineCompletionList>))
            return new SumConverter<SumType<InlineCompletionItem[], InlineCompletionList>>();
        if (typeToConvert == typeof(SumType<InlineValueText, InlineValueVariableLookup, InlineValueEvaluatableExpression>))
            return new SumConverter<SumType<InlineValueText, InlineValueVariableLookup, InlineValueEvaluatableExpression>>();
        if (typeToConvert == typeof(SumType<Location, Location[], LocationLink[]>))
            return new SumConverter<SumType<Location, Location[], LocationLink[]>>();
        if (typeToConvert == typeof(SumType<Location, Location[], DocumentLink[]>))
            return new SumConverter<SumType<Location, Location[], DocumentLink[]>>();
        if (typeToConvert == typeof(SumType<Location, WorkspaceSymbolLocation>))
            return new SumConverter<SumType<Location, WorkspaceSymbolLocation>>();
        if (typeToConvert == typeof(SumType<Location[], LocationLink[]>))
            return new SumConverter<SumType<Location[], LocationLink[]>>();
        if (typeToConvert == typeof(SumType<Location[], VSInternalReferenceItem[]>))
            return new SumConverter<SumType<Location[], VSInternalReferenceItem[]>>();
        if (typeToConvert == typeof(SumType<NotebookDocumentSyncOptions, NotebookDocumentSyncRegistrationOptions>))
            return new SumConverter<SumType<NotebookDocumentSyncOptions, NotebookDocumentSyncRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<Range, InsertReplaceRange>))
            return new SumConverter<SumType<Range, InsertReplaceRange>>();
        if (typeToConvert == typeof(SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>))
            return new SumConverter<SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>>();
        if (typeToConvert == typeof(SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport>))
            return new SumConverter<SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport>>();
        if (typeToConvert == typeof(SumType<RenameRange, DefaultBehaviorPrepareRename, Range>))
            return new SumConverter<SumType<RenameRange, DefaultBehaviorPrepareRename, Range>>();
        if (typeToConvert == typeof(SumType<SemanticTokens, SemanticTokensDelta>))
            return new SumConverter<SumType<SemanticTokens, SemanticTokensDelta>>();
        if (typeToConvert == typeof(SumType<SemanticTokensOptions, SemanticTokensRegistrationOptions>))
            return new SumConverter<SumType<SemanticTokensOptions, SemanticTokensRegistrationOptions>>();
        if (typeToConvert == typeof(SumType<SymbolInformation[], DocumentSymbol[]>))
            return new SumConverter<SumType<SymbolInformation[], DocumentSymbol[]>>();
        if (typeToConvert == typeof(SumType<SymbolInformation[], WorkspaceSymbol[]>))
            return new SumConverter<SumType<SymbolInformation[], WorkspaceSymbol[]>>();
        if (typeToConvert == typeof(SumType<TextDocumentContentChangePartial, TextDocumentContentChangeWholeDocument>))
            return new SumConverter<SumType<TextDocumentContentChangePartial, TextDocumentContentChangeWholeDocument>>();
        if (typeToConvert == typeof(SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>))
            return new SumConverter<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();
        if (typeToConvert == typeof(SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]>))
            return new SumConverter<SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]>>();
        if (typeToConvert == typeof(SumType<TextEdit, AnnotatedTextEdit>))
            return new SumConverter<SumType<TextEdit, AnnotatedTextEdit>>();
        if (typeToConvert == typeof(SumType<TextEdit, InsertReplaceEdit>))
            return new SumConverter<SumType<TextEdit, InsertReplaceEdit>>();
        if (typeToConvert == typeof(SumType<VSInternalContinueCharacterSingle, VSInternalContinueCharacterRange, VSInternalContinueCharacterClass>))
            return new SumConverter<SumType<VSInternalContinueCharacterSingle, VSInternalContinueCharacterRange, VSInternalContinueCharacterClass>>();
        if (typeToConvert == typeof(SumType<VSInternalReferenceItem, Location>))
            return new SumConverter<SumType<VSInternalReferenceItem, Location>>();
        if (typeToConvert == typeof(SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>))
            return new SumConverter<SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>>();
        if (typeToConvert == typeof(SumType<WorkspaceFolder, DocumentUri>))
            return new SumConverter<SumType<WorkspaceFolder, DocumentUri>>();
        if (typeToConvert == typeof(SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>))
            return new SumConverter<SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>>();
        if (typeToConvert == typeof(SumType<bool, SemanticTokensFullOptions>))
            return new SumConverter<SumType<bool, SemanticTokensFullOptions>>();
        if (typeToConvert == typeof(SumType<bool, int, string>))
            return new SumConverter<SumType<bool, int, string>>();
        if (typeToConvert == typeof(SumType<int, string>))
            return new SumConverter<SumType<int, string>>();
        if (typeToConvert == typeof(SumType<string, bool>))
            return new SumConverter<SumType<string, bool>>();
        if (typeToConvert == typeof(SumType<string, int, bool, string[]>))
            return new SumConverter<SumType<string, int, bool, string[]>>();
        if (typeToConvert == typeof(SumType<string, InlayHintLabelPart[]>))
            return new SumConverter<SumType<string, InlayHintLabelPart[]>>();
        if (typeToConvert == typeof(SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>))
            return new SumConverter<SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>>();
        if (typeToConvert == typeof(SumType<string, MarkedString>))
            return new SumConverter<SumType<string, MarkedString>>();
        if (typeToConvert == typeof(SumType<string, MarkupContent>))
            return new SumConverter<SumType<string, MarkupContent>>();
        if (typeToConvert == typeof(SumType<string, NotebookDocumentFilter>))
            return new SumConverter<SumType<string, NotebookDocumentFilter>>();
        if (typeToConvert == typeof(SumType<string, RelativePattern>))
            return new SumConverter<SumType<string, RelativePattern>>();
        if (typeToConvert == typeof(SumType<string, StringValue>))
            return new SumConverter<SumType<string, StringValue>>();
        if (typeToConvert == typeof(SumType<string, Tuple<int, int>>))
            return new SumConverter<SumType<string, Tuple<int, int>>>();
        if (typeToConvert == typeof(SumType<string[], VSInternalCommitCharacter[]>))
            return new SumConverter<SumType<string[], VSInternalCommitCharacter[]>>();
#pragma warning restore CS0618

        throw new NotSupportedException($"Missing statically registered JSON converter for union type '{typeToConvert}'.");
    }

    internal sealed class SumTypeInfoCache
    {
        // netstandard1.0 doesn't support Array.Empty
#pragma warning disable CA1825 // Avoid zero-length array allocations.
        private static readonly IReadOnlyList<UnionTypeInfo> EmptyUnionInfos = new UnionTypeInfo[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations.

        private readonly IReadOnlyList<UnionTypeInfo> allUnionTypeInfos;

        private readonly IReadOnlyList<UnionTypeInfo> primitiveUnionTypeInfos;

        private readonly IReadOnlyList<UnionTypeInfo> arrayUnionTypeInfos;

        private readonly IReadOnlyList<UnionTypeInfo> objectUnionTypeInfos;

        public SumTypeInfoCache(IReadOnlyList<UnionTypeInfo> unionTypeInfos)
        {
            var allUnionTypeInfosSet = new List<UnionTypeInfo>();
            List<UnionTypeInfo>? primitiveUnionTypeInfosSet = null;
            List<UnionTypeInfo>? arrayUnionTypeInfosSet = null;
            List<UnionTypeInfo>? objectUnionTypeInfosSet = null;

            foreach (var unionTypeInfo in unionTypeInfos)
            {
                var parameterType = unionTypeInfo.Type;
                var parameterTypeInfo = NormalizeToNonNullable(parameterType);
                allUnionTypeInfosSet.Add(unionTypeInfo);

                if (parameterTypeInfo.IsPrimitive ||
                    parameterTypeInfo == typeof(string) ||
                    parameterTypeInfo == typeof(DocumentUri) ||
                    parameterTypeInfo == typeof(Uri) ||
                    typeof(IStringEnum).IsAssignableFrom(parameterTypeInfo))
                {
                    primitiveUnionTypeInfosSet ??= [];
                    primitiveUnionTypeInfosSet.Add(unionTypeInfo);
                }
                else if (parameterTypeInfo.IsArray)
                {
                    arrayUnionTypeInfosSet ??= [];
                    arrayUnionTypeInfosSet.Add(unionTypeInfo);
                }
                else
                {
                    objectUnionTypeInfosSet ??= [];
                    objectUnionTypeInfosSet.Add(unionTypeInfo);
                }
            }

            this.allUnionTypeInfos = allUnionTypeInfosSet;
            this.primitiveUnionTypeInfos = primitiveUnionTypeInfosSet ?? EmptyUnionInfos;
            this.arrayUnionTypeInfos = arrayUnionTypeInfosSet ?? EmptyUnionInfos;
            if ((objectUnionTypeInfosSet?.Count ?? 0) > 1)
            {
                // If some types are tagged with a KindAttribute, make sure they are first in the list in order to avoid the wrong type being deserialized
                this.objectUnionTypeInfos = objectUnionTypeInfosSet.Where(t => t.KindAttribute is not null).Concat(
                                            objectUnionTypeInfosSet.Where(t => t.KindAttribute is null)).ToList();
            }
            else
            {
                this.objectUnionTypeInfos = objectUnionTypeInfosSet ?? EmptyUnionInfos;
            }
        }

        public IReadOnlyList<UnionTypeInfo> GetApplicableInfos(JsonTokenType startingTokenType)
        {
            return startingTokenType switch
            {
                JsonTokenType.StartArray
                    => this.arrayUnionTypeInfos,
                JsonTokenType.Number or
                JsonTokenType.String or
                JsonTokenType.True or
                JsonTokenType.False
                    => this.primitiveUnionTypeInfos,
                JsonTokenType.StartObject
                    => this.objectUnionTypeInfos,
                _ => this.allUnionTypeInfos,
            };
        }

        private static Type NormalizeToNonNullable(Type sumTypeType)
        {
            return Nullable.GetUnderlyingType(sumTypeType) ?? sumTypeType;
        }

        public sealed class UnionTypeInfo
        {
            // System.Text.Json can pre-compile the generic SumType<> constructor call so we don't need to do it through reflection every time.
            internal delegate T StjReader<T>(ref Utf8JsonReader reader, JsonSerializerOptions options);

            public UnionTypeInfo(Type type, Delegate reader)
            {
                this.Type = type ?? throw new ArgumentNullException(nameof(type));
                this.KindAttribute = GetKind(type);
                StjReaderFunction = reader;
            }

            public Type Type { get; }

            public KindAttribute? KindAttribute { get; }

            public Delegate StjReaderFunction { get; }

            internal static KindAttribute? GetKind(Type type)
            {
                if (type == typeof(CreateFile))
                    return new("create");
                if (type == typeof(DeleteFile))
                    return new("delete");
                if (type == typeof(RenameFile))
                    return new("rename");
                if (type == typeof(FullDocumentDiagnosticReport) ||
                    type == typeof(RelatedFullDocumentDiagnosticReport) ||
                    type == typeof(WorkspaceFullDocumentDiagnosticReport))
                {
                    return new(DocumentDiagnosticReportKind.Full);
                }

                if (type == typeof(UnchangedDocumentDiagnosticReport) ||
                    type == typeof(RelatedUnchangedDocumentDiagnosticReport) ||
                    type == typeof(WorkspaceUnchangedDocumentDiagnosticReport))
                {
                    return new(DocumentDiagnosticReportKind.Unchanged);
                }

                if (type == typeof(VSInternalContinueCharacterSingle))
                    return new(VSInternalContinueCharacterSingle.Type, "_vs_type");
                if (type == typeof(VSInternalContinueCharacterRange))
                    return new(VSInternalContinueCharacterRange.Type, "_vs_type");
                if (type == typeof(VSInternalContinueCharacterClass))
                    return new(VSInternalContinueCharacterClass.Type, "_vs_type");

                return null;
            }
        }
    }
}

internal sealed class SumConverter<T> : JsonConverter<T>
    where T : struct, ISumType
{

    /// <inheritdoc/>
    public override T Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            // We shouldn't really ever have a non-Nullable SumType being received as null but, if we do, return an "empty" SumType
            return default;
        }

        var sumTypeInfoCache = SumConverter.SumTypeCache.GetOrAdd(objectType, static _ => default(T).CreateTypeInfo());

        var applicableUnionTypeInfos = sumTypeInfoCache.GetApplicableInfos(reader.TokenType);
        if (applicableUnionTypeInfos.Count > 0)
        {
            var backupReader = reader;
            if (applicableUnionTypeInfos[0].KindAttribute is { } kindAttribute)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                reader = backupReader;
                if (document.RootElement.TryGetProperty(kindAttribute.KindPropertyName, out var value))
                {
                    var kind = value.GetString();
                    for (var i = 0; i < applicableUnionTypeInfos.Count; i++)
                    {
                        var unionTypeInfo = applicableUnionTypeInfos[i];
                        if (unionTypeInfo.KindAttribute == null)
                        {
                            throw new JsonException(LanguageServerProtocolResources.NoSumTypeMatch);
                        }

                        if (unionTypeInfo.KindAttribute.Kind == kind)
                        {
                            var result = ((SumConverter.SumTypeInfoCache.UnionTypeInfo.StjReader<T>)unionTypeInfo.StjReaderFunction).Invoke(ref reader, options);
                            return result;
                        }
                    }
                }
            }

            // Track which arms were skipped by token filtering so pass 2 only retries those.
            using var _ = ArrayBuilder<SumConverter.SumTypeInfoCache.UnionTypeInfo>.GetInstance(out var untestedArms);

            // Pass 1: Try only token-compatible arms (avoids costly exception-based probing)
            for (var i = 0; i < applicableUnionTypeInfos.Count; i++)
            {
                var unionTypeInfo = applicableUnionTypeInfos[i];

                if (unionTypeInfo.KindAttribute != null)
                {
                    continue;
                }

                if (!IsTokenCompatibleWithType(ref reader, unionTypeInfo))
                {
                    untestedArms.Add(unionTypeInfo);
                    continue;
                }

                if (TryDeserializeArm(ref reader, backupReader, unionTypeInfo, options, out var result))
                {
                    return result;
                }
            }

            // Pass 2: If no token-compatible arm succeeded, fall back to trying untested arms.
            // This handles types whose converters accept unexpected token types (e.g.,
            // converters registered at the property level or via JsonSerializerOptions).
            for (var i = 0; i < untestedArms.Count; i++)
            {
                if (TryDeserializeArm(ref reader, backupReader, untestedArms[i], options, out var result))
                {
                    return result;
                }
            }
        }

        throw new JsonException($"No sum type match for {objectType}");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer = writer ?? throw new ArgumentNullException(nameof(writer));

        var sumValue = value.Value;

        // behavior from DocumentUriConverter
        if (sumValue is DocumentUri documentUri)
        {
            writer.WriteStringValue(documentUri.UriString);
            return;
        }
        else if (sumValue is Uri)
        {
            writer.WriteStringValue(sumValue.ToString());
            return;
        }

        if (sumValue != null)
        {
            JsonSerializer.Serialize(writer, sumValue, options);
        }
    }

    private static bool TryDeserializeArm(ref Utf8JsonReader reader, Utf8JsonReader backupReader, SumConverter.SumTypeInfoCache.UnionTypeInfo unionTypeInfo, JsonSerializerOptions options, out T result)
    {
        try
        {
            result = ((SumConverter.SumTypeInfoCache.UnionTypeInfo.StjReader<T>)unionTypeInfo.StjReaderFunction).Invoke(ref reader, options);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            reader = backupReader;
            result = default!;
            return false;
        }
    }

    private static bool IsTokenCompatibleWithType(ref Utf8JsonReader reader, SumConverter.SumTypeInfoCache.UnionTypeInfo unionTypeInfo)
    {
        return IsTokenCompatibleWithType(ref reader, unionTypeInfo.Type);
    }

    private static bool IsTokenCompatibleWithType(ref Utf8JsonReader reader, Type type)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => IsBooleanType(type),
            JsonTokenType.Number => IsNumericType(type),
            JsonTokenType.String => IsStringLikeType(type),
            JsonTokenType.StartObject => !IsSerializedAsJsonPrimitiveType(type) && !type.IsArray,
            JsonTokenType.StartArray => IsArrayElementCompatible(ref reader, type),
            _ => true,
        };
    }

    /// <summary>
    /// Returns true for types that are serialized as JSON primitives (strings, numbers, booleans)
    /// rather than JSON objects. Used to reject incompatible types for StartObject tokens.
    /// </summary>
    private static bool IsSerializedAsJsonPrimitiveType(Type type)
    {
        return IsBooleanType(type) || IsStringLikeType(type) || IsNumericType(type);
    }

    private static bool IsBooleanType(Type type)
    {
        return type == typeof(bool);
    }

    private static bool IsStringLikeType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(Uri) ||
               type == typeof(DocumentUri) ||
               typeof(IStringEnum).IsAssignableFrom(type);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(double) || type == typeof(float) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// For array SumType variants, peeks at the first array element to determine compatibility.
    /// This avoids costly exception-based type probing (e.g., trying to deserialize
    /// VSInternalCommitCharacter[] as string[] which throws InvalidOperationException per attempt).
    /// </summary>
    private static bool IsArrayElementCompatible(ref Utf8JsonReader reader, Type arrayType)
    {
        if (!arrayType.IsArray)
        {
            return false;
        }

        var elementType = arrayType.GetElementType()!;

        // Peek at first array element to disambiguate array types.
        var peekReader = reader;
        if (!peekReader.Read())
        {
            // Can't read into the array, let the normal try/catch handle it.
            return true;
        }

        // Empty array is compatible with any array type.
        if (peekReader.TokenType == JsonTokenType.EndArray)
        {
            return true;
        }

        return IsTokenCompatibleWithType(ref peekReader, elementType);
    }
}
