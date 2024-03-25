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
    internal static bool TryGetSymbolSpec(
        Section section,
        string namingRuleTitle,
        IReadOnlyDictionary<string, (string value, TextLine? line)> properties,
        [NotNullWhen(true)] out ApplicableSymbolInfo? applicableSymbolInfo)
    {
        return TryGetSymbolSpec(
            namingRuleTitle,
            properties,
            s => (s.value, s.line),
            () => null,
            (nameTuple, kindsTuple, accessibilitiesTuple, modifiersTuple) =>
            {
                var (name, nameTextLine) = nameTuple;
                var (kinds, kindsTextLine) = kindsTuple;
                var (accessibilities, accessibilitiesTextLine) = accessibilitiesTuple;
                var (modifiers, modifiersTextLine) = modifiersTuple;
                return new ApplicableSymbolInfo(
                    OptionName: (section, nameTextLine?.Span, name),
                    SymbolKinds: (section, kindsTextLine?.Span, kinds),
                    Accessibilities: (section, accessibilitiesTextLine?.Span, accessibilities),
                    Modifiers: (section, modifiersTextLine?.Span, modifiers));
            },
            out applicableSymbolInfo);
    }

    private static bool TryGetSymbolSpec(
        string namingRuleTitle,
        IReadOnlyDictionary<string, string> conventionsDictionary,
        [NotNullWhen(true)] out SymbolSpecification? symbolSpec)
    {
        return TryGetSymbolSpec<string, object?, SymbolSpecification>(
            namingRuleTitle,
            conventionsDictionary,
            s => (s, null),
            () => null,
            (t0, t1, t2, t3) => new SymbolSpecification(
                    Guid.NewGuid(),
                    t0.name,
                    t1.kinds,
                    t2.accessibilities,
                    t3.modifiers),
            out symbolSpec);
    }

    private static bool TryGetSymbolSpec<T, TData, TResult>(
        string namingRuleTitle,
        IReadOnlyDictionary<string, T> conventionsDictionary,
        Func<T, (string value, TData data)> tupleSelector,
        Func<TData> defaultValue,
        Func<(string name, TData data),
             (ImmutableArray<SymbolKindOrTypeKind> kinds, TData data),
             (ImmutableArray<Accessibility> accessibilities, TData data),
             (ImmutableArray<ModifierKind> modifiers, TData data),
            TResult> constructor,
        [NotNullWhen(true)] out TResult? symbolSpec)
    {
        symbolSpec = default;
        if (!TryGetSymbolSpecNameForNamingRule(namingRuleTitle, conventionsDictionary, tupleSelector, out var symbolSpecName))
        {
            return false;
        }

        var applicableKinds = GetSymbolsApplicableKinds(symbolSpecName.name, conventionsDictionary, tupleSelector, defaultValue);
        var applicableAccessibilities = GetSymbolsApplicableAccessibilities(symbolSpecName.name, conventionsDictionary, tupleSelector, defaultValue);
        var requiredModifiers = GetSymbolsRequiredModifiers(symbolSpecName.name, conventionsDictionary, tupleSelector, defaultValue);

        symbolSpec = constructor(symbolSpecName, applicableKinds, applicableAccessibilities, requiredModifiers);
        return symbolSpec is not null;
    }

    private static bool TryGetSymbolSpecNameForNamingRule<T, TData>(
        string namingRuleName,
        IReadOnlyDictionary<string, T> conventionsDictionary,
        Func<T, (string symbolSpecName, TData data)> tupleSelector,
        out (string name, TData data) result)
    {
        if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.symbols", out var symbolSpecName))
        {
            result = tupleSelector(symbolSpecName);
            return result.name != null;
        }

        result = default;
        return false;
    }

    private static (ImmutableArray<SymbolKindOrTypeKind> kinds, TData data) GetSymbolsApplicableKinds<T, TData>(
        string symbolSpecName,
        IReadOnlyDictionary<string, T> conventionsDictionary,
        Func<T, (string value, TData data)> tupleSelector,
        Func<TData> defaultValue)
    {
        if (conventionsDictionary.TryGetValue($"dotnet_naming_symbols.{symbolSpecName}.applicable_kinds", out var result))
        {
            var (symbolSpecApplicableKinds, data) = tupleSelector(result);
            var kinds = ParseSymbolKindList(symbolSpecApplicableKinds ?? string.Empty);
            return (kinds, data);
        }

        return (_all, defaultValue());
    }

    private static readonly SymbolKindOrTypeKind _namespace = new(SymbolKind.Namespace);
    private static readonly SymbolKindOrTypeKind _class = new(TypeKind.Class);
    private static readonly SymbolKindOrTypeKind _struct = new(TypeKind.Struct);
    private static readonly SymbolKindOrTypeKind _interface = new(TypeKind.Interface);
    private static readonly SymbolKindOrTypeKind _enum = new(TypeKind.Enum);
    private static readonly SymbolKindOrTypeKind _property = new(SymbolKind.Property);
    private static readonly SymbolKindOrTypeKind _method = new(MethodKind.Ordinary);
    private static readonly SymbolKindOrTypeKind _localFunction = new(MethodKind.LocalFunction);
    private static readonly SymbolKindOrTypeKind _field = new(SymbolKind.Field);
    private static readonly SymbolKindOrTypeKind _event = new(SymbolKind.Event);
    private static readonly SymbolKindOrTypeKind _delegate = new(TypeKind.Delegate);
    private static readonly SymbolKindOrTypeKind _parameter = new(SymbolKind.Parameter);
    private static readonly SymbolKindOrTypeKind _typeParameter = new(SymbolKind.TypeParameter);
    private static readonly SymbolKindOrTypeKind _local = new(SymbolKind.Local);
    private static readonly ImmutableArray<SymbolKindOrTypeKind> _all =
        [_namespace, _class, _struct, _interface, _enum, _property, _method, _localFunction, _field, _event, _delegate, _parameter, _typeParameter, _local];

    private static ImmutableArray<SymbolKindOrTypeKind> ParseSymbolKindList(string symbolSpecApplicableKinds)
    {
        if (symbolSpecApplicableKinds == null)
        {
            return [];
        }

        if (symbolSpecApplicableKinds.Trim() == "*")
        {
            return _all;
        }

        var builder = ArrayBuilder<SymbolKindOrTypeKind>.GetInstance();
        foreach (var symbolSpecApplicableKind in symbolSpecApplicableKinds.Split(',').Select(x => x.Trim()))
        {
            switch (symbolSpecApplicableKind)
            {
                case "class":
                    builder.Add(_class);
                    break;
                case "struct":
                    builder.Add(_struct);
                    break;
                case "interface":
                    builder.Add(_interface);
                    break;
                case "enum":
                    builder.Add(_enum);
                    break;
                case "property":
                    builder.Add(_property);
                    break;
                case "method":
                    builder.Add(_method);
                    break;
                case "local_function":
                    builder.Add(_localFunction);
                    break;
                case "field":
                    builder.Add(_field);
                    break;
                case "event":
                    builder.Add(_event);
                    break;
                case "delegate":
                    builder.Add(_delegate);
                    break;
                case "parameter":
                    builder.Add(_parameter);
                    break;
                case "type_parameter":
                    builder.Add(_typeParameter);
                    break;
                case "namespace":
                    builder.Add(_namespace);
                    break;
                case "local":
                    builder.Add(_local);
                    break;
                default:
                    break;
            }
        }

        return builder.ToImmutableAndFree();
    }

    private static (ImmutableArray<Accessibility> accessibilities, TData data) GetSymbolsApplicableAccessibilities<T, TData>(
        string symbolSpecName,
        IReadOnlyDictionary<string, T> conventionsDictionary,
        Func<T, (string value, TData data)> tupleSelector,
        Func<TData> defaultValue)
    {
        if (conventionsDictionary.TryGetValue($"dotnet_naming_symbols.{symbolSpecName}.applicable_accessibilities", out var result))
        {
            var (symbolSpecApplicableAccessibilities, data) = tupleSelector(result);
            return (ParseAccessibilityKindList(symbolSpecApplicableAccessibilities ?? string.Empty), data);
        }

        return (s_allAccessibility, defaultValue());
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

    private static (ImmutableArray<ModifierKind> modifiers, TData data) GetSymbolsRequiredModifiers<T, TData>(
        string symbolSpecName,
        IReadOnlyDictionary<string, T> conventionsDictionary,
        Func<T, (string value, TData data)> tupleSelector,
        Func<TData> defaultValue)
    {
        if (conventionsDictionary.TryGetValue($"dotnet_naming_symbols.{symbolSpecName}.required_modifiers", out var result))
        {
            var (symbolSpecRequiredModifiers, data) = tupleSelector(result);
            return (ParseModifiers(symbolSpecRequiredModifiers ?? string.Empty), data);
        }

        return (ImmutableArray<ModifierKind>.Empty, defaultValue());
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

        if (_all.All(symbols.Contains) && symbols.All(_all.Contains))
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
