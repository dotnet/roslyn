// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Utilities to aid work with the LSP Extensions.
/// </summary>
internal static class VSInternalExtensionUtilities
{
    /// <summary>
    /// Adds <see cref="VSExtensionConverter{TBase, TExtension}"/> necessary to deserialize
    /// JSON stream into objects which include VS-specific extensions.
    /// </summary>
    internal static void AddVSInternalExtensionConverters(this JsonSerializerOptions options)
    {
        VSExtensionUtilities.AddVSExtensionConverters(options);
        AddConverters(options.Converters);
    }

    private static void AddConverters(IList<JsonConverter> converters)
    {
        // Reading the number of converters before we start adding new ones
        var existingConvertersCount = converters.Count;

        AddOrReplaceConverter<ClientCapabilities, VSInternalClientCapabilities>();
        AddOrReplaceConverter<CodeAction, VSInternalCodeAction>();
        AddOrReplaceConverter<CodeActionContext, VSInternalCodeActionContext>();
        AddOrReplaceConverter<CodeActionLiteralSetting, VSInternalCodeActionLiteralSetting>();
        AddOrReplaceConverter<CompletionContext, VSInternalCompletionContext>();
        AddOrReplaceConverter<CompletionItem, VSInternalCompletionItem>();
        AddOrReplaceConverter<CompletionList, VSInternalCompletionList>();
        AddOrReplaceConverter<CompletionSetting, VSInternalCompletionSetting>();
        AddOrReplaceConverter<DynamicRegistrationSetting, VSInternalExecuteCommandClientCapabilities>();
        AddOrReplaceConverter<Hover, VSInternalHover>();
        AddOrReplaceConverter<Location, VSInternalLocation>();
        AddOrReplaceConverter<VSProjectContext, VSInternalProjectContext>();
        AddOrReplaceConverter<ServerCapabilities, VSInternalServerCapabilities>();
#pragma warning disable CS0618 // SymbolInformation is obsolete but we need the converter regardless
        AddOrReplaceConverter<SymbolInformation, VSInternalSymbolInformation>();
#pragma warning restore CS0618
        AddOrReplaceConverter<ReferenceParams, VSInternalReferenceParams>();
        AddOrReplaceConverter<SignatureInformation, VSInternalSignatureInformation>();
        AddOrReplaceConverter<TextDocumentClientCapabilities, VSInternalTextDocumentClientCapabilities>();
        AddOrReplaceConverter<RenameRange, VSInternalRenameRange>();
        AddOrReplaceConverter<RenameParams, VSInternalRenameParams>();
        AddOrReplaceConverter<DocumentSymbol, RoslynDocumentSymbol>();
        AddOrReplaceConverter<WorkspaceClientCapabilities, VSInternalWorkspaceClientCapabilities>();

        void AddOrReplaceConverter<TBase, TExtension>()
            where TExtension : TBase
        {
            for (var i = 0; i < existingConvertersCount; i++)
            {
                var existingConverterType = converters[i].GetType();
                if (existingConverterType.IsGenericType &&
                    (existingConverterType.GetGenericTypeDefinition() == typeof(VSExtensionConverter<,>) || existingConverterType.GetGenericTypeDefinition() == typeof(VSExtensionConverter<,>)) &&
                    existingConverterType.GenericTypeArguments[0] == typeof(TBase))
                {
                    converters.RemoveAt(i);
                    existingConvertersCount--;
                    break;
                }
            }

            converters.Add(new VSExtensionConverter<TBase, TExtension>());
        }
    }

    /// <summary>
    /// Returns a string read from the <see cref="Utf8JsonReader"/>. If the string is small enough
    /// to fit into the provided <paramref name="scratchChars"/>, no allocations will be needed.
    /// </summary>
    internal static ReadOnlySpan<char> GetStringSpan(this ref readonly Utf8JsonReader reader, Span<char> scratchChars)
    {
        var valueLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;

        if (valueLength <= scratchChars.Length)
        {
            // If the value fits into the scratch buffer, copy it there, and return a span from that.
            var actualLength = reader.CopyString(scratchChars);

            return scratchChars.Slice(0, actualLength);
        }
        else
        {
            // Otherwise, ask the reader to allocate a string and return a span from that.
            return reader.GetString().AsSpan();
        }
    }
}
