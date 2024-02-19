// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using Newtonsoft.Json;

    /// <summary>
    /// Utility functions to simplify working with the Visual Studio extensions to the Language Server Protocol.
    /// </summary>
    internal static class VSExtensionUtilities
    {
        /// <summary>
        /// Adds <see cref="VSExtensionConverter{TBase, TExtension}"/> to the <paramref name="serializer"/> allowing to
        /// deserialize the JSON stream into objects which include Visual Studio specific extensions.
        ///
        /// For example, it allows to correctly deserialize the <see cref="CodeAction.Diagnostics"/> entries of a
        /// 'codeAction/resolve' request into <see cref="VSDiagnostic"/> objects even if <see cref="CodeAction.Diagnostics"/>
        /// is defined as an array of <see cref="Diagnostic"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="serializer"/> is used in parallel to the execution of this method,
        /// its access needs to be synchronized with this method call, to guarantee that the
        /// <see cref="JsonSerializer.Converters"/> collection is not modified when <paramref name="serializer"/> is in use.
        /// </remarks>
        /// <param name="serializer">Instance of <see cref="JsonSerializer"/> to be configured.</param>
        public static void AddVSExtensionConverters(this JsonSerializer serializer)
        {
            // Reading the number of converters before we start adding new ones
            var existingConvertersCount = serializer.Converters.Count;

            TryAddConverter<Diagnostic, VSDiagnostic>();
            TryAddConverter<Location, VSLocation>();
            TryAddConverter<ServerCapabilities, VSServerCapabilities>();
            TryAddConverter<SymbolInformation, VSSymbolInformation>();
            TryAddConverter<TextDocumentIdentifier, VSTextDocumentIdentifier>();

            void TryAddConverter<TBase, TExtension>()
                where TExtension : TBase
            {
                for (var i = 0; i < existingConvertersCount; i++)
                {
                    var existingConverterType = serializer.Converters[i].GetType();
                    if (existingConverterType.IsGenericType &&
                        existingConverterType.GetGenericTypeDefinition() == typeof(VSExtensionConverter<,>) &&
                        existingConverterType.GenericTypeArguments[0] == typeof(TBase))
                    {
                        return;
                    }
                }

                serializer.Converters.Add(new VSExtensionConverter<TBase, TExtension>());
            }
        }
    }
}
