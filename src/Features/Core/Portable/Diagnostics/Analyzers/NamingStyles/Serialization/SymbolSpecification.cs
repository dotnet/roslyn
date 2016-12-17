// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class SymbolSpecification
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }

        public ImmutableArray<SymbolKindOrTypeKind> ApplicableSymbolKindList { get; }
        public ImmutableArray<AccessibilityKind> ApplicableAccessibilityList { get; }
        public ImmutableArray<ModifierKind> RequiredModifierList { get; }

        internal SymbolSpecification()
        {
            ID = Guid.NewGuid();

            ApplicableSymbolKindList = ImmutableArray.Create(
                new SymbolKindOrTypeKind(SymbolKind.Namespace),
                new SymbolKindOrTypeKind(TypeKind.Class),
                new SymbolKindOrTypeKind(TypeKind.Struct),
                new SymbolKindOrTypeKind(TypeKind.Interface),
                new SymbolKindOrTypeKind(TypeKind.Delegate),
                new SymbolKindOrTypeKind(TypeKind.Enum),
                new SymbolKindOrTypeKind(TypeKind.Module),
                new SymbolKindOrTypeKind(TypeKind.Pointer),
                new SymbolKindOrTypeKind(TypeKind.TypeParameter),
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(SymbolKind.Method),
                new SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolKindOrTypeKind(SymbolKind.Event));

            ApplicableAccessibilityList = ImmutableArray.Create(
                new AccessibilityKind(Accessibility.Public),
                new AccessibilityKind(Accessibility.Internal),
                new AccessibilityKind(Accessibility.Private),
                new AccessibilityKind(Accessibility.Protected),
                new AccessibilityKind(Accessibility.ProtectedAndInternal),
                new AccessibilityKind(Accessibility.ProtectedOrInternal));

            RequiredModifierList = ImmutableArray<ModifierKind>.Empty;
        }

        public SymbolSpecification(
            Guid id, string symbolSpecName,
            ImmutableArray<SymbolKindOrTypeKind> symbolKindList,
            ImmutableArray<AccessibilityKind> accessibilityKindList,
            ImmutableArray<ModifierKind> modifiers)
        {
            ID = id;
            Name = symbolSpecName;
            ApplicableAccessibilityList = accessibilityKindList;
            RequiredModifierList = modifiers;
            ApplicableSymbolKindList = symbolKindList;
        }

        internal bool AppliesTo(ISymbol symbol)
        {
            return AnyMatches(this.ApplicableSymbolKindList, symbol) &&
                   AllMatches(this.RequiredModifierList, symbol) &&
                   AnyMatches(this.ApplicableAccessibilityList, symbol);
        }

        private bool AnyMatches<TSymbolMatcher>(ImmutableArray<TSymbolMatcher> matchers, ISymbol symbol)
            where TSymbolMatcher : ISymbolMatcher
        {
            if (!matchers.Any())
            {
                return true;
            }

            foreach (var matcher in matchers)
            {
                if (matcher.MatchesSymbol(symbol))
                {
                    return true;
                }
            }

            return false;
        }

        private bool AllMatches<TSymbolMatcher>(ImmutableArray<TSymbolMatcher> matchers, ISymbol symbol)
            where TSymbolMatcher : ISymbolMatcher
        {
            foreach (var matcher in matchers)
            {
                if (!matcher.MatchesSymbol(symbol))
                {
                    return false;
                }
            }

            return true;
        }

        internal XElement CreateXElement()
        {
            return new XElement(nameof(SymbolSpecification),
                new XAttribute(nameof(ID), ID),
                new XAttribute(nameof(Name), Name),
                CreateSymbolKindsXElement(),
                CreateAccessibilitiesXElement(),
                CreateModifiersXElement());
        }

        private XElement CreateSymbolKindsXElement()
        {
            var symbolKindsElement = new XElement(nameof(ApplicableSymbolKindList));

            foreach (var symbolKind in ApplicableSymbolKindList)
            {
                symbolKindsElement.Add(symbolKind.CreateXElement());
            }

            return symbolKindsElement;
        }

        private XElement CreateAccessibilitiesXElement()
        {
            var accessibilitiesElement = new XElement(nameof(ApplicableAccessibilityList));

            foreach (var accessibility in ApplicableAccessibilityList)
            {
                accessibilitiesElement.Add(accessibility.CreateXElement());
            }

            return accessibilitiesElement;
        }

        private XElement CreateModifiersXElement()
        {
            var modifiersElement = new XElement(nameof(RequiredModifierList));

            foreach (var modifier in RequiredModifierList)
            {
                modifiersElement.Add(modifier.CreateXElement());
            }

            return modifiersElement;
        }

        internal static SymbolSpecification FromXElement(XElement symbolSpecificationElement)
            => new SymbolSpecification(
                id: Guid.Parse(symbolSpecificationElement.Attribute(nameof(ID)).Value),
                symbolSpecName: symbolSpecificationElement.Attribute(nameof(Name)).Value,
                symbolKindList: GetSymbolKindListFromXElement(symbolSpecificationElement.Element(nameof(ApplicableSymbolKindList))),
                accessibilityKindList: GetAccessibilityListFromXElement(symbolSpecificationElement.Element(nameof(ApplicableAccessibilityList))),
                modifiers: GetModifierListFromXElement(symbolSpecificationElement.Element(nameof(RequiredModifierList))));

        private static ImmutableArray<SymbolKindOrTypeKind> GetSymbolKindListFromXElement(XElement symbolKindListElement)
        {
            var applicableSymbolKindList = ArrayBuilder<SymbolKindOrTypeKind>.GetInstance();
            foreach (var symbolKindElement in symbolKindListElement.Elements(nameof(SymbolKind)))
            {
                applicableSymbolKindList.Add(SymbolKindOrTypeKind.AddSymbolKindFromXElement(symbolKindElement));
            }

            foreach (var typeKindElement in symbolKindListElement.Elements(nameof(TypeKind)))
            {
                applicableSymbolKindList.Add(SymbolKindOrTypeKind.AddTypeKindFromXElement(typeKindElement));
            }

            return applicableSymbolKindList.ToImmutableAndFree();
        }

        private static ImmutableArray<AccessibilityKind> GetAccessibilityListFromXElement(XElement accessibilityListElement)
        {
            var applicableAccessibilityList = ArrayBuilder<AccessibilityKind>.GetInstance();
            foreach (var accessibilityElement in accessibilityListElement.Elements(nameof(AccessibilityKind)))
            {
                applicableAccessibilityList.Add(AccessibilityKind.FromXElement(accessibilityElement));
            }
            return applicableAccessibilityList.ToImmutableAndFree();
        }

        private static ImmutableArray<ModifierKind> GetModifierListFromXElement(XElement modifierListElement)
        {
            var result = ArrayBuilder<ModifierKind>.GetInstance();
            foreach (var modifierElement in modifierListElement.Elements(nameof(ModifierKind)))
            {
                result.Add(ModifierKind.FromXElement(modifierElement));
            }

            return result.ToImmutableAndFree();
        }

        private interface ISymbolMatcher
        {
            bool MatchesSymbol(ISymbol symbol);
        }

        public struct SymbolKindOrTypeKind : ISymbolMatcher
        {
            public SymbolKind? SymbolKind { get; }
            public TypeKind? TypeKind { get; }

            public SymbolKindOrTypeKind(SymbolKind symbolKind) : this()
            {
                SymbolKind = symbolKind;
            }

            public SymbolKindOrTypeKind(TypeKind typeKind) : this()
            {
                TypeKind = typeKind;
            }

            public bool MatchesSymbol(ISymbol symbol)
                => SymbolKind.HasValue
                    ? symbol.IsKind(SymbolKind.Value)
                    : symbol is ITypeSymbol s && s.TypeKind == TypeKind.Value;

            internal XElement CreateXElement()
                => SymbolKind.HasValue
                     ? new XElement(nameof(SymbolKind), SymbolKind)
                     : new XElement(nameof(TypeKind), TypeKind);

            internal static SymbolKindOrTypeKind AddSymbolKindFromXElement(XElement symbolKindElement)
                => new SymbolKindOrTypeKind((SymbolKind)Enum.Parse(typeof(SymbolKind), symbolKindElement.Value));

            internal static SymbolKindOrTypeKind AddTypeKindFromXElement(XElement typeKindElement)
                => new SymbolKindOrTypeKind((TypeKind)Enum.Parse(typeof(TypeKind), typeKindElement.Value));
        }

        public struct AccessibilityKind : ISymbolMatcher
        {
            public Accessibility Accessibility { get; }

            public AccessibilityKind(Accessibility accessibility)
            {
                Accessibility = accessibility;
            }

            public bool MatchesSymbol(ISymbol symbol)
                => symbol.DeclaredAccessibility == Accessibility;

            internal XElement CreateXElement()
                => new XElement(nameof(AccessibilityKind), Accessibility);

            internal static AccessibilityKind FromXElement(XElement accessibilityElement)
                => new AccessibilityKind((Accessibility)Enum.Parse(typeof(Accessibility), accessibilityElement.Value));
        }

        public struct ModifierKind : ISymbolMatcher
        {
            public ModifierKindEnum ModifierKindWrapper;

            internal DeclarationModifiers Modifier { get; }

            public ModifierKind(DeclarationModifiers modifier) : this()
            {
                this.Modifier = modifier;

                if (modifier.IsAbstract)
                {
                    ModifierKindWrapper = ModifierKindEnum.IsAbstract;
                }
                else if (modifier.IsStatic)
                {
                    ModifierKindWrapper = ModifierKindEnum.IsStatic;
                }
                else if (modifier.IsAsync)
                {
                    ModifierKindWrapper = ModifierKindEnum.IsAsync;
                }
                else if (modifier.IsReadOnly)
                {
                    ModifierKindWrapper = ModifierKindEnum.IsReadOnly;
                }
                else if (modifier.IsConst)
                {
                    ModifierKindWrapper = ModifierKindEnum.IsConst;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public ModifierKind(ModifierKindEnum modifierKind) : this()
            {
                ModifierKindWrapper = modifierKind;

                Modifier = new DeclarationModifiers(
                    isAbstract: ModifierKindWrapper == ModifierKindEnum.IsAbstract,
                    isStatic: ModifierKindWrapper == ModifierKindEnum.IsStatic,
                    isAsync: ModifierKindWrapper == ModifierKindEnum.IsAsync,
                    isReadOnly: ModifierKindWrapper == ModifierKindEnum.IsReadOnly,
                    isConst: ModifierKindWrapper == ModifierKindEnum.IsConst);
            }

            public bool MatchesSymbol(ISymbol symbol)
            {
                if ((Modifier.IsAbstract && symbol.IsAbstract) ||
                    (Modifier.IsStatic && symbol.IsStatic))
                {
                    return true;
                }

                var kind = symbol.Kind;
                if (Modifier.IsAsync && kind == SymbolKind.Method && ((IMethodSymbol)symbol).IsAsync)
                {
                    return true;
                }

                if (Modifier.IsReadOnly)
                {
                    if (kind == SymbolKind.Field && ((IFieldSymbol)symbol).IsReadOnly)
                    {
                        return true;
                    }
                }

                if (Modifier.IsConst)
                {
                    if ((kind == SymbolKind.Field && ((IFieldSymbol)symbol).IsConst) ||
                        (kind == SymbolKind.Local && ((ILocalSymbol)symbol).IsConst))
                    {
                        return true;
                    }
                }

                return false;
            }

            internal XElement CreateXElement()
                => new XElement(nameof(ModifierKind), ModifierKindWrapper);

            internal static ModifierKind FromXElement(XElement modifierElement)
                => new ModifierKind((ModifierKindEnum)(ModifierKindEnum)Enum.Parse((Type)typeof(ModifierKindEnum), (string)modifierElement.Value));
        }

        public enum ModifierKindEnum
        {
            IsAbstract,
            IsStatic,
            IsAsync,
            IsReadOnly,
            IsConst
        }
    }
}