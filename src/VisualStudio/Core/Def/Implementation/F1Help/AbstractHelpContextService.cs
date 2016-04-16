// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.F1Help
{
    internal abstract class AbstractHelpContextService : IHelpContextService
    {
        // MSDN Format: type parameters to types expressed as `1
        // type parameters to methods expressed as ``1
        // C`1.M``2
        // constructors: Parent.Type.#ctor
        protected static readonly SymbolDisplayFormat TypeFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.None,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        protected static readonly SymbolDisplayFormat SpecialTypeFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected static readonly SymbolDisplayFormat NameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public abstract string Language { get; }
        public abstract string Product { get; }

        public abstract Task<string> GetHelpTermAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

        public abstract string FormatSymbol(ISymbol symbol);
    }
}
