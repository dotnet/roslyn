// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

/// <summary>
/// These are very temporary types that we need in order to serialize document symbol data
/// using Newtonsoft instead of System.Text.Json
///
/// We currently must support Newtonsoft serialization here because we have not yet opted into using STJ
/// in the VS language server client (and so the client will serialize the request using Newtonsoft).
/// 
/// https://github.com/dotnet/roslyn/pull/72675 tracks opting in the client to STJ.
/// TODO - everything in this type should be deleted once the client side is using STJ.
/// </summary>
internal class DocumentSymbolNewtonsoft
{
    private class NewtonsoftDocumentUriConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            reader = reader ?? throw new ArgumentNullException(nameof(reader));
            if (reader.TokenType == JsonToken.String)
            {
                var token = JToken.ReadFrom(reader);
                var uri = new Uri(token.ToObject<string>());

                return uri;
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.DocumentUriSerializationError, reader.Value));
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer = writer ?? throw new ArgumentNullException(nameof(writer));

            if (value is Uri uri)
            {
                var token = JToken.FromObject(uri.AbsoluteUri);
                token.WriteTo(writer);
            }
            else
            {
                throw new ArgumentException($"{nameof(value)} must be of type {nameof(Uri)}");
            }
        }
    }

    [DataContract]
    internal record NewtonsoftTextDocumentIdentifier([property: DataMember(Name = "uri"), JsonConverter(typeof(NewtonsoftDocumentUriConverter))] Uri Uri);

    [DataContract]
    internal record NewtonsoftRoslynDocumentSymbolParams(
        [property: DataMember(Name = "textDocument")] NewtonsoftTextDocumentIdentifier TextDocument,
        [property: DataMember(Name = "useHierarchicalSymbols"), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] bool UseHierarchicalSymbols);

    [DataContract]
    internal record NewtonsoftRoslynDocumentSymbol(
        [property: DataMember(IsRequired = true, Name = "name")] string Name,
        [property: DataMember(Name = "detail")][property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] string? Detail,
        [property: DataMember(Name = "kind")] NewtonsoftSymbolKind Kind,
        [property: DataMember(Name = "deprecated")][property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] bool Deprecated,
        [property: DataMember(IsRequired = true, Name = "range")] NewtonsoftRange Range,
        [property: DataMember(IsRequired = true, Name = "selectionRange")] NewtonsoftRange SelectionRange,
        [property: DataMember(Name = "children")][property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] NewtonsoftRoslynDocumentSymbol[]? Children,
        [property: DataMember(Name = "glyph")] int Glyph);

    [DataContract]
    internal record NewtonsoftRange(
        [property: DataMember(Name = "start"), JsonProperty(Required = Required.Always)] NewtonsoftPosition Start,
        [property: DataMember(Name = "end"), JsonProperty(Required = Required.Always)] NewtonsoftPosition End);

    [DataContract]
    internal record NewtonsoftPosition([property: DataMember(Name = "line")] int Line, [property: DataMember(Name = "character")] int Character);

    [DataContract]
    internal enum NewtonsoftSymbolKind
    {
        File = 1,
        Module = 2,
        Namespace = 3,
        Package = 4,
        Class = 5,
        Method = 6,
        Property = 7,
        Field = 8,
        Constructor = 9,
        Enum = 10,
        Interface = 11,
        Function = 12,
        Variable = 13,
        Constant = 14,
        String = 15,
        Number = 16,
        Boolean = 17,
        Array = 18,
        Object = 19,
        Key = 20,
        Null = 21,
        EnumMember = 22,
        Struct = 23,
        Event = 24,
        Operator = 25,
        TypeParameter = 26,
    }
}
