// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

internal abstract partial class AbstractDescriptionBuilder
{
    private readonly IVsObjectBrowserDescription3 _description;
    private readonly AbstractObjectBrowserLibraryManager _libraryManager;
    private readonly ObjectListItem _listItem;
    private readonly Project _project;

    private static readonly SymbolDisplayFormat s_typeDisplay = new(
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

    private Task<Compilation> GetCompilationAsync(CancellationToken cancellationToken)
        => _project.GetCompilationAsync(cancellationToken);

    protected void AddAssemblyLink(IAssemblySymbol assemblySymbol)
    {
        var name = assemblySymbol.Identity.Name;
        var navInfo = _libraryManager.LibraryService.NavInfoFactory.CreateForAssembly(assemblySymbol);

        _description.AddDescriptionText3(name, VSOBDESCRIPTIONSECTION.OBDS_TYPE, navInfo);
    }

    protected void AddComma()
        => _description.AddDescriptionText3(", ", VSOBDESCRIPTIONSECTION.OBDS_COMMA, null);

    protected void AddEndDeclaration()
        => _description.AddDescriptionText3("\n", VSOBDESCRIPTIONSECTION.OBDS_ENDDECL, null);

    protected void AddIndent()
        => _description.AddDescriptionText3("    ", VSOBDESCRIPTIONSECTION.OBDS_MISC, null);

    protected void AddLineBreak()
        => _description.AddDescriptionText3("\n", VSOBDESCRIPTIONSECTION.OBDS_MISC, null);

    protected void AddName(string text)
        => _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_NAME, null);

