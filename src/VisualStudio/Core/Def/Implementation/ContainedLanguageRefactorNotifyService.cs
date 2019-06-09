// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IRefactorNotifyService))]
    internal sealed class ContainedLanguageRefactorNotifyService : IRefactorNotifyService
    {
        private static readonly SymbolDisplayFormat s_qualifiedDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        [ImportingConstructor]
        public ContainedLanguageRefactorNotifyService()
        {
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            return true;
        }

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            if (workspace is VisualStudioWorkspaceImpl visualStudioWorkspace)
            {
                foreach (var documentId in changedDocumentIDs)
                {
                    var containedDocument = visualStudioWorkspace.TryGetContainedDocument(documentId);
                    if (containedDocument != null)
                    {
                        var containedLanguageHost = containedDocument.ContainedLanguageHost;
                        if (containedLanguageHost != null)
                        {
                            var hresult = containedLanguageHost.OnRenamed(
                                GetRenameType(symbol), symbol.ToDisplayString(s_qualifiedDisplayFormat), newName);
                            if (hresult < 0)
                            {
                                if (throwOnFailure)
                                {
                                    Marshal.ThrowExceptionForHR(hresult);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private ContainedLanguageRenameType GetRenameType(ISymbol symbol)
        {
            if (symbol is INamespaceSymbol)
            {
                return ContainedLanguageRenameType.CLRT_NAMESPACE;
            }
            else if (symbol is INamedTypeSymbol && (symbol as INamedTypeSymbol).TypeKind == TypeKind.Class)
            {
                return ContainedLanguageRenameType.CLRT_CLASS;
            }
            else if (symbol.Kind == SymbolKind.Event ||
                symbol.Kind == SymbolKind.Field ||
                symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Property)
            {
                return ContainedLanguageRenameType.CLRT_CLASSMEMBER;
            }
            else
            {
                return ContainedLanguageRenameType.CLRT_OTHER;
            }
        }
    }
}
