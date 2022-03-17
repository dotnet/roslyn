// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IRefactorNotifyService))]
    internal sealed class ContainedLanguageRefactorNotifyService : IRefactorNotifyService
    {
        private static readonly SymbolDisplayFormat s_qualifiedDisplayFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ContainedLanguageRefactorNotifyService()
        {
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
            => true;

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

        private static ContainedLanguageRenameType GetRenameType(ISymbol symbol)
        {
            if (symbol is INamespaceSymbol)
            {
                return ContainedLanguageRenameType.CLRT_NAMESPACE;
            }
            else if (symbol is INamedTypeSymbol && (symbol as INamedTypeSymbol).TypeKind == TypeKind.Class)
            {
                return ContainedLanguageRenameType.CLRT_CLASS;
            }
            else if (symbol.Kind is SymbolKind.Event or
                SymbolKind.Field or
                SymbolKind.Method or
                SymbolKind.Property)
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
