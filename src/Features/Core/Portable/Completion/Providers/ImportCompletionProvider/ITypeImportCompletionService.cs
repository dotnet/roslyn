// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal interface ITypeImportCompletionService : ILanguageService
    {
        /// <summary>
        /// Get all the top level types from given project. This method is intended to be used for 
        /// getting types from source only, so the project must support compilation. 
        /// For getting types from PE, use <see cref="GetTopLevelTypesFromPEReference"/>.
        /// </summary>
        Task GetTopLevelTypesAsync(
            Project project,
            SyntaxContext syntaxContext,
            Action<TypeImportCompletionItemInfo> handleItem,
            CancellationToken cancellationToken);

        void GetTopLevelTypesFromPEReference(
            Solution solution,
            Compilation compilation,
            PortableExecutableReference peReference,
            SyntaxContext syntaxContext,
            Action<TypeImportCompletionItemInfo> handleItem,
            CancellationToken cancellationToken);
    }

    internal readonly struct TypeImportCompletionItemInfo
    {
        public TypeImportCompletionItemInfo(CompletionItem item, bool isPublic)
        {
            Item = item;
            IsPublic = isPublic;
        }

        public CompletionItem Item { get; }

        public bool IsPublic { get; }

        public TypeImportCompletionItemInfo WithItem(CompletionItem item)
        {
            return new TypeImportCompletionItemInfo(item, IsPublic);
        }
    }
}
