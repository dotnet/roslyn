// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Utilities to aid work with VS Code LSP Extensions.
    /// </summary>
    internal static class VSCodeInternalExtensionUtilities
    {
        /// <summary>
        /// Adds <see cref="VSExtensionConverter{TBase, TExtension}"/> necessary to deserialize
        /// JSON stream into objects which include VS Code-specific extensions.
        /// </summary>
        public static void AddVSCodeInternalExtensionConverters(this JsonSerializerOptions options)
        {
            // Reading the number of converters before we start adding new ones
            var existingConvertersCount = options.Converters.Count;

            AddOrReplaceConverter<TextDocumentRegistrationOptions, VSInternalTextDocumentRegistrationOptions>(options.Converters);
            AddOrReplaceConverter<TextDocumentClientCapabilities, VSInternalTextDocumentClientCapabilities>(options.Converters);

            void AddOrReplaceConverter<TBase, TExtension>(IList<JsonConverter> converters)
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
    }
}
