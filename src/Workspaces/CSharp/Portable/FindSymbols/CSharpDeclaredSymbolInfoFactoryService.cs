// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FindSymbols;

[ExportLanguageService(typeof(IDeclaredSymbolInfoFactoryService), LanguageNames.CSharp), Shared]
internal class CSharpDeclaredSymbolInfoFactoryService : AbstractDeclaredSymbolInfoFactoryService<
    CompilationUnitSyntax,
    UsingDirectiveSyntax,
    BaseNamespaceDeclarationSyntax,
    TypeDeclarationSyntax,
    EnumDeclarationSyntax,
    MethodDeclarationSyntax,
    MemberDeclarationSyntax,
    NameSyntax,
    QualifiedNameSyntax,
    IdentifierNameSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpDeclaredSymbolInfoFactoryService()
    {
    }

    private static ImmutableArray<string> GetInheritanceNames(StringTable stringTable, BaseListSyntax baseList)
    {
        if (baseList == null)
        {
            return [];
        }

        var builder = ArrayBuilder<string>.GetInstance(baseList.Types.Count);

        // It's not sufficient to just store the textual names we see in the inheritance list
        // of a type.  For example if we have:
        //
        //   using Y = X;
        //      ...
        //      using Z = Y;
        //      ...
        //      class C : Z
        //
        // It's insufficient to just state that 'C' derives from 'Z'.  If we search for derived
        // types from 'B' we won't examine 'C'.  To solve this, we keep track of the aliasing
        // that occurs in containing scopes.  Then, when we're adding an inheritance name we 
        // walk the alias maps and we also add any names that these names alias to.  In the
        // above example we'd put Z, Y, and X in the inheritance names list for 'C'.

        // Each dictionary in this list is a mapping from alias name to the name of the thing
        // it aliases.  Then, each scope with alias mapping gets its own entry in this list.
        // For the above example, we would produce:  [{Z => Y}, {Y => X}]
        var aliasMaps = AllocateAliasMapList();
        try
        {
            AddAliasMaps(baseList, aliasMaps);

            foreach (var baseType in baseList.Types)
            {
                AddInheritanceName(builder, baseType.Type, aliasMaps);
            }

            Intern(stringTable, builder);
            return builder.ToImmutableAndFree();
        }
        finally
        {
            FreeAliasMapList(aliasMaps);
        }
    }

    private static void AddAliasMaps(SyntaxNode node, List<Dictionary<string, string>> aliasMaps)
    {
        for (var current = node; current != null; current = current.Parent)
        {
            if (current is BaseNamespaceDeclarationSyntax nsDecl)
            {
                ProcessUsings(aliasMaps, nsDecl.Usings);
            }
            else if (current is CompilationUnitSyntax compilationUnit)
            {
                ProcessUsings(aliasMaps, compilationUnit.Usings);
            }
        }
    }

    private static void ProcessUsings(List<Dictionary<string, string>> aliasMaps, SyntaxList<UsingDirectiveSyntax> usings)
    {
        Dictionary<string, string> aliasMap = null;

        foreach (var usingDecl in usings)
        {
            if (usingDecl.Alias != null)
            {
                var mappedName = GetTypeName(usingDecl.Name);
                if (mappedName != null)
                {
                    aliasMap ??= AllocateAliasMap();

                    // If we have:  using X = Goo, then we store a mapping from X -> Goo
                    // here.  That way if we see a class that inherits from X we also state
                    // that it inherits from Goo as well.
                    aliasMap[usingDecl.Alias.Name.Identifier.ValueText] = mappedName;
                }
            }
        }

        if (aliasMap != null)
        {
            aliasMaps.Add(aliasMap);
        }
    }

    private static void AddInheritanceName(
        ArrayBuilder<string> builder, TypeSyntax type,
        List<Dictionary<string, string>> aliasMaps)
    {
        var name = GetTypeName(type);
        if (name != null)
        {
            // First, add the name that the typename that the type directly says it inherits from.
            builder.Add(name);

            // Now, walk the alias chain and add any names this alias may eventually map to.
            var currentName = name;
            foreach (var aliasMap in aliasMaps)
            {
                if (aliasMap.TryGetValue(currentName, out var mappedName))
                {
                    // Looks like this could be an alias.  Also include the name the alias points to
                    builder.Add(mappedName);

                    // Keep on searching.  An alias in an inner namespcae can refer to an 
                    // alias in an outer namespace.  
                    currentName = mappedName;
                }
            }
        }
    }

    protected override void AddLocalFunctionInfos(
        MemberDeclarationSyntax memberDeclaration,
        StringTable stringTable,
        ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos,
        string containerDisplayName,
        string fullyQualifiedContainerName,
        CancellationToken cancellationToken)
    {
        using var pooledQueue = SharedPools.Default<Queue<SyntaxNodeOrToken>>().GetPooledObject();
        var queue = pooledQueue.Object;
        queue.Enqueue(memberDeclaration);
        while (!queue.IsEmpty())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = queue.Dequeue();
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    queue.Enqueue(child);
                }
            }

            if (node.AsNode() is LocalFunctionStatementSyntax localFunction)
            {
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    localFunction.Identifier.ValueText, GetMethodSuffix(localFunction),
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    isPartial: false,
                    hasAttributes: localFunction.AttributeLists.Any(),
                    DeclaredSymbolInfoKind.Method,
                    Accessibility.Private,
                    localFunction.Identifier.Span,
                    inheritanceNames: [],
                    parameterCount: localFunction.ParameterList.Parameters.Count,
                    typeParameterCount: localFunction.TypeParameterList?.Parameters.Count ?? 0));
            }
        }
    }

    protected override DeclaredSymbolInfo? GetTypeDeclarationInfo(
        SyntaxNode container,
        TypeDeclarationSyntax typeDeclaration,
        StringTable stringTable,
        string containerDisplayName,
        string fullyQualifiedContainerName)
    {
        // If this is a part of partial type that only contains nested types, then we don't make an info type for
        // it. That's because we effectively think of this as just being a virtual container just to hold the nested
        // types, and not something someone would want to explicitly navigate to itself.  Similar to how we think of
        // namespaces.
        if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
            typeDeclaration.Members.Any() &&
            typeDeclaration.Members.All(m => m is BaseTypeDeclarationSyntax))
        {
            return null;
        }

        return DeclaredSymbolInfo.Create(
            stringTable,
            typeDeclaration.Identifier.ValueText,
            GetTypeParameterSuffix(typeDeclaration.TypeParameterList),
            containerDisplayName,
            fullyQualifiedContainerName,
            typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
            typeDeclaration.AttributeLists.Any(),
            typeDeclaration.Kind() switch
            {
                SyntaxKind.ClassDeclaration => DeclaredSymbolInfoKind.Class,
                SyntaxKind.InterfaceDeclaration => DeclaredSymbolInfoKind.Interface,
                SyntaxKind.StructDeclaration => DeclaredSymbolInfoKind.Struct,
                SyntaxKind.RecordDeclaration => DeclaredSymbolInfoKind.Record,
                SyntaxKind.RecordStructDeclaration => DeclaredSymbolInfoKind.RecordStruct,
                _ => throw ExceptionUtilities.UnexpectedValue(typeDeclaration.Kind()),
            },
            GetAccessibility(container, typeDeclaration.Modifiers),
            typeDeclaration.Identifier.Span,
            GetInheritanceNames(stringTable, typeDeclaration.BaseList),
            IsNestedType(typeDeclaration),
            typeParameterCount: typeDeclaration.TypeParameterList?.Parameters.Count ?? 0);
    }

    protected override DeclaredSymbolInfo GetEnumDeclarationInfo(
        SyntaxNode container,
        EnumDeclarationSyntax enumDeclaration,
        StringTable stringTable,
        string containerDisplayName,
        string fullyQualifiedContainerName)
    {
        return DeclaredSymbolInfo.Create(
            stringTable,
            enumDeclaration.Identifier.ValueText, null,
            containerDisplayName,
            fullyQualifiedContainerName,
            enumDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
            enumDeclaration.AttributeLists.Any(),
            DeclaredSymbolInfoKind.Enum,
            GetAccessibility(container, enumDeclaration.Modifiers),
            enumDeclaration.Identifier.Span,
            inheritanceNames: [],
            isNestedType: IsNestedType(enumDeclaration));
    }

    protected override void AddMemberDeclarationInfos(
        SyntaxNode container,
        MemberDeclarationSyntax node,
        StringTable stringTable,
        ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos,
        string containerDisplayName,
        string fullyQualifiedContainerName)
    {
        Contract.ThrowIfTrue(node is TypeDeclarationSyntax);
        switch (node.Kind())
        {
            case SyntaxKind.ConstructorDeclaration:
                var ctorDecl = (ConstructorDeclarationSyntax)node;
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    ctorDecl.Identifier.ValueText,
                    GetConstructorSuffix(ctorDecl),
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    ctorDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                    ctorDecl.AttributeLists.Any(),
                    DeclaredSymbolInfoKind.Constructor,
                    GetAccessibility(container, ctorDecl.Modifiers),
                    ctorDecl.Identifier.Span,
                    inheritanceNames: [],
                    parameterCount: ctorDecl.ParameterList?.Parameters.Count ?? 0));
                return;
            case SyntaxKind.DelegateDeclaration:
                var delegateDecl = (DelegateDeclarationSyntax)node;
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    delegateDecl.Identifier.ValueText,
                    GetTypeParameterSuffix(delegateDecl.TypeParameterList),
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    delegateDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                    delegateDecl.AttributeLists.Any(),
                    DeclaredSymbolInfoKind.Delegate,
                    GetAccessibility(container, delegateDecl.Modifiers),
                    delegateDecl.Identifier.Span,
                    inheritanceNames: []));
                return;
            case SyntaxKind.EnumMemberDeclaration:
                var enumMember = (EnumMemberDeclarationSyntax)node;
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    enumMember.Identifier.ValueText, null,
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    enumMember.Modifiers.Any(SyntaxKind.PartialKeyword),
                    enumMember.AttributeLists.Any(),
                    DeclaredSymbolInfoKind.EnumMember,
                    Accessibility.Public,
                    enumMember.Identifier.Span,
                    inheritanceNames: []));
                return;
            case SyntaxKind.EventDeclaration:
                var eventDecl = (EventDeclarationSyntax)node;
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    eventDecl.Identifier.ValueText, null,
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    eventDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                    eventDecl.AttributeLists.Any(),
                    DeclaredSymbolInfoKind.Event,
                    GetAccessibility(container, eventDecl.Modifiers),
                    eventDecl.Identifier.Span,
                    inheritanceNames: []));
                return;
            case SyntaxKind.IndexerDeclaration:
                var indexerDecl = (IndexerDeclarationSyntax)node;
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    "this", GetIndexerSuffix(indexerDecl),
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    indexerDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                    indexerDecl.AttributeLists.Any(),
                    DeclaredSymbolInfoKind.Indexer,
                    GetAccessibility(container, indexerDecl.Modifiers),
                    indexerDecl.ThisKeyword.Span,
                    inheritanceNames: []));
                return;
            case SyntaxKind.MethodDeclaration:
                var method = (MethodDeclarationSyntax)node;
                var isExtensionMethod = IsExtensionMethod(method);
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    method.Identifier.ValueText, GetMethodSuffix(method),
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    method.Modifiers.Any(SyntaxKind.PartialKeyword),
                    method.AttributeLists.Any(),
                    isExtensionMethod ? DeclaredSymbolInfoKind.ExtensionMethod : DeclaredSymbolInfoKind.Method,
                    GetAccessibility(container, method.Modifiers),
                    method.Identifier.Span,
                    inheritanceNames: [],
                    parameterCount: method.ParameterList?.Parameters.Count ?? 0,
                    typeParameterCount: method.TypeParameterList?.Parameters.Count ?? 0));
                return;
            case SyntaxKind.PropertyDeclaration:
                var property = (PropertyDeclarationSyntax)node;
                declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                    stringTable,
                    property.Identifier.ValueText, null,
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    property.Modifiers.Any(SyntaxKind.PartialKeyword),
                    property.AttributeLists.Any(),
                    DeclaredSymbolInfoKind.Property,
                    GetAccessibility(container, property.Modifiers),
                    property.Identifier.Span,
                    inheritanceNames: []));
                return;
            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.EventFieldDeclaration:
                var fieldDeclaration = (BaseFieldDeclarationSyntax)node;
                foreach (var variableDeclarator in fieldDeclaration.Declaration.Variables)
                {
                    var kind = fieldDeclaration is EventFieldDeclarationSyntax
                        ? DeclaredSymbolInfoKind.Event
                        : fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword)
                            ? DeclaredSymbolInfoKind.Constant
                            : DeclaredSymbolInfoKind.Field;

                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        variableDeclarator.Identifier.ValueText, null,
                        containerDisplayName,
                        fullyQualifiedContainerName,
                        fieldDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
                        fieldDeclaration.AttributeLists.Any(),
                        kind,
                        GetAccessibility(container, fieldDeclaration.Modifiers),
                        variableDeclarator.Identifier.Span,
                        inheritanceNames: []));
                }

                return;
        }
    }

    protected override void AddSynthesizedDeclaredSymbolInfos(
        SyntaxNode container,
        MemberDeclarationSyntax memberDeclaration,
        StringTable stringTable,
        ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos,
        string containerDisplayName,
        string fullyQualifiedContainerName,
        CancellationToken cancellationToken)
    {
        // Add synthesized properties for record primary constructors that are not backed by an existing field
        // or property.
        if (memberDeclaration is RecordDeclarationSyntax { ParameterList: { Parameters.Count: > 0 } parameterList } recordDeclaration)
        {
            using var _ = PooledHashSet<string>.GetInstance(out var existingFieldPropNames);

            foreach (var member in recordDeclaration.Members)
            {
                switch (member)
                {
                    case PropertyDeclarationSyntax property:
                        existingFieldPropNames.Add(property.Identifier.ValueText);
                        continue;
                    case FieldDeclarationSyntax fieldDeclaration:
                        foreach (var variable in fieldDeclaration.Declaration.Variables)
                            existingFieldPropNames.Add(variable.Identifier.ValueText);
                        continue;
                }
            }

            foreach (var parameter in parameterList.Parameters)
            {
                if (!existingFieldPropNames.Contains(parameter.Identifier.ValueText))
                {
                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        parameter.Identifier.ValueText, null,
                        containerDisplayName,
                        fullyQualifiedContainerName,
                        isPartial: false,
                        parameter.AttributeLists.Any(),
                        DeclaredSymbolInfoKind.Property,
                        Accessibility.Public,
                        parameter.Identifier.Span,
                        inheritanceNames: []));
                }
            }
        }
    }

    protected override SyntaxList<MemberDeclarationSyntax> GetChildren(CompilationUnitSyntax node)
        => node.Members;

    protected override SyntaxList<MemberDeclarationSyntax> GetChildren(BaseNamespaceDeclarationSyntax node)
        => node.Members;

    protected override SyntaxList<MemberDeclarationSyntax> GetChildren(TypeDeclarationSyntax node)
        => node.Members;

    protected override IEnumerable<MemberDeclarationSyntax> GetChildren(EnumDeclarationSyntax node)
        => node.Members;

    protected override SyntaxList<UsingDirectiveSyntax> GetUsingAliases(CompilationUnitSyntax node)
        => node.Usings;

    protected override SyntaxList<UsingDirectiveSyntax> GetUsingAliases(BaseNamespaceDeclarationSyntax node)
        => node.Usings;

    protected override NameSyntax GetName(BaseNamespaceDeclarationSyntax node)
        => node.Name;

    protected override NameSyntax GetLeft(QualifiedNameSyntax node)
        => node.Left;

    protected override NameSyntax GetRight(QualifiedNameSyntax node)
        => node.Right;

    protected override SyntaxToken GetIdentifier(IdentifierNameSyntax node)
        => node.Identifier;

    private static bool IsNestedType(BaseTypeDeclarationSyntax typeDecl)
        => typeDecl.Parent is BaseTypeDeclarationSyntax;

    private static string GetConstructorSuffix(ConstructorDeclarationSyntax constructor)
        => constructor.Modifiers.Any(SyntaxKind.StaticKeyword)
            ? ".static " + constructor.Identifier + "()"
            : GetSuffix('(', ')', constructor.ParameterList.Parameters);

    private static string GetMethodSuffix(MethodDeclarationSyntax method)
        => GetTypeParameterSuffix(method.TypeParameterList) +
           GetSuffix('(', ')', method.ParameterList.Parameters);

    private static string GetMethodSuffix(LocalFunctionStatementSyntax method)
        => GetTypeParameterSuffix(method.TypeParameterList) +
           GetSuffix('(', ')', method.ParameterList.Parameters);

    private static string GetIndexerSuffix(IndexerDeclarationSyntax indexer)
        => GetSuffix('[', ']', indexer.ParameterList.Parameters);

    private static string GetTypeParameterSuffix(TypeParameterListSyntax typeParameterList)
    {
        if (typeParameterList == null)
        {
            return null;
        }

        var pooledBuilder = PooledStringBuilder.GetInstance();

        var builder = pooledBuilder.Builder;
        builder.Append('<');

        var first = true;
        foreach (var parameter in typeParameterList.Parameters)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(parameter.Identifier.Text);
            first = false;
        }

        builder.Append('>');

        return pooledBuilder.ToStringAndFree();
    }

    /// <summary>
    /// Builds up the suffix to show for something with parameters in navigate-to.
    /// While it would be nice to just use the compiler SymbolDisplay API for this,
    /// it would be too expensive as it requires going back to Symbols (which requires
    /// creating compilations, etc.) in a perf sensitive area.
    /// 
    /// So, instead, we just build a reasonable suffix using the pure syntax that a 
    /// user provided.  That means that if they wrote "Method(System.Int32 i)" we'll 
    /// show that as "Method(System.Int32)" not "Method(int)".  Given that this is
    /// actually what the user wrote, and it saves us from ever having to go back to
    /// symbols/compilations, this is well worth it, even if it does mean we have to
    /// create our own 'symbol display' logic here.
    /// </summary>
    private static string GetSuffix(
        char openBrace, char closeBrace, SeparatedSyntaxList<ParameterSyntax> parameters)
    {
        var pooledBuilder = PooledStringBuilder.GetInstance();

        var builder = pooledBuilder.Builder;
        builder.Append(openBrace);
        AppendParameters(parameters, builder);
        builder.Append(closeBrace);

        return pooledBuilder.ToStringAndFree();
    }

    private static void AppendParameters(SeparatedSyntaxList<ParameterSyntax> parameters, StringBuilder builder)
    {
        var first = true;
        foreach (var parameter in parameters)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            foreach (var modifier in parameter.Modifiers)
            {
                builder.Append(modifier.Text);
                builder.Append(' ');
            }

            if (parameter.Type != null)
            {
                builder.Append(parameter.Type.ConvertToSingleLine().ToString());
            }
            else
            {
                builder.Append(parameter.Identifier.Text);
            }

            first = false;
        }
    }

    protected override string GetContainerDisplayName(MemberDeclarationSyntax node)
        => CSharpSyntaxFacts.Instance.GetDisplayName(node, DisplayNameOptions.IncludeTypeParameters);

    protected override string GetFullyQualifiedContainerName(MemberDeclarationSyntax node, string rootNamespace)
        => CSharpSyntaxFacts.Instance.GetDisplayName(node, DisplayNameOptions.IncludeNamespaces);

    private static Accessibility GetAccessibility(SyntaxNode container, SyntaxTokenList modifiers)
    {
        var sawInternal = false;
        foreach (var modifier in modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword: return Accessibility.Public;
                case SyntaxKind.PrivateKeyword: return Accessibility.Private;
                case SyntaxKind.ProtectedKeyword: return Accessibility.Protected;
                case SyntaxKind.InternalKeyword:
                    sawInternal = true;
                    continue;
            }
        }

        if (sawInternal)
            return Accessibility.Internal;

        // No accessibility modifiers:
        switch (container.Kind())
        {
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
                // Anything without modifiers is private if it's in a class/struct declaration.
                return Accessibility.Private;
            case SyntaxKind.InterfaceDeclaration:
                // Anything without modifiers is public if it's in an interface declaration.
                return Accessibility.Public;
            case SyntaxKind.CompilationUnit:
                // Things are private by default in script
                if (((CSharpParseOptions)container.SyntaxTree.Options).Kind == SourceCodeKind.Script)
                    return Accessibility.Private;

                return Accessibility.Internal;

            default:
                // Otherwise it's internal
                return Accessibility.Internal;
        }
    }

    private static string GetTypeName(TypeSyntax type)
    {
        if (type is SimpleNameSyntax simpleName)
        {
            return GetSimpleTypeName(simpleName);
        }
        else if (type is QualifiedNameSyntax qualifiedName)
        {
            return GetSimpleTypeName(qualifiedName.Right);
        }
        else if (type is AliasQualifiedNameSyntax aliasName)
        {
            return GetSimpleTypeName(aliasName.Name);
        }

        return null;
    }

    private static string GetSimpleTypeName(SimpleNameSyntax name)
        => name.Identifier.ValueText;

    private static bool IsExtensionMethod(MethodDeclarationSyntax method)
        => method.ParameterList.Parameters is [var parameter, ..] && parameter.Modifiers.Any(SyntaxKind.ThisKeyword);

    // Root namespace is a VB only concept, which basically means root namespace is always global in C#.
    protected override string GetRootNamespace(CompilationOptions compilationOptions)
        => string.Empty;

    protected override bool TryGetAliasesFromUsingDirective(
        UsingDirectiveSyntax usingDirectiveNode, out ImmutableArray<(string aliasName, string name)> aliases)
    {
        if (usingDirectiveNode.Alias != null)
        {
            if (TryGetSimpleTypeName(usingDirectiveNode.Alias.Name, typeParameterNames: null, out var aliasName, out _) &&
                TryGetSimpleTypeName(usingDirectiveNode.NamespaceOrType, typeParameterNames: null, out var name, out _))
            {
                aliases = [(aliasName, name)];
                return true;
            }
        }

        aliases = default;
        return false;
    }

    protected override string GetReceiverTypeName(MethodDeclarationSyntax methodDeclaration)
    {
        Debug.Assert(IsExtensionMethod(methodDeclaration));

        var typeParameterNames = methodDeclaration.TypeParameterList?.Parameters.SelectAsArray(p => p.Identifier.Text);
        TryGetSimpleTypeName(methodDeclaration.ParameterList.Parameters[0].Type, typeParameterNames, out var targetTypeName, out var isArray);
        return CreateReceiverTypeString(targetTypeName, isArray);
    }

    private static bool TryGetSimpleTypeName(SyntaxNode node, ImmutableArray<string>? typeParameterNames, out string simpleTypeName, out bool isArray)
    {
        isArray = false;

        if (node is TypeSyntax typeNode)
        {
            switch (typeNode)
            {
                case IdentifierNameSyntax identifierNameNode:
                    // We consider it a complex method if the receiver type is a type parameter.
                    var text = identifierNameNode.Identifier.Text;
                    simpleTypeName = typeParameterNames?.Contains(text) == true ? null : text;
                    return simpleTypeName != null;

                case ArrayTypeSyntax arrayTypeNode:
                    isArray = true;
                    return TryGetSimpleTypeName(arrayTypeNode.ElementType, typeParameterNames, out simpleTypeName, out _);

                case GenericNameSyntax genericNameNode:
                    var name = genericNameNode.Identifier.Text;
                    var arity = genericNameNode.Arity;
                    simpleTypeName = arity == 0 ? name : name + ArityUtilities.GetMetadataAritySuffix(arity);
                    return true;

                case PredefinedTypeSyntax predefinedTypeNode:
                    simpleTypeName = GetSpecialTypeName(predefinedTypeNode);
                    return simpleTypeName != null;

                case AliasQualifiedNameSyntax aliasQualifiedNameNode:
                    return TryGetSimpleTypeName(aliasQualifiedNameNode.Name, typeParameterNames, out simpleTypeName, out _);

                case QualifiedNameSyntax qualifiedNameNode:
                    // For an identifier to the right of a '.', it can't be a type parameter,
                    // so we don't need to check for it further.
                    return TryGetSimpleTypeName(qualifiedNameNode.Right, typeParameterNames: null, out simpleTypeName, out _);

                case NullableTypeSyntax nullableNode:
                    // Ignore nullability, becase nullable reference type might not be enabled universally.
                    // In the worst case we just include more methods to check in out filter.
                    return TryGetSimpleTypeName(nullableNode.ElementType, typeParameterNames, out simpleTypeName, out isArray);

                case TupleTypeSyntax tupleType:
                    simpleTypeName = CreateValueTupleTypeString(tupleType.Elements.Count);
                    return true;
            }
        }

        simpleTypeName = null;
        return false;
    }

    private static string GetSpecialTypeName(PredefinedTypeSyntax predefinedTypeNode)
    {
        var kind = predefinedTypeNode.Keyword.Kind();
        return kind switch
        {
            SyntaxKind.BoolKeyword => "Boolean",
            SyntaxKind.ByteKeyword => "Byte",
            SyntaxKind.SByteKeyword => "SByte",
            SyntaxKind.ShortKeyword => "Int16",
            SyntaxKind.UShortKeyword => "UInt16",
            SyntaxKind.IntKeyword => "Int32",
            SyntaxKind.UIntKeyword => "UInt32",
            SyntaxKind.LongKeyword => "Int64",
            SyntaxKind.ULongKeyword => "UInt64",
            SyntaxKind.DoubleKeyword => "Double",
            SyntaxKind.FloatKeyword => "Single",
            SyntaxKind.DecimalKeyword => "Decimal",
            SyntaxKind.StringKeyword => "String",
            SyntaxKind.CharKeyword => "Char",
            SyntaxKind.ObjectKeyword => "Object",
            _ => null,
        };
    }
}
