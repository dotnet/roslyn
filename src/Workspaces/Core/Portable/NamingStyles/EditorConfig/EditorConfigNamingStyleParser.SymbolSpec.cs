// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
                Guid.NewGuid(),
                symbolSpecName,
                symbolKindList: applicableKinds,
                accessibilityKindList: applicableAccessibilities,
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

            return ImmutableArray<SymbolKindOrTypeKind>.Empty;
        }

        private static readonly SymbolKindOrTypeKind _class = new SymbolKindOrTypeKind(TypeKind.Class);
        private static readonly SymbolKindOrTypeKind _struct = new SymbolKindOrTypeKind(TypeKind.Struct);
        private static readonly SymbolKindOrTypeKind _interface = new SymbolKindOrTypeKind(TypeKind.Interface);
        private static readonly SymbolKindOrTypeKind _enum = new SymbolKindOrTypeKind(TypeKind.Enum);
        private static readonly SymbolKindOrTypeKind _property = new SymbolKindOrTypeKind(SymbolKind.Property);
        private static readonly SymbolKindOrTypeKind _method = new SymbolKindOrTypeKind(SymbolKind.Method);
        private static readonly SymbolKindOrTypeKind _field = new SymbolKindOrTypeKind(SymbolKind.Field);
        private static readonly SymbolKindOrTypeKind _event = new SymbolKindOrTypeKind(SymbolKind.Event);
        private static readonly SymbolKindOrTypeKind _namespace = new SymbolKindOrTypeKind(SymbolKind.Namespace);
        private static readonly SymbolKindOrTypeKind _delegate = new SymbolKindOrTypeKind(TypeKind.Delegate);
        private static readonly SymbolKindOrTypeKind _typeParameter = new SymbolKindOrTypeKind(SymbolKind.TypeParameter);
        private static readonly ImmutableArray<SymbolKindOrTypeKind> _all = ImmutableArray.Create(_class, _struct, _interface, _enum, _property, _method, _field, _event, _namespace, _delegate, _typeParameter);
        private static ImmutableArray<SymbolKindOrTypeKind> ParseSymbolKindList(string symbolSpecApplicableKinds)
        {
            if (symbolSpecApplicableKinds == null)
            {
                return ImmutableArray<SymbolKindOrTypeKind>.Empty;
            }

            var builder = ArrayBuilder<SymbolKindOrTypeKind>.GetInstance();
            if (symbolSpecApplicableKinds.Trim() == "*")
            {
                return _all;
            }

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
                    case "field":
                        builder.Add(_field);
                        break;
                    case "event":
                        builder.Add(_event);
                        break;
                    case "namespace":
                        builder.Add(_namespace);
                        break;
                    case "delegate":
                        builder.Add(_delegate);
                        break;
                    case "type_parameter":
                        builder.Add(_typeParameter);
                        break;
                    default:
                        break;
                }
            }

            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<AccessibilityKind> GetSymbolsApplicableAccessibilities(
            string symbolSpecName,
            IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_symbols.{symbolSpecName}.applicable_accessibilities", out object result))
            {
                return ParseAccessibilityKindList(result as string ?? string.Empty);
            }

            return ImmutableArray<AccessibilityKind>.Empty;
        }

        private static readonly AccessibilityKind _publicAccessibilityKind = new AccessibilityKind(Accessibility.Public);
        private static readonly AccessibilityKind _internalAccessibilityKind = new AccessibilityKind(Accessibility.Internal);
        private static readonly AccessibilityKind _privateAccessibilityKind = new AccessibilityKind(Accessibility.Private);
        private static readonly AccessibilityKind _protectedAccessibilityKind = new AccessibilityKind(Accessibility.Protected);
        private static readonly AccessibilityKind _protectedOrInternalAccessibilityKind = new AccessibilityKind(Accessibility.ProtectedOrInternal);

        private static ImmutableArray<AccessibilityKind> ParseAccessibilityKindList(string symbolSpecApplicableAccessibilities)
        {
            if (symbolSpecApplicableAccessibilities == null)
            {
                return ImmutableArray<AccessibilityKind>.Empty;
            }

            var builder = ArrayBuilder<AccessibilityKind>.GetInstance();
            if (symbolSpecApplicableAccessibilities.Trim() == "*")
            {
                builder.AddRange(_publicAccessibilityKind, _internalAccessibilityKind, _privateAccessibilityKind, _protectedAccessibilityKind, _protectedOrInternalAccessibilityKind);
                return builder.ToImmutableAndFree();
            }

            foreach (var symbolSpecApplicableAccessibility in symbolSpecApplicableAccessibilities.Split(',').Select(x => x.Trim()))
            {
                switch (symbolSpecApplicableAccessibility)
                {
                    case "public":
                        builder.Add(_publicAccessibilityKind);
                        break;
                    case "internal":
                        builder.Add(_internalAccessibilityKind);
                        break;
                    case "private":
                        builder.Add(_privateAccessibilityKind);
                        break;
                    case "protected":
                        builder.Add(_protectedAccessibilityKind);
                        break;
                    case "protected_internal":
                        builder.Add(_protectedOrInternalAccessibilityKind);
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

        private static ImmutableArray<ModifierKind> ParseModifiers(string symbolSpecRequiredModifiers)
        {
            if (symbolSpecRequiredModifiers == null)
            {
                return ImmutableArray<ModifierKind>.Empty;
            }

            var builder = ArrayBuilder<ModifierKind>.GetInstance();
            if (symbolSpecRequiredModifiers.Trim() == "*")
            {
                builder.AddRange(_abstractModifierKind, _asyncModifierKind, _constModifierKind, _readonlyModifierKind, _staticModifierKind);
                return builder.ToImmutableAndFree();
            }

            foreach (var symbolSpecRequiredModifier in symbolSpecRequiredModifiers.Split(',').Select(x => x.Trim()))
            {
                switch (symbolSpecRequiredModifier)
                {
                    case "abstract":
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
