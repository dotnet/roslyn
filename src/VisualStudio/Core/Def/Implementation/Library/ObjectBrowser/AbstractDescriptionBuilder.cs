// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractDescriptionBuilder
    {
        private readonly IVsObjectBrowserDescription3 _description;
        private readonly AbstractObjectBrowserLibraryManager _libraryManager;
        private readonly ObjectListItem _listItem;
        private readonly Project _project;

        private static readonly SymbolDisplayFormat s_typeDisplay = new SymbolDisplayFormat(
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected AbstractDescriptionBuilder(
            IVsObjectBrowserDescription3 description,
            AbstractObjectBrowserLibraryManager libraryManager,
            ObjectListItem listItem,
            Project project)
        {
            _description = description;
            _libraryManager = libraryManager;
            _listItem = listItem;
            _project = project;
        }

        private Compilation GetCompilation()
        {
            return _project
                .GetCompilationAsync(CancellationToken.None)
                .WaitAndGetResult_ObjectBrowser(CancellationToken.None);
        }

        protected void AddAssemblyLink(IAssemblySymbol assemblySymbol)
        {
            var name = assemblySymbol.Identity.Name;
            var navInfo = _libraryManager.LibraryService.NavInfoFactory.CreateForAssembly(assemblySymbol);

            _description.AddDescriptionText3(name, VSOBDESCRIPTIONSECTION.OBDS_TYPE, navInfo);
        }

        protected void AddComma()
        {
            _description.AddDescriptionText3(", ", VSOBDESCRIPTIONSECTION.OBDS_COMMA, null);
        }

        protected void AddEndDeclaration()
        {
            _description.AddDescriptionText3("\n", VSOBDESCRIPTIONSECTION.OBDS_ENDDECL, null);
        }

        protected void AddIndent()
        {
            _description.AddDescriptionText3("    ", VSOBDESCRIPTIONSECTION.OBDS_MISC, null);
        }

        protected void AddLineBreak()
        {
            _description.AddDescriptionText3("\n", VSOBDESCRIPTIONSECTION.OBDS_MISC, null);
        }

        protected void AddName(string text)
        {
            _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_NAME, null);
        }

        protected void AddNamespaceLink(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol.IsGlobalNamespace)
            {
                return;
            }

            var text = namespaceSymbol.ToDisplayString();
            var navInfo = _libraryManager.LibraryService.NavInfoFactory.CreateForNamespace(namespaceSymbol, _project, GetCompilation(), useExpandedHierarchy: false);

            _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_TYPE, navInfo);
        }

        protected void AddParam(string text)
        {
            _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_PARAM, null);
        }

        protected void AddText(string text)
        {
            _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_MISC, null);
        }

        protected void AddTypeLink(ITypeSymbol typeSymbol, LinkFlags flags)
        {
            if (typeSymbol.TypeKind == TypeKind.Unknown ||
                typeSymbol.TypeKind == TypeKind.Error ||
                typeSymbol.TypeKind == TypeKind.TypeParameter ||
                typeSymbol.SpecialType == SpecialType.System_Void)
            {
                AddName(typeSymbol.ToDisplayString(s_typeDisplay));
                return;
            }

            var useSpecialTypes = (flags & LinkFlags.ExpandPredefinedTypes) == 0;
            var splitLink = !useSpecialTypes & (flags & LinkFlags.SplitNamespaceAndType) != 0;

            if (splitLink && !typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                AddNamespaceLink(typeSymbol.ContainingNamespace);
                AddText(".");
            }

            var typeQualificationStyle = splitLink
                ? SymbolDisplayTypeQualificationStyle.NameAndContainingTypes
                : SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces;

            var miscellaneousOptions = useSpecialTypes
                ? SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                : SymbolDisplayMiscellaneousOptions.ExpandNullable;

            var typeDisplayFormat = new SymbolDisplayFormat(
                typeQualificationStyle: typeQualificationStyle,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                miscellaneousOptions: miscellaneousOptions);

            var text = typeSymbol.ToDisplayString(typeDisplayFormat);
            var navInfo = _libraryManager.LibraryService.NavInfoFactory.CreateForType(typeSymbol, _project, GetCompilation(), useExpandedHierarchy: false);

            _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_TYPE, navInfo);
        }

        private void BuildProject(ProjectListItem projectListItem)
        {
            AddText(ServicesVSResources.Library_Project);
            AddName(projectListItem.DisplayText);
        }

        private void BuildReference(ReferenceListItem referenceListItem)
        {
            AddText(ServicesVSResources.Library_Assembly);
            AddName(referenceListItem.DisplayText);
            AddEndDeclaration();
            AddIndent();

            var portableExecutableReference = referenceListItem.MetadataReference as PortableExecutableReference;
            if (portableExecutableReference != null)
            {
                AddText(portableExecutableReference.FilePath);
            }
        }

        private void BuildNamespace(NamespaceListItem namespaceListItem, _VSOBJDESCOPTIONS options)
        {
            var compilation = GetCompilation();
            if (compilation == null)
            {
                return;
            }

            var namespaceSymbol = namespaceListItem.ResolveTypedSymbol(compilation);
            if (namespaceSymbol == null)
            {
                return;
            }

            BuildNamespaceDeclaration(namespaceSymbol, options);

            AddEndDeclaration();
            BuildMemberOf(namespaceSymbol.ContainingAssembly, options);
        }

        private void BuildType(TypeListItem typeListItem, _VSOBJDESCOPTIONS options)
        {
            var compilation = GetCompilation();
            if (compilation == null)
            {
                return;
            }

            var symbol = typeListItem.ResolveTypedSymbol(compilation);
            if (symbol == null)
            {
                return;
            }

            if (symbol.TypeKind == TypeKind.Delegate)
            {
                BuildDelegateDeclaration(symbol, options);
            }
            else
            {
                BuildTypeDeclaration(symbol, options);
            }

            AddEndDeclaration();
            BuildMemberOf(symbol.ContainingNamespace, options);

            BuildXmlDocumentation(symbol, compilation, options);
        }

        private void BuildMember(MemberListItem memberListItem, _VSOBJDESCOPTIONS options)
        {
            var compilation = GetCompilation();
            if (compilation == null)
            {
                return;
            }

            var symbol = memberListItem.ResolveTypedSymbol(compilation);
            if (symbol == null)
            {
                return;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    BuildMethodDeclaration((IMethodSymbol)symbol, options);
                    break;

                case SymbolKind.Field:
                    BuildFieldDeclaration((IFieldSymbol)symbol, options);
                    break;

                case SymbolKind.Property:
                    BuildPropertyDeclaration((IPropertySymbol)symbol, options);
                    break;

                case SymbolKind.Event:
                    BuildEventDeclaration((IEventSymbol)symbol, options);
                    break;

                default:
                    Debug.Fail("Unsupported member kind: " + symbol.Kind.ToString());
                    return;
            }

            AddEndDeclaration();
            BuildMemberOf(symbol.ContainingType, options);

            BuildXmlDocumentation(symbol, compilation, options);
        }

        protected abstract void BuildNamespaceDeclaration(INamespaceSymbol namespaceSymbol, _VSOBJDESCOPTIONS options);
        protected abstract void BuildDelegateDeclaration(INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options);
        protected abstract void BuildTypeDeclaration(INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options);
        protected abstract void BuildMethodDeclaration(IMethodSymbol methodSymbol, _VSOBJDESCOPTIONS options);
        protected abstract void BuildFieldDeclaration(IFieldSymbol fieldSymbol, _VSOBJDESCOPTIONS options);
        protected abstract void BuildPropertyDeclaration(IPropertySymbol propertySymbol, _VSOBJDESCOPTIONS options);
        protected abstract void BuildEventDeclaration(IEventSymbol eventSymbol, _VSOBJDESCOPTIONS options);

        private void BuildMemberOf(ISymbol containingSymbol, _VSOBJDESCOPTIONS options)
        {
            if (containingSymbol is INamespaceSymbol &&
                ((INamespaceSymbol)containingSymbol).IsGlobalNamespace)
            {
                containingSymbol = containingSymbol.ContainingAssembly;
            }

            var memberOfText = ServicesVSResources.Library_MemberOf;
            const string specifier = "{0}";

            var index = memberOfText.IndexOf(specifier, StringComparison.Ordinal);
            if (index < 0)
            {
                Debug.Fail("MemberOf string resource is incorrect.");
                return;
            }

            var left = memberOfText.Substring(0, index);
            var right = memberOfText.Substring(index + specifier.Length);

            AddIndent();
            AddText(left);

            if (containingSymbol is IAssemblySymbol)
            {
                AddAssemblyLink((IAssemblySymbol)containingSymbol);
            }
            else if (containingSymbol is ITypeSymbol)
            {
                AddTypeLink((ITypeSymbol)containingSymbol, LinkFlags.SplitNamespaceAndType | LinkFlags.ExpandPredefinedTypes);
            }
            else if (containingSymbol is INamespaceSymbol)
            {
                AddNamespaceLink((INamespaceSymbol)containingSymbol);
            }

            AddText(right);
            AddEndDeclaration();
        }

        private void BuildXmlDocumentation(ISymbol symbol, Compilation compilation, _VSOBJDESCOPTIONS options)
        {
            var documentationComment = symbol.GetDocumentationComment(expandIncludes: true, cancellationToken: CancellationToken.None);
            if (documentationComment == null)
            {
                return;
            }

            var formattingService = _project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
            if (formattingService == null)
            {
                return;
            }

            var emittedDocs = false;

            if (documentationComment.SummaryText != null)
            {
                AddLineBreak();
                AddName(ServicesVSResources.Library_Summary);
                AddLineBreak();

                AddText(formattingService.Format(documentationComment.SummaryText, compilation));
                emittedDocs = true;
            }

            if (documentationComment.TypeParameterNames.Length > 0)
            {
                if (emittedDocs)
                {
                    AddLineBreak();
                }

                AddLineBreak();
                AddName(ServicesVSResources.Library_TypeParameters);

                foreach (var typeParameterName in documentationComment.TypeParameterNames)
                {
                    AddLineBreak();

                    var typeParameterText = documentationComment.GetTypeParameterText(typeParameterName);
                    if (typeParameterText != null)
                    {
                        AddParam(typeParameterName);
                        AddText(": ");

                        AddText(formattingService.Format(typeParameterText, compilation));
                        emittedDocs = true;
                    }
                }
            }

            if (documentationComment.ParameterNames.Length > 0)
            {
                if (emittedDocs)
                {
                    AddLineBreak();
                }

                AddLineBreak();
                AddName(ServicesVSResources.Library_Parameters);

                foreach (var parameterName in documentationComment.ParameterNames)
                {
                    AddLineBreak();

                    var parameterText = documentationComment.GetParameterText(parameterName);
                    if (parameterText != null)
                    {
                        AddParam(parameterName);
                        AddText(": ");

                        AddText(formattingService.Format(parameterText, compilation));
                        emittedDocs = true;
                    }
                }
            }

            if (ShowReturnsDocumentation(symbol) && documentationComment.ReturnsText != null)
            {
                if (emittedDocs)
                {
                    AddLineBreak();
                }

                AddLineBreak();
                AddName(ServicesVSResources.Library_Returns);
                AddLineBreak();

                AddText(formattingService.Format(documentationComment.ReturnsText, compilation));
                emittedDocs = true;
            }

            if (documentationComment.RemarksText != null)
            {
                if (emittedDocs)
                {
                    AddLineBreak();
                }

                AddLineBreak();
                AddName(ServicesVSResources.Library_Remarks);
                AddLineBreak();

                AddText(formattingService.Format(documentationComment.RemarksText, compilation));
                emittedDocs = true;
            }

            if (documentationComment.ExceptionTypes.Length > 0)
            {
                if (emittedDocs)
                {
                    AddLineBreak();
                }

                AddLineBreak();
                AddName(ServicesVSResources.Library_Exceptions);

                foreach (var exceptionType in documentationComment.ExceptionTypes)
                {
                    var exceptionTypeSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(exceptionType, compilation) as INamedTypeSymbol;
                    if (exceptionTypeSymbol != null)
                    {
                        AddLineBreak();

                        var exceptionTexts = documentationComment.GetExceptionTexts(exceptionType);
                        if (exceptionTexts.Length == 0)
                        {
                            AddTypeLink(exceptionTypeSymbol, LinkFlags.None);
                        }
                        else
                        {
                            foreach (var exceptionText in exceptionTexts)
                            {
                                AddTypeLink(exceptionTypeSymbol, LinkFlags.None);
                                AddText(": ");
                                AddText(formattingService.Format(exceptionText, compilation));
                            }
                        }

                        emittedDocs = true;
                    }
                }
            }
        }

        private bool ShowReturnsDocumentation(ISymbol symbol)
        {
            return (symbol.Kind == SymbolKind.NamedType && ((INamedTypeSymbol)symbol).TypeKind == TypeKind.Delegate)
                || symbol.Kind == SymbolKind.Method
                || symbol.Kind == SymbolKind.Property;
        }

        internal bool TryBuild(_VSOBJDESCOPTIONS options)
        {
            var projectListItem = _listItem as ProjectListItem;
            if (projectListItem != null)
            {
                BuildProject(projectListItem);
                return true;
            }

            var referenceListItem = _listItem as ReferenceListItem;
            if (referenceListItem != null)
            {
                BuildReference(referenceListItem);
                return true;
            }

            var namespaceListItem = _listItem as NamespaceListItem;
            if (namespaceListItem != null)
            {
                BuildNamespace(namespaceListItem, options);
                return true;
            }

            var typeListItem = _listItem as TypeListItem;
            if (typeListItem != null)
            {
                BuildType(typeListItem, options);
                return true;
            }

            var memberListItem = _listItem as MemberListItem;
            if (memberListItem != null)
            {
                BuildMember(memberListItem, options);
                return true;
            }

            return false;
        }
    }
}
