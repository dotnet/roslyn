// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.EditorConfig.Parsing;
using Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static partial class EditorConfigNamingStyleParser
{
    internal static bool TryGetSymbolSpecification(
        Section section,
        string namingRuleTitle,
        IReadOnlyDictionary<string, string> entries,
        IReadOnlyDictionary<string, TextLine> lines,
        [NotNullWhen(true)] out ApplicableSymbolInfo? applicableSymbolInfo)
    {
        if (TryGetSymbolProperties(
            namingRuleTitle,
            entries,
            out var specification,
            out var kinds,
            out var accessibilities,
            out var modifiers))
        {
            applicableSymbolInfo = new ApplicableSymbolInfo(
                OptionName: new(section, specification.GetSpan(lines), specification.Value),
                SymbolKinds: new(section, kinds.GetSpan(lines), kinds.Value),
                Accessibilities: new(section, accessibilities.GetSpan(lines), accessibilities.Value),
                Modifiers: new(section, modifiers.GetSpan(lines), modifiers.Value));

            return true;
        }

        applicableSymbolInfo = null;
        return false;
    }

    private static bool TryGetSymbolSpecification(
        string namingRuleTitle,
        IReadOnlyDictionary<string, string> entries,
        [NotNullWhen(true)] out SymbolSpecification? symbolSpec)
    {
        if (TryGetSymbolProperties(
            namingRuleTitle,
            entries,
            out var specification,
            out var kinds,
            out var accessibilities,
            out var modifiers))
        {
            symbolSpec = new SymbolSpecification(
                id: Guid.NewGuid(),
                specification.Value,
                kinds.Value,
                accessibilities.Value,
                modifiers.Value);

            return true;
        }

        symbolSpec = null;
        return false;
    }

    private static bool TryGetSymbolProperties(
        string namingRuleTitle,
        IReadOnlyDictionary<string, string> entries,
        out Property<string> specification,
        out Property<ImmutableArray<SymbolKindOrTypeKind>> kinds,
        out Property<ImmutableArray<Accessibility>> accessibilities,
        out Property<ImmutableArray<ModifierKind>> modifiers)
    {
        var key = $"dotnet_naming_rule.{namingRuleTitle}.symbols";
        if (!entries.TryGetValue(key, out var name))
        {
            specification = default;
            kinds = default;
            accessibilities = default;
            modifiers = default;
            return false;
        }

        specification = new Property<string>(key, name);

        const string group = "dotnet_naming_symbols";
        kinds = GetProperty(entries, group, name, "applicable_kinds", ParseSymbolKindList, s_allApplicableKinds);
        accessibilities = GetProperty(entries, group, name, "applicable_accessibilities", ParseAccessibilityKindList, s_allAccessibility);
        modifiers = GetProperty(entries, group, name, "required_modifiers", ParseModifiers, []);
        return true;
    }

    private static readonly SymbolKindOrTypeKind s_namespace = new(SymbolKind.Namespace);
    private static readonly SymbolKindOrTypeKind s_class = new(TypeKind.Class);
    private static readonly SymbolKindOrTypeKind s_struct = new(TypeKind.Struct);
    private static readonly SymbolKindOrTypeKind s_interface = new(TypeKind.Interface);
    private static readonly SymbolKindOrTypeKind s_enum = new(TypeKind.Enum);
    private static readonly SymbolKindOrTypeKind s_property = new(SymbolKind.Property);
    private static readonly SymbolKindOrTypeKind s_method = new(MethodKind.Ordinary);
    private static readonly SymbolKindOrTypeKind s_localFunction = new(MethodKind.LocalFunction);
    private static readonly SymbolKindOrTypeKind s_field = new(SymbolKind.Field);
    private static readonly SymbolKindOrTypeKind s_event = new(SymbolKind.Event);
    private static readonly SymbolKindOrTypeKind s_delegate = new(TypeKind.Delegate);
    private static readonly SymbolKindOrTypeKind s_parameter = new(SymbolKind.Parameter);
    private static readonly SymbolKindOrTypeKind s_typeParameter = new(SymbolKind.TypeParameter);
    private static readonly SymbolKindOrTypeKind s_local = new(SymbolKind.Local);
    private static readonly ImmutableArray<SymbolKindOrTypeKind> s_allApplicableKinds =
        [s_namespace, s_class, s_struct, s_interface, s_enum, s_property, s_method, s_localFunction, s_field, s_event, s_delegate, s_parameter, s_typeParameter, s_local];

    private static ImmutableArray<SymbolKindOrTypeKind> ParseSymbolKindList(string symbolSpecApplicableKinds)
    {
        if (symbolSpecApplicableKinds == null)
        {
            return [];
        }

        if (symbolSpecApplicableKinds.Trim() == "*")
        {
            return s_allApplicableKinds;
        }

        var builder = ArrayBuilder<SymbolKindOrTypeKind>.GetInstance();
        foreach (var symbolSpecApplicableKind in symbolSpecApplicableKinds.Split(',').Select(x => x.Trim()))
        {
            switch (symbolSpecApplicableKind)
            {
                case "class":
                    builder.Add(s_class);
                    break;
                case "struct":
                    builder.Add(s_struct);
                    break;
                case "interface":
                    builder.Add(s_interface);
                    break;
                case "enum":
                    builder.Add(s_enum);
                    break;
                case "property":
                    builder.Add(s_property);
                    break;
                case "method":
                    builder.Add(s_method);
                    break;
                case "local_function":
                    builder.Add(s_localFunction);
                    break;
                case "field":
                    builder.Add(s_field);
                    break;
                case "event":
                    builder.Add(s_event);
                    break;
                case "delegate":
                    builder.Add(s_delegate);
                    break;
                case "parameter":
                    builder.Add(s_parameter);
                    break;
                case "type_parameter":
                    builder.Add(s_typeParameter);
                    break;
                case "namespace":
                    builder.Add(s_namespace);
                    break;
                case "local":
                    builder.Add(s_local);
                    break;
                default:
                    break;
            }
        }

        return builder.ToImmutableAndFree();
    }

    private static readonly ImmutableArray<Accessibility> s_allAccessibility =
    [
        Accessibility.NotApplicable,
        Accessibility.Public,
        Accessibility.Internal,
        Accessibility.Private,
        Accessibility.Protected,
        Accessibility.ProtectedAndInternal,
        Accessibility.ProtectedOrInternal,
    ];

    private static ImmutableArray<Accessibility> ParseAccessibilityKindList(string symbolSpecApplicableAccessibilities)
    {
        if (symbolSpecApplicableAccessibilities == null)
        {
            return [];
        }

        if (symbolSpecApplicableAccessibilities.Trim() == "*")
        {
            return s_allAccessibility;
        }

        var builder = ArrayBuilder<Accessibility>.GetInstance();
        foreach (var symbolSpecApplicableAccessibility in symbolSpecApplicableAccessibilities.Split(',').Select(x => x.Trim()))
        {
            switch (symbolSpecApplicableAccessibility)
            {
                case "public":
                    builder.Add(Accessibility.Public);
                    break;
                case "internal":
                case "friend":
                    builder.Add(Accessibility.Internal);
                    break;
                case "private":
                    builder.Add(Accessibility.Private);
                    break;
                case "protected":
                    builder.Add(Accessibility.Protected);
                    break;
                case "protected_internal":
                case "protected_friend":
                    builder.Add(Accessibility.ProtectedOrInternal);
                    break;
                case "private_protected":
                    builder.Add(Accessibility.ProtectedAndInternal);
                    break;
                case "local":
                    builder.Add(Accessibility.NotApplicable);
                    break;
                default:
                    break;
            }
        }

        return builder.ToImmutableAndFree();
    }

    private static readonly ModifierKind s_abstractModifierKind = new(ModifierKindEnum.IsAbstract);
    private static readonly ModifierKind s_asyncModifierKind = new(ModifierKindEnum.IsAsync);
    private static readonly ModifierKind s_constModifierKind = new(ModifierKindEnum.IsConst);
    private static readonly ModifierKind s_readonlyModifierKind = new(ModifierKindEnum.IsReadOnly);
    private static readonly ModifierKind s_staticModifierKind = new(ModifierKindEnum.IsStatic);
    private static readonly ImmutableArray<ModifierKind> _allModifierKind = [s_abstractModifierKind, s_asyncModifierKind, s_constModifierKind, s_readonlyModifierKind, s_staticModifierKind];

    private static ImmutableArray<ModifierKind> ParseModifiers(string symbolSpecRequiredModifiers)
    {
        if (symbolSpecRequiredModifiers == null)
        {
            return [];
        }

        if (symbolSpecRequiredModifiers.Trim() == "*")
        {
            return _allModifierKind;
        }

        var builder = ArrayBuilder<ModifierKind>.GetInstance();
        foreach (var symbolSpecRequiredModifier in symbolSpecRequiredModifiers.Split(',').Select(x => x.Trim()))
        {
            switch (symbolSpecRequiredModifier)
            {
                case "abstract":
                case "must_inherit":
                    builder.Add(s_abstractModifierKind);
                    break;
                case "async":
                    builder.Add(s_asyncModifierKind);
                    break;
                case "const":
                    builder.Add(s_constModifierKind);
                    break;
                case "readonly":
                    builder.Add(s_readonlyModifierKind);
                    break;
                case "static":
                case "shared":
                    builder.Add(s_staticModifierKind);
                    break;
                default:
                    break;
            }
        }

        return builder.ToImmutableAndFree();
    }

    public static string ToEditorConfigString(this ImmutableArray<SymbolKindOrTypeKind> symbols)
    {
        if (symbols.IsDefaultOrEmpty)
        {
            return "";
        }

        if (s_allApplicableKinds.All(symbols.Contains) && symbols.All(s_allApplicableKinds.Contains))
        {
            return "*";
        }

        return string.Join(", ", symbols.Select(symbol => symbol.ToEditorConfigString()));
    }

    private static string ToEditorConfigString(this SymbolKindOrTypeKind symbol)
    {
        switch (symbol.MethodKind)
        {
            case MethodKind.Ordinary:
                return "method";

            case MethodKind.LocalFunction:
                return "local_function";

            case null:
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(symbol);
        }

        switch (symbol.TypeKind)
        {
            case TypeKind.Class:
                return "class";

            case TypeKind.Struct:
                return "struct";

            case TypeKind.Interface:
                return "interface";

            case TypeKind.Enum:
                return "enum";

            case TypeKind.Delegate:
                return "delegate";

            case TypeKind.Module:
                return "module";

            case TypeKind.Pointer:
                return "pointer";

            case TypeKind.TypeParameter:
                return "type_parameter";

            case null:
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(symbol);
        }

        switch (symbol.SymbolKind)
        {
            case SymbolKind.Namespace:
                return "namespace";

            case SymbolKind.Property:
                return "property";

            case SymbolKind.Field:
                return "field";

            case SymbolKind.Event:
                return "event";

            case SymbolKind.Parameter:
                return "parameter";

            case SymbolKind.TypeParameter:
                return "type_parameter";

            case SymbolKind.Local:
                return "local";

            case null:
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(symbol);
        }

        throw ExceptionUtilities.UnexpectedValue(symbol);
    }

    public static string ToEditorConfigString(this ImmutableArray<Accessibility> accessibilities, string languageName)
    {
        if (accessibilities.IsDefaultOrEmpty)
        {
            return "";
        }

        if (s_allAccessibility.All(accessibilities.Contains) && accessibilities.All(s_allAccessibility.Contains))
        {
            return "*";
        }

        return string.Join(", ", accessibilities.Select(accessibility => accessibility.ToEditorConfigString(languageName)));
    }

    private static string ToEditorConfigString(this Accessibility accessibility, string languageName)
    {
        switch (accessibility)
        {
            case Accessibility.NotApplicable:
                return "local";

            case Accessibility.Private:
                return "private";

            case Accessibility.ProtectedAndInternal:
                return "private_protected";

            case Accessibility.Protected:
                return "protected";

            case Accessibility.Internal:
                if (languageName == LanguageNames.VisualBasic)
                {
                    return "friend";
                }
                else
                {
                    return "internal";
                }

            case Accessibility.ProtectedOrInternal:
                if (languageName == LanguageNames.VisualBasic)
                {
                    return "protected_friend";
                }
                else
                {
                    return "protected_internal";
                }

            case Accessibility.Public:
                return "public";

            default:
                throw ExceptionUtilities.UnexpectedValue(accessibility);
        }
    }

    public static string ToEditorConfigString(this ImmutableArray<ModifierKind> modifiers, string languageName)
    {
        if (modifiers.IsDefaultOrEmpty)
        {
            return "";
        }

        if (_allModifierKind.All(modifiers.Contains) && modifiers.All(_allModifierKind.Contains))
        {
            return "*";
        }

        return string.Join(", ", modifiers.Select(modifier => modifier.ToEditorConfigString(languageName)));
    }

    private static string ToEditorConfigString(this ModifierKind modifier, string languageName)
    {
        switch (modifier.ModifierKindWrapper)
        {
            case ModifierKindEnum.IsAbstract:
                if (languageName == LanguageNames.VisualBasic)
                {
                    return "must_inherit";
                }
                else
                {
                    return "abstract";
                }

            case ModifierKindEnum.IsStatic:
                if (languageName == LanguageNames.VisualBasic)
                {
                    return "shared";
                }
                else
                {
                    return "static";
                }

            case ModifierKindEnum.IsAsync:
                return "async";

            case ModifierKindEnum.IsReadOnly:
                return "readonly";

            case ModifierKindEnum.IsConst:
                return "const";

            default:
                throw ExceptionUtilities.UnexpectedValue(modifier);
        }
    }
}
