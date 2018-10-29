// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        private static bool TryGetSymbolSpec(
            string namingRuleTitle,
            IReadOnlyDictionary<string, object> conventionsDictionary,
            out SymbolSpecification symbolSpec)
        {
            symbolSpec = null;
            if (!TryGetSymbolSpecNameForNamingRule(namingRuleTitle, conventionsDictionary, out string symbolSpecName))
            {
                return false;
            }

            var applicableKinds = GetSymbolsApplicableKinds(symbolSpecName, conventionsDictionary);
            var applicableAccessibilities = GetSymbolsApplicableAccessibilities(symbolSpecName, conventionsDictionary);
            var requiredModifiers = GetSymbolsRequiredModifiers(symbolSpecName, conventionsDictionary);

            symbolSpec = new SymbolSpecification(
                null,
                symbolSpecName,
                symbolKindList: applicableKinds,
                accessibilityList: applicableAccessibilities,
                modifiers: requiredModifiers);
            return true;
        }

        private static bool TryGetSymbolSpecNameForNamingRule(
            string namingRuleName,
            IReadOnlyDictionary<string, object> conventionsDictionary,
            out string symbolSpecName)
        {
            symbolSpecName = null;
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.symbols", out object result))
            {
                symbolSpecName = result as string;
                return symbolSpecName != null;
            }

            return false;
        }

        private static ImmutableArray<SymbolKindOrTypeKind> GetSymbolsApplicableKinds(
            string symbolSpecName,
            IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_symbols.{symbolSpecName}.applicable_kinds", out object result))
            {
                return ParseSymbolKindList(result as string ?? string.Empty);
            }

            return _all;
        }

        private static readonly SymbolKindOrTypeKind _namespace = new SymbolKindOrTypeKind(SymbolKind.Namespace);
        private static readonly SymbolKindOrTypeKind _class = new SymbolKindOrTypeKind(TypeKind.Class);
        private static readonly SymbolKindOrTypeKind _struct = new SymbolKindOrTypeKind(TypeKind.Struct);
        private static readonly SymbolKindOrTypeKind _interface = new SymbolKindOrTypeKind(TypeKind.Interface);
        private static readonly SymbolKindOrTypeKind _enum = new SymbolKindOrTypeKind(TypeKind.Enum);
        private static readonly SymbolKindOrTypeKind _property = new SymbolKindOrTypeKind(SymbolKind.Property);
        private static readonly SymbolKindOrTypeKind _method = new SymbolKindOrTypeKind(MethodKind.Ordinary);
        private static readonly SymbolKindOrTypeKind _localFunction = new SymbolKindOrTypeKind(MethodKind.LocalFunction);
        private static readonly SymbolKindOrTypeKind _field = new SymbolKindOrTypeKind(SymbolKind.Field);
        private static readonly SymbolKindOrTypeKind _event = new SymbolKindOrTypeKind(SymbolKind.Event);
        private static readonly SymbolKindOrTypeKind _delegate = new SymbolKindOrTypeKind(TypeKind.Delegate);
        private static readonly SymbolKindOrTypeKind _parameter = new SymbolKindOrTypeKind(SymbolKind.Parameter);
        private static readonly SymbolKindOrTypeKind _typeParameter = new SymbolKindOrTypeKind(SymbolKind.TypeParameter);
        private static readonly SymbolKindOrTypeKind _local = new SymbolKindOrTypeKind(SymbolKind.Local);
        private static readonly ImmutableArray<SymbolKindOrTypeKind> _all =
            ImmutableArray.Create(
                _namespace,
                _class,
                _struct,
                _interface,
                _enum,
                _property,
                _method,
                _localFunction,
                _field,
                _event,
                _delegate,
                _parameter,
                _typeParameter,
                _local);

        private static ImmutableArray<SymbolKindOrTypeKind> ParseSymbolKindList(string symbolSpecApplicableKinds)
        {
            if (symbolSpecApplicableKinds == null)
            {
                return ImmutableArray<SymbolKindOrTypeKind>.Empty;
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

        private static ImmutableArray<Accessibility> GetSymbolsApplicableAccessibilities(
            string symbolSpecName,
            IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_symbols.{symbolSpecName}.applicable_accessibilities", out object result))
            {
                return ParseAccessibilityKindList(result as string ?? string.Empty);
            }

            return _allAccessibility;
        }

        private static readonly ImmutableArray<Accessibility> _allAccessibility = ImmutableArray.Create(Accessibility.NotApplicable, Accessibility.Public, Accessibility.Internal, Accessibility.Private, Accessibility.Protected, Accessibility.ProtectedAndInternal, Accessibility.ProtectedOrInternal);

        private static ImmutableArray<Accessibility> ParseAccessibilityKindList(string symbolSpecApplicableAccessibilities)
        {
            if (symbolSpecApplicableAccessibilities == null)
            {
                return ImmutableArray<Accessibility>.Empty;
            }

            if (symbolSpecApplicableAccessibilities.Trim() == "*")
            {
                return _allAccessibility;
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

        private static ImmutableArray<ModifierKind> GetSymbolsRequiredModifiers(
            string symbolSpecName,
            IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_symbols.{symbolSpecName}.required_modifiers", out object result))
            {
                return ParseModifiers(result as string ?? string.Empty);
            }

            return ImmutableArray<ModifierKind>.Empty;
        }

        private static readonly ModifierKind _abstractModifierKind = new ModifierKind(ModifierKindEnum.IsAbstract);
        private static readonly ModifierKind _asyncModifierKind = new ModifierKind(ModifierKindEnum.IsAsync);
        private static readonly ModifierKind _constModifierKind = new ModifierKind(ModifierKindEnum.IsConst);
        private static readonly ModifierKind _readonlyModifierKind = new ModifierKind(ModifierKindEnum.IsReadOnly);
        private static readonly ModifierKind _staticModifierKind = new ModifierKind(ModifierKindEnum.IsStatic);
        private static readonly ImmutableArray<ModifierKind> _allModifierKind = ImmutableArray.Create(_abstractModifierKind, _asyncModifierKind, _constModifierKind, _readonlyModifierKind, _staticModifierKind);

        private static ImmutableArray<ModifierKind> ParseModifiers(string symbolSpecRequiredModifiers)
        {
            if (symbolSpecRequiredModifiers == null)
            {
                return ImmutableArray<ModifierKind>.Empty;
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
                        builder.Add(_abstractModifierKind);
                        break;
                    case "async":
                        builder.Add(_asyncModifierKind);
                        break;
                    case "const":
                        builder.Add(_constModifierKind);
                        break;
                    case "readonly":
                        builder.Add(_readonlyModifierKind);
                        break;
                    case "static":
                    case "shared":
                        builder.Add(_staticModifierKind);
                        break;
                    default:
                        break;
                }
            }

            return builder.ToImmutableAndFree();
        }
    }
}
