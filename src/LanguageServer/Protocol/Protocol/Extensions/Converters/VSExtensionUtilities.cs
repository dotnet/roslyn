// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Utility functions to simplify working with the Visual Studio extensions to the Language Server Protocol.
/// </summary>
internal static class VSExtensionUtilities
{
    /// <summary>
    /// Adds <see cref="VSExtensionConverter{TBase, TExtension}"/> to the <paramref name="options"/> allowing to
    /// deserialize the JSON stream into objects which include Visual Studio specific extensions.
    ///
    /// For example, it allows to correctly deserialize the <see cref="CodeAction.Diagnostics"/> entries of a
    /// 'codeAction/resolve' request into <see cref="VSDiagnostic"/> objects even if <see cref="CodeAction.Diagnostics"/>
    /// is defined as an array of <see cref="Diagnostic"/>.
    /// </summary>
    internal static void AddVSExtensionConverters(this JsonSerializerOptions options)
    {
        AddConverters(options.Converters);
    }

    private static void AddConverters(IList<JsonConverter> converters)
    {
        // Reading the number of converters before we start adding new ones
        var existingConvertersCount = converters.Count;

        TryAddConverter<Diagnostic, VSDiagnostic>();
        TryAddConverter<Location, VSLocation>();
        TryAddConverter<ServerCapabilities, VSServerCapabilities>();
#pragma warning disable CS0618 // SymbolInformation is obsolete but we need the converter regardless
        TryAddConverter<SymbolInformation, VSSymbolInformation>();
#pragma warning restore CS0618
        TryAddConverter<TextDocumentIdentifier, VSTextDocumentIdentifier>();

        void TryAddConverter<TBase, TExtension>()
            where TExtension : TBase
        {
            for (var i = 0; i < existingConvertersCount; i++)
            {
                var existingConverterType = converters[i].GetType();
                if (existingConverterType.IsGenericType &&
                    (existingConverterType.GetGenericTypeDefinition() == typeof(VSExtensionConverter<,>) || existingConverterType.GetGenericTypeDefinition() == typeof(VSExtensionConverter<,>)) &&
                    existingConverterType.GenericTypeArguments[0] == typeof(TBase))
                {
                    return;
                }
            }

            converters.Add(new VSExtensionConverter<TBase, TExtension>());
        }
    }
}
