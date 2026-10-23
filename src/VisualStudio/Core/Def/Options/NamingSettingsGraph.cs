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
using static Microsoft.VisualStudio.LanguageServices.Options.NamingSettingsConstants;

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
            properties[Properties.Name] = "";
            properties[Properties.DefaultName] = true;
        }

        return graph;
    }

    public static NamingSettingsGraph FromPreferences(NamingStylePreferences preferences, NamingSettingsGraph? previous = null)
    {
        var previousSpecifications = GetPreviousItems(previous?.SymbolSpecifications ?? [], IdentityKinds.SymbolSpecification);
        var previousStyles = GetPreviousItems(previous?.NamingStyles ?? [], IdentityKinds.NamingStyle);
        var specificationIds = ReserveIds(preferences.SymbolSpecifications.Select(item => item.ID), previousSpecifications, IdentityKinds.SymbolSpecification);
        var styleIds = ReserveIds(preferences.NamingStyles.Select(item => item.ID), previousStyles, IdentityKinds.NamingStyle);
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
                [Properties.Id] = id,
                [Properties.Name] = specification.Name ?? "",
                [Properties.SymbolKinds] = specification.ApplicableSymbolKindList.Select(SerializeKind).ToArray(),
                [Properties.Accessibilities] = specification.ApplicableAccessibilityList.Select(SerializeAccessibility).ToArray(),
                [Properties.RequiredModifiers] = specification.RequiredModifierList.Select(modifier => SerializeModifier(modifier.ModifierKindWrapper)).ToArray(),
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
                [Properties.Id] = id,
                [Properties.Name] = style.Name ?? "",
                [Properties.Prefix] = style.Prefix,
                [Properties.Suffix] = style.Suffix,
                [Properties.WordSeparator] = style.WordSeparator,
                [Properties.Capitalization] = SerializeCapitalization(style.CapitalizationScheme),
            };
            PreserveDefaultName(item, existing, style.Name ?? "", isStyle: true);
            styles.Add(item);
        }

        foreach (var rule in preferences.Rules.NamingRules)
        {
            rules.Add(new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [Properties.SymbolSpecificationId] = specificationMap[rule.SymbolSpecification.ID],
                [Properties.NamingStyleId] = styleMap[rule.NamingStyle.ID],
                [Properties.Severity] = SerializeSeverity(rule.EnforcementLevel),
            });
        }

        return new NamingSettingsGraph(specifications.ToImmutable(), styles.ToImmutable(), rules.ToImmutable());
    }

    public NamingStylePreferences ToPreferences()
    {
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
            var legacyId = ReadLegacyId(item, IdentityKinds.SymbolSpecification);
            var specification = new SymbolSpecification(
                legacyId, ReadName(item, legacyId, isStyle: false),
                ReadStrings(item, Properties.SymbolKinds).Select(ParseKind).ToImmutableArray(),
                ReadStrings(item, Properties.Accessibilities).Select(ParseAccessibility).ToImmutableArray(),
                ReadStrings(item, Properties.RequiredModifiers).Select(token => new SymbolSpecification.ModifierKind(ParseModifier(token))).ToImmutableArray());
            if (specificationsById.ContainsKey(id) || !specificationGuids.Add(legacyId))
                throw new InvalidDataException(ServicesVSResources.Symbol_specification_identities_must_be_unique);
            specificationsById.Add(id, specification);
            specifications.Add(specification);
        }

        foreach (var item in NamingStyles)
        {
            var id = ReadId(item);
            var legacyId = ReadLegacyId(item, IdentityKinds.NamingStyle);
            var style = new NamingStyle(
                legacyId, ReadName(item, legacyId, isStyle: true), ReadString(item, Properties.Prefix), ReadString(item, Properties.Suffix),
                ReadString(item, Properties.WordSeparator), ParseCapitalization(ReadString(item, Properties.Capitalization)));
            if (stylesById.ContainsKey(id) || !styleGuids.Add(legacyId))
                throw new InvalidDataException(ServicesVSResources.Naming_style_identities_must_be_unique);
            stylesById.Add(id, style);
            styles.Add(style);
        }

        foreach (var item in Rules)
        {
            if (!specificationsById.TryGetValue(ReadString(item, Properties.SymbolSpecificationId), out var specification)
                || !stylesById.TryGetValue(ReadString(item, Properties.NamingStyleId), out var style))
            {
                throw new InvalidDataException(ServicesVSResources.A_naming_rule_refers_to_an_unknown_symbol_specification_or_naming_style);
            }

            rules.Add(new NamingRule(specification, style, ParseSeverity(ReadString(item, Properties.Severity))));
        }

        return new NamingStylePreferences(specifications.ToImmutable(), styles.ToImmutable(), rules.ToImmutable());
    }

    private static string ReadName(IReadOnlyDictionary<string, object> item, Guid id, bool isStyle)
    {
        var name = ReadString(item, Properties.Name);
        if (!item.TryGetValue(Properties.DefaultName, out var defaultName))
            return name;

        if (defaultName is not bool useDefault)
            throw new InvalidDataException(ServicesVSResources.Naming_defaultName_must_be_a_Boolean);

        if (!useDefault)
            return name;

        var localizedName = GetDefaultName(id, isStyle);
        if (name.Length != 0 || localizedName is null)
            throw new InvalidDataException(ServicesVSResources.Only_built_in_naming_definitions_with_an_empty_name_can_use_defaultName);

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
        var id = NamingSettingsIdentity.GetLegacyId(ReadId(item), isStyle ? IdentityKinds.NamingStyle : IdentityKinds.SymbolSpecification);
        if ((string)normalized[Properties.Name] == GetDefaultName(id, isStyle))
        {
            normalized[Properties.Name] = "";
            normalized[Properties.DefaultName] = true;
        }
        return normalized;
    }

    internal static IReadOnlyDictionary<string, object> MaterializeDefinition(IReadOnlyDictionary<string, object> item, bool isStyle)
    {
        var name = ReadName(item, ReadLegacyId(item, isStyle ? IdentityKinds.NamingStyle : IdentityKinds.SymbolSpecification), isStyle);
        var result = item.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        result[Properties.Name] = name;
        result.Remove(Properties.LegacyId);
        result.Remove(Properties.DefaultName);
        return result;
    }

    internal static void ValidateDefinitionProperty(IReadOnlyDictionary<string, object> item, string property, bool isStyle)
    {
        switch (property)
        {
            case Properties.Name:
                if (string.IsNullOrWhiteSpace(ReadString(item, property)))
                    throw new InvalidDataException(ServicesVSResources.Enter_a_name);
                break;
            case Properties.Prefix or Properties.Suffix or Properties.WordSeparator when isStyle:
                _ = ReadString(item, property);
                break;
            case Properties.Capitalization when isStyle:
                _ = ParseCapitalization(ReadString(item, property));
                break;
            case Properties.SymbolKinds when !isStyle:
                foreach (var token in ReadStrings(item, property))
                    _ = ParseKind(token);
                break;
            case Properties.Accessibilities when !isStyle:
                foreach (var token in ReadStrings(item, property))
                    _ = ParseAccessibility(token);
                break;
            case Properties.RequiredModifiers when !isStyle:
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
        var id = NamingSettingsIdentity.GetLegacyId(ReadId(item), isStyle ? IdentityKinds.NamingStyle : IdentityKinds.SymbolSpecification);
        if (previous is not null && previous.TryGetValue(Properties.DefaultName, out var defaultName) && defaultName is true
            && name == GetDefaultName(id, isStyle))
        {
            item[Properties.Name] = "";
            item[Properties.DefaultName] = true;
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
        // Reverse migration fills omitted compatibility IDs with their registered empty-string default.
        if (item.ContainsKey(Properties.LegacyId) && ReadString(item, Properties.LegacyId).Length != 0)
            return ReadLegacyId(item);

        return NamingSettingsIdentity.GetLegacyId(ReadId(item), identityKind);
    }

    private static string ReadId(IReadOnlyDictionary<string, object> item)
    {
        var id = ReadString(item, Properties.Id);
        if (!NamingSettingsIdentity.IsValidId(id))
            throw new InvalidDataException(ServicesVSResources.Invalid_naming_identity);
        return id;
    }

    private static Guid ReadLegacyId(IReadOnlyDictionary<string, object> item)
    {
        if (!Guid.TryParse(ReadString(item, Properties.LegacyId), out var id) || id == Guid.Empty)
            throw new InvalidDataException(ServicesVSResources.Naming_compatibility_identities_must_be_nonempty_GUIDs);
        return id;
    }

    internal static string ReadString(IReadOnlyDictionary<string, object> item, string property)
        => item.TryGetValue(property, out var value) && value is string text
            ? text : throw new InvalidDataException(string.Format(ServicesVSResources.Naming_property_0_must_be_a_string, property));

    internal static ImmutableArray<string> ReadStrings(IReadOnlyDictionary<string, object> item, string property)
    {
        if (!item.TryGetValue(property, out var value) || value is not IEnumerable sequence || value is string)
            throw new InvalidDataException(string.Format(ServicesVSResources.Naming_property_0_must_be_an_array, property));
        var result = ImmutableArray.CreateBuilder<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in sequence)
        {
            if (element is not string text || !seen.Add(text))
                throw new InvalidDataException(string.Format(ServicesVSResources.Naming_property_0_must_contain_unique_strings, property));
            result.Add(text);
        }
        return result.ToImmutable();
    }

    internal static string SerializeKind(SymbolSpecification.SymbolKindOrTypeKind kind)
        => kind.SymbolKind is SymbolKind.Namespace ? SymbolKinds.Namespace
            : kind.SymbolKind is SymbolKind.Property ? SymbolKinds.Property
            : kind.SymbolKind is SymbolKind.Field ? SymbolKinds.Field
            : kind.SymbolKind is SymbolKind.Event ? SymbolKinds.Event
            : kind.SymbolKind is SymbolKind.Parameter ? SymbolKinds.Parameter
            : kind.SymbolKind is SymbolKind.TypeParameter ? SymbolKinds.TypeParameter
            : kind.SymbolKind is SymbolKind.Local ? SymbolKinds.Local
            : kind.TypeKind is TypeKind.Class ? SymbolKinds.Class
            : kind.TypeKind is TypeKind.Struct ? SymbolKinds.Struct
            : kind.TypeKind is TypeKind.Interface ? SymbolKinds.Interface
            : kind.TypeKind is TypeKind.Delegate ? SymbolKinds.Delegate
            : kind.TypeKind is TypeKind.Enum ? SymbolKinds.Enum
            : kind.TypeKind is TypeKind.Module ? SymbolKinds.Module
            : kind.TypeKind is TypeKind.Pointer ? SymbolKinds.Pointer
            : kind.MethodKind is MethodKind.Ordinary ? SymbolKinds.Method
            : kind.MethodKind is MethodKind.LocalFunction ? SymbolKinds.LocalFunction
            : kind.SymbolKind is { } symbol ? SymbolKinds.SymbolPrefix + symbol
            : kind.TypeKind is { } type ? SymbolKinds.TypePrefix + type
            : kind.MethodKind is { } method ? SymbolKinds.MethodPrefix + method
            : throw new InvalidDataException("Unknown symbol kind.");

    private static SymbolSpecification.SymbolKindOrTypeKind ParseKind(string token)
    {
        var knownKind = token switch
        {
            SymbolKinds.Namespace => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Namespace),
            SymbolKinds.Property => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Property),
            SymbolKinds.Field => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
            SymbolKinds.Event => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Event),
            SymbolKinds.Parameter => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter),
            SymbolKinds.TypeParameter => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.TypeParameter),
            SymbolKinds.Local => new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local),
            SymbolKinds.Class => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class),
            SymbolKinds.Struct => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Struct),
            SymbolKinds.Interface => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Interface),
            SymbolKinds.Delegate => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Delegate),
            SymbolKinds.Enum => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Enum),
            SymbolKinds.Module => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Module),
            SymbolKinds.Pointer => new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Pointer),
            SymbolKinds.Method => new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary),
            SymbolKinds.LocalFunction => new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction),
            _ => (SymbolSpecification.SymbolKindOrTypeKind?)null,
        };
        if (knownKind is { } kind)
            return kind;

        if (token.StartsWith(SymbolKinds.SymbolPrefix, StringComparison.Ordinal))
            return new SymbolSpecification.SymbolKindOrTypeKind(ParseEnum<SymbolKind>(token.Substring(SymbolKinds.SymbolPrefix.Length)));
        if (token.StartsWith(SymbolKinds.TypePrefix, StringComparison.Ordinal))
            return new SymbolSpecification.SymbolKindOrTypeKind(ParseEnum<TypeKind>(token.Substring(SymbolKinds.TypePrefix.Length)));
        if (token.StartsWith(SymbolKinds.MethodPrefix, StringComparison.Ordinal))
            return new SymbolSpecification.SymbolKindOrTypeKind(ParseEnum<MethodKind>(token.Substring(SymbolKinds.MethodPrefix.Length)));
        throw new InvalidDataException(string.Format(ServicesVSResources.Unsupported_naming_setting_value_0, token));
    }

    private static T ParseEnum<T>(string token) where T : struct, Enum
        => Enum.TryParse<T>(token, out var value) && Enum.IsDefined(typeof(T), value)
            ? value : throw new InvalidDataException(string.Format(ServicesVSResources.Unsupported_naming_setting_value_0, token));

    private static string SerializeAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.NotApplicable => Accessibilities.Local,
        Accessibility.Public => Accessibilities.Public,
        Accessibility.Private => Accessibilities.Private,
        Accessibility.Internal => Accessibilities.Internal,
        Accessibility.Protected => Accessibilities.Protected,
        Accessibility.ProtectedAndInternal => Accessibilities.PrivateProtected,
        Accessibility.ProtectedOrInternal => Accessibilities.ProtectedInternal,
        _ => throw new InvalidDataException($"Unknown accessibility '{accessibility}'."),
    };

    private static Accessibility ParseAccessibility(string token) => token switch
    {
        Accessibilities.Local => Accessibility.NotApplicable,
        Accessibilities.Public => Accessibility.Public,
        Accessibilities.Private => Accessibility.Private,
        Accessibilities.Internal => Accessibility.Internal,
        Accessibilities.Protected => Accessibility.Protected,
        Accessibilities.PrivateProtected => Accessibility.ProtectedAndInternal,
        Accessibilities.ProtectedInternal => Accessibility.ProtectedOrInternal,
        _ => throw new InvalidDataException(string.Format(ServicesVSResources.Unsupported_naming_setting_value_0, token)),
    };

    private static string SerializeModifier(SymbolSpecification.ModifierKindEnum modifier) => modifier switch
    {
        SymbolSpecification.ModifierKindEnum.IsAbstract => Modifiers.Abstract,
        SymbolSpecification.ModifierKindEnum.IsStatic => Modifiers.Static,
        SymbolSpecification.ModifierKindEnum.IsAsync => Modifiers.Async,
        SymbolSpecification.ModifierKindEnum.IsReadOnly => Modifiers.ReadOnly,
        SymbolSpecification.ModifierKindEnum.IsConst => Modifiers.Const,
        _ => throw new InvalidDataException($"Unknown modifier '{modifier}'."),
    };

    private static SymbolSpecification.ModifierKindEnum ParseModifier(string token) => token switch
    {
        Modifiers.Abstract => SymbolSpecification.ModifierKindEnum.IsAbstract,
        Modifiers.Static => SymbolSpecification.ModifierKindEnum.IsStatic,
        Modifiers.Async => SymbolSpecification.ModifierKindEnum.IsAsync,
        Modifiers.ReadOnly => SymbolSpecification.ModifierKindEnum.IsReadOnly,
        Modifiers.Const => SymbolSpecification.ModifierKindEnum.IsConst,
        _ => throw new InvalidDataException(string.Format(ServicesVSResources.Unsupported_naming_setting_value_0, token)),
    };

    private static string SerializeCapitalization(Capitalization capitalization) => capitalization switch
    {
        Capitalization.PascalCase => Capitalizations.PascalCase,
        Capitalization.CamelCase => Capitalizations.CamelCase,
        Capitalization.FirstUpper => Capitalizations.FirstUpper,
        Capitalization.AllUpper => Capitalizations.AllUpper,
        Capitalization.AllLower => Capitalizations.AllLower,
        _ => throw new InvalidDataException($"Unknown capitalization '{capitalization}'."),
    };

    private static Capitalization ParseCapitalization(string token) => token switch
    {
        Capitalizations.PascalCase => Capitalization.PascalCase,
        Capitalizations.CamelCase => Capitalization.CamelCase,
        Capitalizations.FirstUpper => Capitalization.FirstUpper,
        Capitalizations.AllUpper => Capitalization.AllUpper,
        Capitalizations.AllLower => Capitalization.AllLower,
        _ => throw new InvalidDataException(string.Format(ServicesVSResources.Unsupported_naming_setting_value_0, token)),
    };

    private static string SerializeSeverity(ReportDiagnostic severity) => severity switch
    {
        ReportDiagnostic.Hidden => Severities.Silent,
        ReportDiagnostic.Info => Severities.Suggestion,
        ReportDiagnostic.Warn => Severities.Warning,
        ReportDiagnostic.Error => Severities.Error,
        _ => throw new InvalidDataException($"Naming severity '{severity}' cannot be preserved by legacy naming storage."),
    };

    private static ReportDiagnostic ParseSeverity(string token) => token switch
    {
        Severities.Silent => ReportDiagnostic.Hidden,
        Severities.Suggestion => ReportDiagnostic.Info,
        Severities.Warning => ReportDiagnostic.Warn,
        Severities.Error => ReportDiagnostic.Error,
        _ => throw new InvalidDataException(string.Format(ServicesVSResources.Unsupported_naming_setting_value_0, token)),
    };
}
