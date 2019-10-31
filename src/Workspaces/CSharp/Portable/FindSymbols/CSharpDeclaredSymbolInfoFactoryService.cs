// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FindSymbols
{
    [ExportLanguageService(typeof(IDeclaredSymbolInfoFactoryService), LanguageNames.CSharp), Shared]
    internal class CSharpDeclaredSymbolInfoFactoryService : AbstractDeclaredSymbolInfoFactoryService
    {
        [ImportingConstructor]
        public CSharpDeclaredSymbolInfoFactoryService()
        {
        }

        private ImmutableArray<string> GetInheritanceNames(StringTable stringTable, BaseListSyntax baseList)
        {
            if (baseList == null)
            {
                return ImmutableArray<string>.Empty;
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

        private void AddAliasMaps(SyntaxNode node, List<Dictionary<string, string>> aliasMaps)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    ProcessUsings(aliasMaps, ((NamespaceDeclarationSyntax)current).Usings);
                }
                else if (current.IsKind(SyntaxKind.CompilationUnit))
                {
                    ProcessUsings(aliasMaps, ((CompilationUnitSyntax)current).Usings);
                }
            }
        }

        private void ProcessUsings(List<Dictionary<string, string>> aliasMaps, SyntaxList<UsingDirectiveSyntax> usings)
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

        private void AddInheritanceName(
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

        public override bool TryGetDeclaredSymbolInfo(StringTable stringTable, SyntaxNode node, string rootNamespace, out DeclaredSymbolInfo declaredSymbolInfo)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    var classDecl = (ClassDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        classDecl.Identifier.ValueText,
                        GetTypeParameterSuffix(classDecl.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Class,
                        GetAccessibility(classDecl, classDecl.Modifiers),
                        classDecl.Identifier.Span,
                        GetInheritanceNames(stringTable, classDecl.BaseList),
                        IsNestedType(classDecl));
                    return true;
                case SyntaxKind.EnumDeclaration:
                    var enumDecl = (EnumDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        enumDecl.Identifier.ValueText, null,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Enum,
                        GetAccessibility(enumDecl, enumDecl.Modifiers),
                        enumDecl.Identifier.Span,
                        inheritanceNames: ImmutableArray<string>.Empty,
                        isNestedType: IsNestedType(enumDecl));
                    return true;
                case SyntaxKind.InterfaceDeclaration:
                    var interfaceDecl = (InterfaceDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        interfaceDecl.Identifier.ValueText, GetTypeParameterSuffix(interfaceDecl.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Interface,
                        GetAccessibility(interfaceDecl, interfaceDecl.Modifiers),
                        interfaceDecl.Identifier.Span,
                        GetInheritanceNames(stringTable, interfaceDecl.BaseList),
                        IsNestedType(interfaceDecl));
                    return true;
                case SyntaxKind.StructDeclaration:
                    var structDecl = (StructDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        structDecl.Identifier.ValueText, GetTypeParameterSuffix(structDecl.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Struct,
                        GetAccessibility(structDecl, structDecl.Modifiers),
                        structDecl.Identifier.Span,
                        GetInheritanceNames(stringTable, structDecl.BaseList),
                        IsNestedType(structDecl));
                    return true;
                case SyntaxKind.ConstructorDeclaration:
                    var ctorDecl = (ConstructorDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        ctorDecl.Identifier.ValueText,
                        GetConstructorSuffix(ctorDecl),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Constructor,
                        GetAccessibility(ctorDecl, ctorDecl.Modifiers),
                        ctorDecl.Identifier.Span,
                        inheritanceNames: ImmutableArray<string>.Empty,
                        parameterCount: ctorDecl.ParameterList?.Parameters.Count ?? 0);
                    return true;
                case SyntaxKind.DelegateDeclaration:
                    var delegateDecl = (DelegateDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        delegateDecl.Identifier.ValueText,
                        GetTypeParameterSuffix(delegateDecl.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Delegate,
                        GetAccessibility(delegateDecl, delegateDecl.Modifiers),
                        delegateDecl.Identifier.Span,
                        inheritanceNames: ImmutableArray<string>.Empty);
                    return true;
                case SyntaxKind.EnumMemberDeclaration:
                    var enumMember = (EnumMemberDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        enumMember.Identifier.ValueText, null,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.EnumMember,
                        Accessibility.Public,
                        enumMember.Identifier.Span,
                        inheritanceNames: ImmutableArray<string>.Empty);
                    return true;
                case SyntaxKind.EventDeclaration:
                    var eventDecl = (EventDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        eventDecl.Identifier.ValueText, null,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Event,
                        GetAccessibility(eventDecl, eventDecl.Modifiers),
                        eventDecl.Identifier.Span,
                        inheritanceNames: ImmutableArray<string>.Empty);
                    return true;
                case SyntaxKind.IndexerDeclaration:
                    var indexerDecl = (IndexerDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        "this", GetIndexerSuffix(indexerDecl),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Indexer,
                        GetAccessibility(indexerDecl, indexerDecl.Modifiers),
                        indexerDecl.ThisKeyword.Span,
                        inheritanceNames: ImmutableArray<string>.Empty);
                    return true;
                case SyntaxKind.MethodDeclaration:
                    var method = (MethodDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        method.Identifier.ValueText, GetMethodSuffix(method),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        IsExtensionMethod(method) ? DeclaredSymbolInfoKind.ExtensionMethod : DeclaredSymbolInfoKind.Method,
                        GetAccessibility(method, method.Modifiers),
                        method.Identifier.Span,
                        inheritanceNames: ImmutableArray<string>.Empty,
                        parameterCount: method.ParameterList?.Parameters.Count ?? 0,
                        typeParameterCount: method.TypeParameterList?.Parameters.Count ?? 0);
                    return true;
                case SyntaxKind.PropertyDeclaration:
                    var property = (PropertyDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        stringTable,
                        property.Identifier.ValueText, null,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Property,
                        GetAccessibility(property, property.Modifiers),
                        property.Identifier.Span,
                        inheritanceNames: ImmutableArray<string>.Empty);
                    return true;
                case SyntaxKind.VariableDeclarator:
                    // could either be part of a field declaration or an event field declaration
                    var variableDeclarator = (VariableDeclaratorSyntax)node;
                    var variableDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
                    if (variableDeclaration?.Parent is BaseFieldDeclarationSyntax fieldDeclaration)
                    {
                        var kind = fieldDeclaration is EventFieldDeclarationSyntax
                            ? DeclaredSymbolInfoKind.Event
                            : fieldDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.ConstKeyword)
                                ? DeclaredSymbolInfoKind.Constant
                                : DeclaredSymbolInfoKind.Field;

                        declaredSymbolInfo = new DeclaredSymbolInfo(
                            stringTable,
                            variableDeclarator.Identifier.ValueText, null,
                            GetContainerDisplayName(fieldDeclaration.Parent),
                            GetFullyQualifiedContainerName(fieldDeclaration.Parent),
                            kind,
                            GetAccessibility(fieldDeclaration, fieldDeclaration.Modifiers),
                            variableDeclarator.Identifier.Span,
                            inheritanceNames: ImmutableArray<string>.Empty);
                        return true;
                    }

                    break;
            }

            declaredSymbolInfo = default;
            return false;
        }

        private bool IsNestedType(BaseTypeDeclarationSyntax typeDecl)
            => typeDecl.Parent is BaseTypeDeclarationSyntax;

        private string GetConstructorSuffix(ConstructorDeclarationSyntax constructor)
            => constructor.Modifiers.Any(SyntaxKind.StaticKeyword)
                ? ".static " + constructor.Identifier + "()"
                : GetSuffix('(', ')', constructor.ParameterList.Parameters);

        private string GetMethodSuffix(MethodDeclarationSyntax method)
            => GetTypeParameterSuffix(method.TypeParameterList) +
               GetSuffix('(', ')', method.ParameterList.Parameters);

        private string GetIndexerSuffix(IndexerDeclarationSyntax indexer)
            => GetSuffix('[', ']', indexer.ParameterList.Parameters);

        private string GetTypeParameterSuffix(TypeParameterListSyntax typeParameterList)
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
        private string GetSuffix(
            char openBrace, char closeBrace, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();

            var builder = pooledBuilder.Builder;
            builder.Append(openBrace);
            AppendParameters(parameters, builder);
            builder.Append(closeBrace);

            return pooledBuilder.ToStringAndFree();
        }

        private void AppendParameters(SeparatedSyntaxList<ParameterSyntax> parameters, StringBuilder builder)
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
                    AppendTokens(parameter.Type, builder);
                }
                else
                {
                    builder.Append(parameter.Identifier.Text);
                }

                first = false;
            }
        }

        private string GetContainerDisplayName(SyntaxNode node)
            => CSharpSyntaxFactsService.Instance.GetDisplayName(node, DisplayNameOptions.IncludeTypeParameters);

        private string GetFullyQualifiedContainerName(SyntaxNode node)
            => CSharpSyntaxFactsService.Instance.GetDisplayName(node, DisplayNameOptions.IncludeNamespaces);

        private Accessibility GetAccessibility(SyntaxNode node, SyntaxTokenList modifiers)
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
            {
                return Accessibility.Internal;
            }

            // No accessibility modifiers:
            switch (node.Parent.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                    // Anything without modifiers is private if it's in a class/struct declaration.
                    return Accessibility.Private;
                case SyntaxKind.InterfaceDeclaration:
                    // Anything without modifiers is public if it's in an interface declaration.
                    return Accessibility.Public;
                case SyntaxKind.CompilationUnit:
                    // Things are private by default in script
                    if (((CSharpParseOptions)node.SyntaxTree.Options).Kind == SourceCodeKind.Script)
                    {
                        return Accessibility.Private;
                    }

                    return Accessibility.Internal;

                default:
                    // Otherwise it's internal
                    return Accessibility.Internal;
            }
        }

        private string GetTypeName(TypeSyntax type)
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

        private bool IsExtensionMethod(MethodDeclarationSyntax method)
            => method.ParameterList.Parameters.Count > 0 &&
               method.ParameterList.Parameters[0].Modifiers.Any(SyntaxKind.ThisKeyword);

        // Root namespace is a VB only concept, which basically means root namespace is always global in C#.
        public override string GetRootNamespace(CompilationOptions compilationOptions)
            => string.Empty;

        public override bool TryGetAliasesFromUsingDirective(SyntaxNode node, out ImmutableArray<(string aliasName, string name)> aliases)
        {
            if (node is UsingDirectiveSyntax usingDirectiveNode && usingDirectiveNode.Alias != null)
            {
                if (TryGetSimpleTypeName(usingDirectiveNode.Alias.Name, typeParameterNames: null, out var aliasName) &&
                    TryGetSimpleTypeName(usingDirectiveNode.Name, typeParameterNames: null, out var name))
                {
                    aliases = ImmutableArray.Create<(string, string)>((aliasName, name));
                    return true;
                }
            }

            aliases = default;
            return false;
        }

        public override bool TryGetTargetTypeName(SyntaxNode node, out string targetTypeName)
        {
            if (node is MethodDeclarationSyntax methodDeclaration && IsExtensionMethod(methodDeclaration))
            {
                var typeParameterNames = methodDeclaration.TypeParameterList?.Parameters.SelectAsArray(p => p.Identifier.Text);
                TryGetSimpleTypeName(methodDeclaration.ParameterList.Parameters[0].Type, typeParameterNames, out targetTypeName);

                // We always return true here, which indicates we have a valid taget type name. (which would be null for a complex type though).
                return true;
            }

            targetTypeName = null;
            return false;
        }

        private static bool TryGetSimpleTypeName(SyntaxNode node, ImmutableArray<string>? typeParameterNames, out string simpleTypeName)
        {
            if (node is TypeSyntax typeNode)
            {
                switch (typeNode)
                {
                    case IdentifierNameSyntax identifierNameNode:
                        // We consider it a complex method if the receiver type is a type parameter.
                        var text = identifierNameNode.Identifier.Text;
                        simpleTypeName = typeParameterNames?.Contains(text) == true ? null : text;
                        return simpleTypeName != null;

                    case GenericNameSyntax genericNameNode:
                        var name = genericNameNode.Identifier.Text;
                        var arity = genericNameNode.Arity;
                        simpleTypeName = arity == 0 ? name : name + GetMetadataAritySuffix(arity);
                        return true;

                    case PredefinedTypeSyntax predefinedTypeNode:
                        simpleTypeName = GetSpecialTypeName(predefinedTypeNode);
                        return simpleTypeName != null;

                    case AliasQualifiedNameSyntax aliasQualifiedNameNode:
                        return TryGetSimpleTypeName(aliasQualifiedNameNode.Name, typeParameterNames, out simpleTypeName);

                    case QualifiedNameSyntax qualifiedNameNode:
                        // For an identifier to the right of a '.', it can't be a type parameter,
                        // so we don't need to check for it further.
                        return TryGetSimpleTypeName(qualifiedNameNode.Right, typeParameterNames: null, out simpleTypeName);

                    case NullableTypeSyntax nullableNode:
                        // Ignore nullability, becase nullable reference type might not be enabled universally.
                        // In the worst case we just include more methods to check in out filter.
                        return TryGetSimpleTypeName(nullableNode.ElementType, typeParameterNames, out simpleTypeName);
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
}
