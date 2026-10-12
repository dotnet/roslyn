// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class NamingSettingsGraph(
    ImmutableArray<IReadOnlyDictionary<string, object>> symbolSpecifications,
    ImmutableArray<IReadOnlyDictionary<string, object>> namingStyles,
    ImmutableArray<IReadOnlyDictionary<string, object>> rules)
{
    public ImmutableArray<IReadOnlyDictionary<string, object>> SymbolSpecifications { get; } = symbolSpecifications;
    public ImmutableArray<IReadOnlyDictionary<string, object>> NamingStyles { get; } = namingStyles;
    public ImmutableArray<IReadOnlyDictionary<string, object>> Rules { get; } = rules;

    public bool ConfigurationEquals(NamingSettingsGraph other)
        => ToPreferences().Equals(other.ToPreferences())
        && IdentifiersEqual(SymbolSpecifications, other.SymbolSpecifications)
        && IdentifiersEqual(NamingStyles, other.NamingStyles);

    private static bool IdentifiersEqual(ImmutableArray<IReadOnlyDictionary<string, object>> left, ImmutableArray<IReadOnlyDictionary<string, object>> right)
        => left.Length == right.Length && left.Zip(right, (first, second) =>
            ReadId(first) == ReadId(second)).All(equal => equal);

    public static NamingSettingsGraph Default { get; } = CreateDefault();

    private static NamingSettingsGraph CreateDefault()
    {
        var graph = FromPreferences(NamingStylePreferences.Default);
        foreach (var item in graph.SymbolSpecifications.Concat(graph.NamingStyles))
        {
            var properties = (Dictionary<string, object>)item;
            properties["name"] = "";
            properties["defaultName"] = true;
        }

        return graph;
    }

    public static NamingSettingsGraph FromPreferences(NamingStylePreferences preferences, NamingSettingsGraph? previous = null)
    {
        var previousSpecifications = GetPreviousItems(previous?.SymbolSpecifications ?? [], "symbolSpecification");
        var previousStyles = GetPreviousItems(previous?.NamingStyles ?? [], "namingStyle");
        var specificationIds = ReserveIds(preferences.SymbolSpecifications.Select(item => item.ID), previousSpecifications, "symbolSpecification");
        var styleIds = ReserveIds(preferences.NamingStyles.Select(item => item.ID), previousStyles, "namingStyle");
        var specifications = ImmutableArray.CreateBuilder<IReadOnlyDictionary<string, object>>();
        var styles = ImmutableArray.CreateBuilder<IReadOnlyDictionary<string, object>>();
        var rules = ImmutableArray.CreateBuilder<IReadOnlyDictionary<string, object>>();
        var specificationMap = new Dictionary<Guid, string>();
        var styleMap = new Dictionary<Guid, string>();

        foreach (var specification in preferences.SymbolSpecifications)
        {
            var id = previousSpecifications.TryGetValue(specification.ID, out var existing)
                ? ReadId(existing) : NamingSettingsIdentity.TryGetBuiltInId(specification.ID, out var builtIn)
                    ? builtIn! : NamingSettingsIdentity.CreateId(Guid.Empty, specification.Name ?? "", specificationIds);
            specificationMap.Add(specification.ID, id);
            var item = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["id"] = id,
                ["name"] = specification.Name ?? "",
                ["symbolKinds"] = specification.ApplicableSymbolKindList.Select(SerializeKind).ToArray(),
                ["accessibilities"] = specification.ApplicableAccessibilityList.Select(SerializeAccessibility).ToArray(),
                ["requiredModifiers"] = specification.RequiredModifierList.Select(modifier => SerializeModifier(modifier.ModifierKindWrapper)).ToArray(),
            };
            PreserveDefaultName(item, existing, specification.Name ?? "", isStyle: false);
            specifications.Add(item);
        }

        foreach (var style in preferences.NamingStyles)
        {
            var id = previousStyles.TryGetValue(style.ID, out var existing)
                ? ReadId(existing) : NamingSettingsIdentity.TryGetBuiltInId(style.ID, out var builtIn)
                    ? builtIn! : NamingSettingsIdentity.CreateId(Guid.Empty, style.Name ?? "", styleIds);
            styleMap.Add(style.ID, id);
            var item = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["id"] = id,
                ["name"] = style.Name ?? "",
                ["prefix"] = style.Prefix,
                ["suffix"] = style.Suffix,
                ["wordSeparator"] = style.WordSeparator,
                ["capitalization"] = SerializeCapitalization(style.CapitalizationScheme),
            };
            PreserveDefaultName(item, existing, style.Name ?? "", isStyle: true);
            styles.Add(item);
        }

        foreach (var rule in preferences.Rules.NamingRules)
        {
            rules.Add(new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["symbolSpecificationId"] = specificationMap[rule.SymbolSpecification.ID],
                ["namingStyleId"] = styleMap[rule.NamingStyle.ID],
                ["severity"] = SerializeSeverity(rule.EnforcementLevel),
            });
        }

        return new NamingSettingsGraph(specifications.ToImmutable(), styles.ToImmutable(), rules.ToImmutable());
    }

    public NamingStylePreferences ToPreferences()
    {
        if (Rules.IsEmpty != SymbolSpecifications.IsEmpty || Rules.IsEmpty != NamingStyles.IsEmpty)
            throw new InvalidDataException("Naming rules, symbol specifications and naming styles must either all be empty or all be nonempty.");

        var specifications = ImmutableArray.CreateBuilder<SymbolSpecification>();
        var styles = ImmutableArray.CreateBuilder<NamingStyle>();
        var rules = ImmutableArray.CreateBuilder<NamingRule>();
        var specificationsById = new Dictionary<string, SymbolSpecification>(StringComparer.Ordinal);
        var stylesById = new Dictionary<string, NamingStyle>(StringComparer.Ordinal);
        var specificationGuids = new HashSet<Guid>();
        var styleGuids = new HashSet<Guid>();

        foreach (var item in SymbolSpecifications)
        {
            var id = ReadId(item);
            var legacyId = ReadLegacyId(item, "symbolSpecification");
            var specification = new SymbolSpecification(
                legacyId, ReadName(item, legacyId, isStyle: false),
                ReadStrings(item, "symbolKinds").Select(ParseKind).ToImmutableArray(),
                ReadStrings(item, "accessibilities").Select(ParseAccessibility).ToImmutableArray(),
                ReadStrings(item, "requiredModifiers").Select(token => new SymbolSpecification.ModifierKind(ParseModifier(token))).ToImmutableArray());
            if (specificationsById.ContainsKey(id) || !specificationGuids.Add(legacyId))
                throw new InvalidDataException("Symbol specification identities must be unique.");
            specificationsById.Add(id, specification);
            specifications.Add(specification);
        }

        foreach (var item in NamingStyles)
        {
            var id = ReadId(item);
            var legacyId = ReadLegacyId(item, "namingStyle");
            var style = new NamingStyle(
                legacyId, ReadName(item, legacyId, isStyle: true), ReadString(item, "prefix"), ReadString(item, "suffix"),
                ReadString(item, "wordSeparator"), ParseCapitalization(ReadString(item, "capitalization")));
            if (stylesById.ContainsKey(id) || !styleGuids.Add(legacyId))
                throw new InvalidDataException("Naming style identities must be unique.");
            stylesById.Add(id, style);
            styles.Add(style);
        }

        foreach (var item in Rules)
        {
            if (!specificationsById.TryGetValue(ReadString(item, "symbolSpecificationId"), out var specification)
                || !stylesById.TryGetValue(ReadString(item, "namingStyleId"), out var style))
            {
                throw new InvalidDataException("A naming rule refers to an unknown symbol specification or naming style.");
            }

            rules.Add(new NamingRule(specification, style, ParseSeverity(ReadString(item, "severity"))));
        }

        return new NamingStylePreferences(specifications.ToImmutable(), styles.ToImmutable(), rules.ToImmutable());
    }

    private static string ReadName(IReadOnlyDictionary<string, object> item, Guid id, bool isStyle)
    {
        var name = ReadString(item, "name");
        if (!item.TryGetValue("defaultName", out var defaultName))
            return name;

        if (defaultName is not bool useDefault)
            throw new InvalidDataException("Naming defaultName must be a Boolean.");

        if (!useDefault)
            return name;

        var localizedName = GetDefaultName(id, isStyle);
        if (name.Length != 0 || localizedName is null)
            throw new InvalidDataException("Only built-in naming definitions with an empty name can use defaultName.");

        return localizedName;
    }

    public NamingSettingsGraph NormalizeDefaultNames()
    {
        _ = ToPreferences();
        return new(
            SymbolSpecifications.Select(item => NormalizeDefinition(item, isStyle: false)).ToImmutableArray(),
            NamingStyles.Select(item => NormalizeDefinition(item, isStyle: true)).ToImmutableArray(),
            Rules);
    }

    internal static IReadOnlyDictionary<string, object> NormalizeDefinition(IReadOnlyDictionary<string, object> item, bool isStyle)
    {
        var normalized = MaterializeDefinition(item, isStyle).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var id = NamingSettingsIdentity.GetLegacyId(ReadId(item), isStyle ? "namingStyle" : "symbolSpecification");
        if ((string)normalized["name"] == GetDefaultName(id, isStyle))
        {
            normalized["name"] = "";
            normalized["defaultName"] = true;
        }
        return normalized;
    }

    internal static IReadOnlyDictionary<string, object> MaterializeDefinition(IReadOnlyDictionary<string, object> item, bool isStyle)
    {
        var name = ReadName(item, ReadLegacyId(item, isStyle ? "namingStyle" : "symbolSpecification"), isStyle);
        var result = item.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        result["name"] = name;
        result.Remove("legacyId");
        result.Remove("defaultName");
        return result;
    }

    internal static void ValidateDefinitionProperty(IReadOnlyDictionary<string, object> item, string property, bool isStyle)
    {
        switch (property)
        {
            case "name":
                if (string.IsNullOrWhiteSpace(ReadString(item, property)))
                    throw new InvalidDataException(ServicesVSResources.Enter_a_title_for_this_Naming_Style);
                break;
            case "prefix" or "suffix" or "wordSeparator" when isStyle:
                _ = ReadString(item, property);
                break;
            case "capitalization" when isStyle:
                _ = ParseCapitalization(ReadString(item, property));
                break;
            case "symbolKinds" when !isStyle:
                foreach (var token in ReadStrings(item, property))
                    _ = ParseKind(token);
                break;
            case "accessibilities" when !isStyle:
                foreach (var token in ReadStrings(item, property))
                    _ = ParseAccessibility(token);
                break;
            case "requiredModifiers" when !isStyle:
                foreach (var token in ReadStrings(item, property))
                    _ = ParseModifier(token);
                break;
            default:
                throw new InvalidDataException($"Unknown naming definition property '{property}'.");
        }
    }

    private static string? GetDefaultName(Guid id, bool isStyle)
        => isStyle
            ? NamingStylePreferences.Default.NamingStyles.FirstOrDefault(style => style.ID == id).Name
            : NamingStylePreferences.Default.SymbolSpecifications.FirstOrDefault(specification => specification.ID == id)?.Name;

    private static void PreserveDefaultName(Dictionary<string, object> item, IReadOnlyDictionary<string, object>? previous, string name, bool isStyle)
    {
        var id = NamingSettingsIdentity.GetLegacyId(ReadId(item), isStyle ? "namingStyle" : "symbolSpecification");
        if (previous is not null && previous.TryGetValue("defaultName", out var defaultName) && defaultName is true
            && name == GetDefaultName(id, isStyle))
        {
            item["name"] = "";
            item["defaultName"] = true;
        }
    }

    private static Dictionary<Guid, IReadOnlyDictionary<string, object>> GetPreviousItems(ImmutableArray<IReadOnlyDictionary<string, object>> items, string identityKind)
        => items.ToDictionary(item => ReadLegacyId(item, identityKind));

    private static HashSet<string> ReserveIds(IEnumerable<Guid> legacyIds, Dictionary<Guid, IReadOnlyDictionary<string, object>> previousItems, string identityKind)
    {
        var result = new HashSet<string>(NamingSettingsIdentity.GetBuiltInIds(identityKind), StringComparer.Ordinal);
        foreach (var legacyId in legacyIds)
        {
            // Reserve surviving identities, not deleted ones, so repeated old imports do not acquire new suffixes.
            if (previousItems.TryGetValue(legacyId, out var previous))
                result.Add(ReadId(previous));
        }

        return result;
    }

    private static Guid ReadLegacyId(IReadOnlyDictionary<string, object> item, string identityKind)
    {
        if (item.ContainsKey("legacyId"))
            return ReadLegacyId(item);

        return NamingSettingsIdentity.GetLegacyId(ReadId(item), identityKind);
    }

    private static string ReadId(IReadOnlyDictionary<string, object> item)
    {
        var id = ReadString(item, "id");
        if (!NamingSettingsIdentity.IsValidId(id))
            throw new InvalidDataException("Naming IDs must start with a lowercase ASCII letter and contain only lowercase letters, digits or underscores.");
        return id;
    }

    private static Guid ReadLegacyId(IReadOnlyDictionary<string, object> item)
    {
        if (!Guid.TryParse(ReadString(item, "legacyId"), out var id) || id == Guid.Empty)
            throw new InvalidDataException("Naming compatibility identities must be nonempty GUIDs.");
        return id;
    }

    internal static string ReadString(IReadOnlyDictionary<string, object> item, string property)
        => item.TryGetValue(property, out var value) && value is string text
            ? text : throw new InvalidDataException($"Naming property '{property}' must be a string.");

    internal static ImmutableArray<string> ReadStrings(IReadOnlyDictionary<string, object> item, string property)
    {
        if (!item.TryGetValue(property, out var value) || value is not IEnumerable sequence || value is string)
            throw new InvalidDataException($"Naming property '{property}' must be an array.");
        var result = ImmutableArray.CreateBuilder<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in sequence)
        {
            if (element is not string text || !seen.Add(text))
                throw new InvalidDataException($"Naming property '{property}' must contain unique strings.");
            result.Add(text);
        }
        return result.ToImmutable();
    }

    internal static string SerializeKind(SymbolSpecification.SymbolKindOrTypeKind kind)
        => kind.SymbolKind is SymbolKind.Namespace ? "namespace"
            : kind.SymbolKind is SymbolKind.Property ? "property"
            : kind.SymbolKind is SymbolKind.Field ? "field"
            : kind.SymbolKind is SymbolKind.Event ? "event"
            : kind.SymbolKind is SymbolKind.Parameter ? "parameter"
            : kind.SymbolKind is SymbolKind.TypeParameter ? "typeParameter"
            : kind.SymbolKind is SymbolKind.Local ? "local"
            : kind.TypeKind is TypeKind.Class ? "class"
            : kind.TypeKind is TypeKind.Struct ? "struct"
            : kind.TypeKind is TypeKind.Interface ? "interface"
            : kind.TypeKind is TypeKind.Delegate ? "delegate"
            : kind.TypeKind is TypeKind.Enum ? "enum"
            : kind.TypeKind is TypeKind.Module ? "module"
            : kind.TypeKind is TypeKind.Pointer ? "pointer"
            : kind.MethodKind is MethodKind.Ordinary ? "method"
            : kind.MethodKind is MethodKind.LocalFunction ? "localFunction"
            : kind.SymbolKind is { } symbol ? "symbol:" + symbol
            : kind.TypeKind is { } type ? "type:" + type
            : kind.MethodKind is { } method ? "method:" + method
            : throw new InvalidDataException("Unknown symbol kind.");

    private static SymbolSpecification.SymbolKindOrTypeKind ParseKind(string token)
    {
        var knownKind = token switch
        {
            "namespace" => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Namespace),
            "property" => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Property),
            "field" => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
            "event" => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Event),
            "parameter" => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter),
            "typeParameter" => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.TypeParameter),
            "local" => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local),
            "class" => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class),
            "struct" => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Struct),
            "interface" => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Interface),
            "delegate" => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Delegate),
            "enum" => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Enum),
            "module" => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Module),
            "pointer" => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Pointer),
            "method" => new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary),
            "localFunction" => new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction),
            _ => (SymbolSpecification.SymbolKindOrTypeKind?)null,
        };
        if (knownKind is { } kind)
            return kind;

        if (token.StartsWith("symbol:", StringComparison.Ordinal))
            return new SymbolSpecification.SymbolKindOrTypeKind(ParseEnum<SymbolKind>(token.Substring(7)));
        if (token.StartsWith("type:", StringComparison.Ordinal))
            return new SymbolSpecification.SymbolKindOrTypeKind(ParseEnum<TypeKind>(token.Substring(5)));
        if (token.StartsWith("method:", StringComparison.Ordinal))
            return new SymbolSpecification.SymbolKindOrTypeKind(ParseEnum<MethodKind>(token.Substring(7)));
        throw new InvalidDataException($"Unknown symbol kind '{token}'.");
    }

    private static T ParseEnum<T>(string token) where T : struct, Enum
        => Enum.TryParse<T>(token, out var value) && Enum.IsDefined(typeof(T), value)
            ? value : throw new InvalidDataException($"Unknown {typeof(T).Name} value '{token}'.");

    private static string SerializeAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.NotApplicable => "local",
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedAndInternal => "privateProtected",
        Accessibility.ProtectedOrInternal => "protectedInternal",
        _ => throw new InvalidDataException($"Unknown accessibility '{accessibility}'."),
    };

    private static Accessibility ParseAccessibility(string token) => token switch
    {
        "local" => Accessibility.NotApplicable,
        "public" => Accessibility.Public,
        "private" => Accessibility.Private,
        "internal" => Accessibility.Internal,
        "protected" => Accessibility.Protected,
        "privateProtected" => Accessibility.ProtectedAndInternal,
        "protectedInternal" => Accessibility.ProtectedOrInternal,
        _ => throw new InvalidDataException($"Unknown accessibility '{token}'."),
    };

    private static string SerializeModifier(SymbolSpecification.ModifierKindEnum modifier) => modifier switch
    {
        SymbolSpecification.ModifierKindEnum.IsAbstract => "abstract",
        SymbolSpecification.ModifierKindEnum.IsStatic => "static",
        SymbolSpecification.ModifierKindEnum.IsAsync => "async",
        SymbolSpecification.ModifierKindEnum.IsReadOnly => "readonly",
        SymbolSpecification.ModifierKindEnum.IsConst => "const",
        _ => throw new InvalidDataException($"Unknown modifier '{modifier}'."),
    };

    private static SymbolSpecification.ModifierKindEnum ParseModifier(string token) => token switch
    {
        "abstract" => SymbolSpecification.ModifierKindEnum.IsAbstract,
        "static" => SymbolSpecification.ModifierKindEnum.IsStatic,
        "async" => SymbolSpecification.ModifierKindEnum.IsAsync,
        "readonly" => SymbolSpecification.ModifierKindEnum.IsReadOnly,
        "const" => SymbolSpecification.ModifierKindEnum.IsConst,
        _ => throw new InvalidDataException($"Unknown modifier '{token}'."),
    };

    private static string SerializeCapitalization(Capitalization capitalization) => capitalization switch
    {
        Capitalization.PascalCase => "pascalCase",
        Capitalization.CamelCase => "camelCase",
        Capitalization.FirstUpper => "firstUpper",
        Capitalization.AllUpper => "allUpper",
        Capitalization.AllLower => "allLower",
        _ => throw new InvalidDataException($"Unknown capitalization '{capitalization}'."),
    };

    private static Capitalization ParseCapitalization(string token) => token switch
    {
        "pascalCase" => Capitalization.PascalCase,
        "camelCase" => Capitalization.CamelCase,
        "firstUpper" => Capitalization.FirstUpper,
        "allUpper" => Capitalization.AllUpper,
        "allLower" => Capitalization.AllLower,
        _ => throw new InvalidDataException($"Unknown capitalization '{token}'."),
    };

    private static string SerializeSeverity(ReportDiagnostic severity) => severity switch
    {
        ReportDiagnostic.Hidden => "silent",
        ReportDiagnostic.Info => "suggestion",
        ReportDiagnostic.Warn => "warning",
        ReportDiagnostic.Error => "error",
        _ => throw new InvalidDataException($"Naming severity '{severity}' cannot be preserved by legacy naming storage."),
    };

    private static ReportDiagnostic ParseSeverity(string token) => token switch
    {
        "silent" => ReportDiagnostic.Hidden,
        "suggestion" => ReportDiagnostic.Info,
        "warning" => ReportDiagnostic.Warn,
        "error" => ReportDiagnostic.Error,
        _ => throw new InvalidDataException($"Unknown naming severity '{token}'."),
    };
}