    protected async Task AddNamespaceLinkAsync(INamespaceSymbol namespaceSymbol, CancellationToken cancellationToken)
    {
        if (namespaceSymbol.IsGlobalNamespace)
        {
            return;
        }

        var text = namespaceSymbol.ToDisplayString();
        var navInfo = _libraryManager.LibraryService.NavInfoFactory.CreateForNamespace(
            namespaceSymbol, _project, await GetCompilationAsync(cancellationToken).ConfigureAwait(true), useExpandedHierarchy: false);

        _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_TYPE, navInfo);
    }

    protected void AddParam(string text)
        => _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_PARAM, null);

    protected void AddText(string text)
        => _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_MISC, null);

    protected async Task AddTypeLinkAsync(
        ITypeSymbol typeSymbol, LinkFlags flags, CancellationToken cancellationToken)
    {
        if (typeSymbol.TypeKind is TypeKind.Unknown or TypeKind.Error or TypeKind.TypeParameter ||
            typeSymbol.SpecialType == SpecialType.System_Void)
        {
            AddName(typeSymbol.ToDisplayString(s_typeDisplay));
            return;
        }

        var useSpecialTypes = (flags & LinkFlags.ExpandPredefinedTypes) == 0;
        var splitLink = !useSpecialTypes & (flags & LinkFlags.SplitNamespaceAndType) != 0;

        if (splitLink && !typeSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            await AddNamespaceLinkAsync(typeSymbol.ContainingNamespace, cancellationToken).ConfigureAwait(true);
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
        var navInfo = _libraryManager.LibraryService.NavInfoFactory.CreateForType(
            typeSymbol, _project, await GetCompilationAsync(cancellationToken).ConfigureAwait(true), useExpandedHierarchy: false);

        _description.AddDescriptionText3(text, VSOBDESCRIPTIONSECTION.OBDS_TYPE, navInfo);
    }

    private void BuildProject(ProjectListItem projectListItem)
    {
        AddText(ServicesVSResources.Project);
        AddName(projectListItem.DisplayText);
    }

    private void BuildReference(ReferenceListItem referenceListItem)
    {
        AddText(ServicesVSResources.Assembly);
        AddName(referenceListItem.DisplayText);
        AddEndDeclaration();
        AddIndent();

        if (referenceListItem.MetadataReference is PortableExecutableReference portableExecutableReference)
        {
            AddText(portableExecutableReference.FilePath);
        }
    }

    private async Task BuildNamespaceAsync(
        NamespaceListItem namespaceListItem, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        var compilation = await GetCompilationAsync(cancellationToken).ConfigureAwait(true);
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
        await BuildMemberOfAsync(namespaceSymbol.ContainingAssembly, cancellationToken).ConfigureAwait(true);
    }

    private async Task BuildTypeAsync(TypeListItem typeListItem, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        var compilation = await GetCompilationAsync(cancellationToken).ConfigureAwait(true);
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
            await BuildDelegateDeclarationAsync(symbol, options, cancellationToken).ConfigureAwait(true);
        }
        else
        {
            await BuildTypeDeclarationAsync(symbol, options, cancellationToken).ConfigureAwait(true);
        }

        AddEndDeclaration();
        await BuildMemberOfAsync(symbol.ContainingNamespace, cancellationToken).ConfigureAwait(true);
        await BuildXmlDocumentationAsync(symbol, compilation, cancellationToken).ConfigureAwait(true);
    }

    private async Task BuildMemberAsync(MemberListItem memberListItem, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        var compilation = await GetCompilationAsync(cancellationToken).ConfigureAwait(true);
        if (compilation == null)
            return;

        var symbol = memberListItem.ResolveTypedSymbol(compilation);
        if (symbol == null)
            return;

        switch (symbol.Kind)
        {
            case SymbolKind.Method:
                await BuildMethodDeclarationAsync((IMethodSymbol)symbol, options, cancellationToken).ConfigureAwait(true);
                break;

            case SymbolKind.Field:
                await BuildFieldDeclarationAsync((IFieldSymbol)symbol, options, cancellationToken).ConfigureAwait(true);
                break;

            case SymbolKind.Property:
                await BuildPropertyDeclarationAsync((IPropertySymbol)symbol, options, cancellationToken).ConfigureAwait(true);
                break;

            case SymbolKind.Event:
                await BuildEventDeclarationAsync((IEventSymbol)symbol, options, cancellationToken).ConfigureAwait(true);
                break;

            default:
                Debug.Fail("Unsupported member kind: " + symbol.Kind.ToString());
                return;
        }

        AddEndDeclaration();
        await BuildMemberOfAsync(symbol.ContainingType, cancellationToken).ConfigureAwait(true);
        await BuildXmlDocumentationAsync(symbol, compilation, cancellationToken).ConfigureAwait(true);
    }

    protected abstract void BuildNamespaceDeclaration(INamespaceSymbol namespaceSymbol, _VSOBJDESCOPTIONS options);
    protected abstract Task BuildDelegateDeclarationAsync(INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken);
    protected abstract Task BuildTypeDeclarationAsync(INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken);
    protected abstract Task BuildMethodDeclarationAsync(IMethodSymbol methodSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken);
    protected abstract Task BuildFieldDeclarationAsync(IFieldSymbol fieldSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken);
    protected abstract Task BuildPropertyDeclarationAsync(IPropertySymbol propertySymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken);
    protected abstract Task BuildEventDeclarationAsync(IEventSymbol eventSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken);

    private async Task BuildMemberOfAsync(ISymbol containingSymbol, CancellationToken cancellationToken)
    {
        if (containingSymbol is INamespaceSymbol &&
            ((INamespaceSymbol)containingSymbol).IsGlobalNamespace)
        {
            containingSymbol = containingSymbol.ContainingAssembly;
        }

        var memberOfText = ServicesVSResources.Member_of_0;
        const string specifier = "{0}";

        var index = memberOfText.IndexOf(specifier, StringComparison.Ordinal);
        if (index < 0)
        {
            Debug.Fail("MemberOf string resource is incorrect.");
            return;
        }

        var left = memberOfText[..index];
        var right = memberOfText[(index + specifier.Length)..];

        AddIndent();
        AddText(left);

        if (containingSymbol is IAssemblySymbol assemblySymbol)
        {
            AddAssemblyLink(assemblySymbol);
        }
        else if (containingSymbol is ITypeSymbol typeSymbol)
        {
            await AddTypeLinkAsync(
                typeSymbol, LinkFlags.SplitNamespaceAndType | LinkFlags.ExpandPredefinedTypes, cancellationToken).ConfigureAwait(true);
        }
        else if (containingSymbol is INamespaceSymbol namespaceSymbol)
        {
            await AddNamespaceLinkAsync(namespaceSymbol, cancellationToken).ConfigureAwait(true);
        }

        AddText(right);
        AddEndDeclaration();
    }

    private async Task BuildXmlDocumentationAsync(
        ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
    {
        var documentationComment = symbol.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: CancellationToken.None);
        if (documentationComment == null)
        {
            return;
        }

        var formattingService = _project.Services.GetService<IDocumentationCommentFormattingService>();
        if (formattingService == null)
        {
            return;
        }

        var emittedDocs = false;

        if (documentationComment.SummaryText != null)
        {
            AddLineBreak();
            AddName(FeaturesResources.Summary_colon);
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
            AddName(ServicesVSResources.Type_Parameters_colon);

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
            AddName(FeaturesResources.Parameters_colon);

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
            AddName(FeaturesResources.Returns_colon);
            AddLineBreak();

            AddText(formattingService.Format(documentationComment.ReturnsText, compilation));
            emittedDocs = true;
        }

        if (ShowValueDocumentation(symbol) && documentationComment.ValueText != null)
        {
            if (emittedDocs)
            {
                AddLineBreak();
            }

            AddLineBreak();
            AddName(FeaturesResources.Value_colon);
            AddLineBreak();

            AddText(formattingService.Format(documentationComment.ValueText, compilation));
            emittedDocs = true;
        }

        if (documentationComment.RemarksText != null)
        {
            if (emittedDocs)
            {
                AddLineBreak();
            }

            AddLineBreak();
            AddName(FeaturesResources.Remarks_colon);
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
            AddName(WorkspacesResources.Exceptions_colon);

            foreach (var exceptionType in documentationComment.ExceptionTypes)
            {
                if (DocumentationCommentId.GetFirstSymbolForDeclarationId(exceptionType, compilation) is INamedTypeSymbol exceptionTypeSymbol)
                {
                    AddLineBreak();

                    var exceptionTexts = documentationComment.GetExceptionTexts(exceptionType);
                    if (exceptionTexts.Length == 0)
                    {
                        await AddTypeLinkAsync(exceptionTypeSymbol, LinkFlags.None, cancellationToken).ConfigureAwait(true);
                    }
                    else
                    {
                        foreach (var exceptionText in exceptionTexts)
                        {
                            await AddTypeLinkAsync(exceptionTypeSymbol, LinkFlags.None, cancellationToken).ConfigureAwait(true);
                            AddText(": ");
                            AddText(formattingService.Format(exceptionText, compilation));
                        }
                    }
                }
            }
        }
    }

    private static bool ShowReturnsDocumentation(ISymbol symbol)
    {
        return symbol is INamedTypeSymbol { TypeKind: TypeKind.Delegate }
            || symbol.Kind is SymbolKind.Method or SymbolKind.Property;
    }

    private static bool ShowValueDocumentation(ISymbol symbol)
    {
        // <returns> is often used in places where <value> was originally intended. Allow either to be used in
        // documentation comments since they are not likely to be used together and it's not clear which one a
        // particular code base will be using more often.
        return ShowReturnsDocumentation(symbol);
    }

    internal async Task<bool> TryBuildAsync(_VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        switch (_listItem)
        {
            case ProjectListItem projectListItem:
                BuildProject(projectListItem);
                return true;
            case ReferenceListItem referenceListItem:
                BuildReference(referenceListItem);
                return true;
            case NamespaceListItem namespaceListItem:
                await BuildNamespaceAsync(namespaceListItem, options, cancellationToken).ConfigureAwait(true);
                return true;
            case TypeListItem typeListItem:
                await BuildTypeAsync(typeListItem, options, cancellationToken).ConfigureAwait(true);
                return true;
            case MemberListItem memberListItem:
                await BuildMemberAsync(memberListItem, options, cancellationToken).ConfigureAwait(true);
                return true;
        }

        return false;
    }
}
