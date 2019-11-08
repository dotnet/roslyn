// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class SymbolSpecification
    {
        private static readonly SymbolSpecification DefaultSymbolSpecificationTemplate = CreateDefaultSymbolSpecification();

        public Guid ID { get; }
        public string Name { get; }

        public ImmutableArray<SymbolKindOrTypeKind> ApplicableSymbolKindList { get; }
        public ImmutableArray<Accessibility> ApplicableAccessibilityList { get; }
        public ImmutableArray<ModifierKind> RequiredModifierList { get; }

        public SymbolSpecification(
            Guid? id, string symbolSpecName,
            ImmutableArray<SymbolKindOrTypeKind> symbolKindList,
            ImmutableArray<Accessibility> accessibilityList = default,
            ImmutableArray<ModifierKind> modifiers = default)
        {
            ID = id ?? Guid.NewGuid();
            Name = symbolSpecName;
            ApplicableSymbolKindList = symbolKindList.IsDefault ? DefaultSymbolSpecificationTemplate.ApplicableSymbolKindList : symbolKindList;
            ApplicableAccessibilityList = accessibilityList.IsDefault ? DefaultSymbolSpecificationTemplate.ApplicableAccessibilityList : accessibilityList;
            RequiredModifierList = modifiers.IsDefault ? DefaultSymbolSpecificationTemplate.RequiredModifierList : modifiers;
        }

        public static SymbolSpecification CreateDefaultSymbolSpecification()
        {
            // This is used to create new, empty symbol specifications for users to then customize.
            // Since these customized specifications will eventually coexist with all the other
            // existing specifications, always use a new, distinct guid.

            return new SymbolSpecification(
                id: Guid.NewGuid(),
                symbolSpecName: null,
                symbolKindList: ImmutableArray.Create(
                    new SymbolKindOrTypeKind(SymbolKind.Namespace),
                    new SymbolKindOrTypeKind(TypeKind.Class),
                    new SymbolKindOrTypeKind(TypeKind.Struct),
                    new SymbolKindOrTypeKind(TypeKind.Interface),
                    new SymbolKindOrTypeKind(TypeKind.Delegate),
                    new SymbolKindOrTypeKind(TypeKind.Enum),
                    new SymbolKindOrTypeKind(TypeKind.Module),
                    new SymbolKindOrTypeKind(TypeKind.Pointer),
                    new SymbolKindOrTypeKind(SymbolKind.Property),
                    new SymbolKindOrTypeKind(MethodKind.Ordinary),
                    new SymbolKindOrTypeKind(MethodKind.LocalFunction),
                    new SymbolKindOrTypeKind(SymbolKind.Field),
                    new SymbolKindOrTypeKind(SymbolKind.Event),
                    new SymbolKindOrTypeKind(SymbolKind.Parameter),
                    new SymbolKindOrTypeKind(TypeKind.TypeParameter),
                    new SymbolKindOrTypeKind(SymbolKind.Local)),
                accessibilityList: ImmutableArray.Create(
                    Accessibility.NotApplicable,
                    Accessibility.Public,
                    Accessibility.Internal,
                    Accessibility.Private,
                    Accessibility.Protected,
                    Accessibility.ProtectedAndInternal,
                    Accessibility.ProtectedOrInternal),
                modifiers: ImmutableArray<ModifierKind>.Empty);
        }

        internal bool AppliesTo(ISymbol symbol)
        {
            return AnyMatches(this.ApplicableSymbolKindList, symbol) &&
                   AllMatches(this.RequiredModifierList, symbol) &&
                   AnyMatches(this.ApplicableAccessibilityList, symbol);
        }

        internal bool AppliesTo(SymbolKind symbolKind, Accessibility accessibility)
            => this.AppliesTo(new SymbolKindOrTypeKind(symbolKind), new DeclarationModifiers(), accessibility);

        internal bool AppliesTo(SymbolKindOrTypeKind kind, DeclarationModifiers modifiers, Accessibility? accessibility)
        {
            if (!ApplicableSymbolKindList.Any(k => k.Equals(kind)))
            {
                return false;
            }

            var collapsedModifiers = CollapseModifiers(RequiredModifierList);
            if ((modifiers & collapsedModifiers) != collapsedModifiers)
            {
                return false;
            }

            if (accessibility.HasValue && !ApplicableAccessibilityList.Any(k => k == accessibility))
            {
                return false;
            }

            return true;
        }

        private DeclarationModifiers CollapseModifiers(ImmutableArray<ModifierKind> requiredModifierList)
        {
            if (requiredModifierList == default)
            {
                return new DeclarationModifiers();
            }

            var result = new DeclarationModifiers();
            foreach (var modifier in requiredModifierList)
            {
                switch (modifier.ModifierKindWrapper)
                {
                    case ModifierKindEnum.IsAbstract:
                        result = result.WithIsAbstract(true);
                        break;
                    case ModifierKindEnum.IsStatic:
                        result = result.WithIsStatic(true);
                        break;
                    case ModifierKindEnum.IsAsync:
                        result = result.WithAsync(true);
                        break;
                    case ModifierKindEnum.IsReadOnly:
                        result = result.WithIsReadOnly(true);
                        break;
                    case ModifierKindEnum.IsConst:
                        result = result.WithIsConst(true);
                        break;
                }
            }
            return result;
        }

        private bool AnyMatches<TSymbolMatcher>(ImmutableArray<TSymbolMatcher> matchers, ISymbol symbol)
            where TSymbolMatcher : ISymbolMatcher
        {
            foreach (var matcher in matchers)
            {
                if (matcher.MatchesSymbol(symbol))
                {
                    return true;
                }
            }

            return false;
        }

        private bool AnyMatches(ImmutableArray<Accessibility> matchers, ISymbol symbol)
        {
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
                accessibilityList: GetAccessibilityListFromXElement(symbolSpecificationElement.Element(nameof(ApplicableAccessibilityList))),
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

            foreach (var methodKindElement in symbolKindListElement.Elements(nameof(MethodKind)))
            {
                applicableSymbolKindList.Add(SymbolKindOrTypeKind.AddMethodKindFromXElement(methodKindElement));
            }

            return applicableSymbolKindList.ToImmutableAndFree();
        }

        private static ImmutableArray<Accessibility> GetAccessibilityListFromXElement(XElement accessibilityListElement)
        {
            var applicableAccessibilityList = ArrayBuilder<Accessibility>.GetInstance();
            foreach (var accessibilityElement in accessibilityListElement.Elements("AccessibilityKind"))
            {
                applicableAccessibilityList.Add(AccessibilityExtensions.FromXElement(accessibilityElement));
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

        public struct SymbolKindOrTypeKind : IEquatable<SymbolKindOrTypeKind>, ISymbolMatcher
        {
            public SymbolKind? SymbolKind { get; }
            public TypeKind? TypeKind { get; }
            public MethodKind? MethodKind { get; }

            public SymbolKindOrTypeKind(SymbolKind symbolKind) : this()
            {
                SymbolKind = symbolKind;
                TypeKind = null;
                MethodKind = null;
            }

            public SymbolKindOrTypeKind(TypeKind typeKind) : this()
            {
                SymbolKind = null;
                TypeKind = typeKind;
                MethodKind = null;
            }

            public SymbolKindOrTypeKind(MethodKind methodKind) : this()
            {
                SymbolKind = null;
                TypeKind = null;
                MethodKind = methodKind;
            }

            public bool MatchesSymbol(ISymbol symbol)
                => SymbolKind.HasValue ? symbol.IsKind(SymbolKind.Value) :
                   TypeKind.HasValue ? symbol is ITypeSymbol { TypeKind: TypeKind.Value } type :
                   MethodKind.HasValue ? symbol is IMethodSymbol method && method.MethodKind == MethodKind.Value :
                   throw ExceptionUtilities.Unreachable;

            internal XElement CreateXElement()
                => SymbolKind.HasValue ? new XElement(nameof(SymbolKind), SymbolKind) :
                   TypeKind.HasValue ? new XElement(nameof(TypeKind), TypeKind) :
                   MethodKind.HasValue ? new XElement(nameof(MethodKind), MethodKind) :
                   throw ExceptionUtilities.Unreachable;

            internal static SymbolKindOrTypeKind AddSymbolKindFromXElement(XElement symbolKindElement)
                => new SymbolKindOrTypeKind((SymbolKind)Enum.Parse(typeof(SymbolKind), symbolKindElement.Value));

            internal static SymbolKindOrTypeKind AddTypeKindFromXElement(XElement typeKindElement)
                => new SymbolKindOrTypeKind((TypeKind)Enum.Parse(typeof(TypeKind), typeKindElement.Value));

            internal static SymbolKindOrTypeKind AddMethodKindFromXElement(XElement methodKindElement)
                => new SymbolKindOrTypeKind((MethodKind)Enum.Parse(typeof(MethodKind), methodKindElement.Value));

            public override bool Equals(object obj)
                => Equals((SymbolKindOrTypeKind)obj);

            public bool Equals(SymbolKindOrTypeKind other)
                => this.SymbolKind == other.SymbolKind && this.TypeKind == other.TypeKind && this.MethodKind == other.MethodKind;

            public override int GetHashCode()
                => Hash.CombineValues(new[] {
                    (int)this.SymbolKind.GetValueOrDefault(),
                    (int)this.TypeKind.GetValueOrDefault(),
                    (int)this.MethodKind.GetValueOrDefault()
                });
        }

        public struct ModifierKind : ISymbolMatcher, IEquatable<ModifierKind>
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
                => new ModifierKind((ModifierKindEnum)Enum.Parse(typeof(ModifierKindEnum), modifierElement.Value));

            public override bool Equals(object obj)
                => obj is ModifierKind kind && Equals(kind);

            public override int GetHashCode()
                => ModifierKindWrapper.GetHashCode();

            public bool Equals(ModifierKind other)
                => ModifierKindWrapper == other.ModifierKindWrapper;
        }

        public enum ModifierKindEnum
        {
            IsAbstract,
            IsStatic,
            IsAsync,
            IsReadOnly,
            IsConst,
        }
    }
}
