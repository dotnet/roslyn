// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using Newtonsoft.Json;

    /// <summary>
    /// Utilities to aid work with the LSP Extensions.
    /// </summary>
    internal static class VSInternalExtensionUtilities
    {
        /// <summary>
        /// Adds <see cref="VSExtensionConverter{TBase, TExtension}"/> necessary to deserialize
        /// JSON stream into objects which include VS-specific extensions.
        /// </summary>
        /// <remarks>
        /// If <paramref name="serializer"/> is used in parallel to execution of this method,
        /// its access needs to be synchronized with this method call, to guarantee that
        /// <see cref="JsonSerializer.Converters"/> collection is not modified when <paramref name="serializer"/> in use.
        /// </remarks>
        /// <param name="serializer">Instance of <see cref="JsonSerializer"/> which is guaranteed to not work in parallel to this method call.</param>
        public static void AddVSInternalExtensionConverters(this JsonSerializer serializer)
        {
            VSExtensionUtilities.AddVSExtensionConverters(serializer);

            // Reading the number of converters before we start adding new ones
            var existingConvertersCount = serializer.Converters.Count;

            AddOrReplaceConverter<TextDocumentRegistrationOptions, VSInternalTextDocumentRegistrationOptions>();
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
            AddOrReplaceConverter<SymbolInformation, VSInternalSymbolInformation>();
            AddOrReplaceConverter<ReferenceParams, VSInternalReferenceParams>();
            AddOrReplaceConverter<SignatureInformation, VSInternalSignatureInformation>();
            AddOrReplaceConverter<TextDocumentClientCapabilities, VSInternalTextDocumentClientCapabilities>();
            AddOrReplaceConverter<TextDocumentClientCapabilities, VSInternalTextDocumentClientCapabilities>();
            AddOrReplaceConverter<RenameRange, VSInternalRenameRange>();
            AddOrReplaceConverter<RenameParams, VSInternalRenameParams>();

            void AddOrReplaceConverter<TBase, TExtension>()
                where TExtension : TBase
            {
                for (var i = 0; i < existingConvertersCount; i++)
                {
                    var existingConverterType = serializer.Converters[i].GetType();
                    if (existingConverterType.IsGenericType &&
                        existingConverterType.GetGenericTypeDefinition() == typeof(VSExtensionConverter<,>) &&
                        existingConverterType.GenericTypeArguments[0] == typeof(TBase))
                    {
                        serializer.Converters.RemoveAt(i);
                        existingConvertersCount--;
                        break;
                    }
                }

                serializer.Converters.Add(new VSExtensionConverter<TBase, TExtension>());
            }
        }
    }
}
