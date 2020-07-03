// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal sealed class SymbolSpecification : IEquatable<SymbolSpecification>, IObjectWritable
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

        public bool AppliesTo(ISymbol symbol)
            => AnyMatches(this.ApplicableSymbolKindList, symbol) &&
               AllMatches(this.RequiredModifierList, symbol) &&
               AnyMatches(this.ApplicableAccessibilityList, symbol);

        public bool AppliesTo(SymbolKind symbolKind, Accessibility accessibility)
            => this.AppliesTo(new SymbolKindOrTypeKind(symbolKind), new DeclarationModifiers(), accessibility);

        public bool AppliesTo(SymbolKindOrTypeKind kind, DeclarationModifiers modifiers, Accessibility? accessibility)
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

        private static DeclarationModifiers CollapseModifiers(ImmutableArray<ModifierKind> requiredModifierList)
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

        private static bool AnyMatches<TSymbolMatcher>(ImmutableArray<TSymbolMatcher> matchers, ISymbol symbol)
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

        private static bool AnyMatches(ImmutableArray<Accessibility> matchers, ISymbol symbol)
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

        private static bool AllMatches<TSymbolMatcher>(ImmutableArray<TSymbolMatcher> matchers, ISymbol symbol)
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

        public override bool Equals(object obj)
        {
            return Equals(obj as SymbolSpecification);
        }

        public bool Equals(SymbolSpecification other)
        {
            if (other is null)
                return false;

            return ID == other.ID
                && Name == other.Name
                && ApplicableSymbolKindList.SequenceEqual(other.ApplicableSymbolKindList)
                && ApplicableAccessibilityList.SequenceEqual(other.ApplicableAccessibilityList)
                && RequiredModifierList.SequenceEqual(other.RequiredModifierList);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ID.GetHashCode(),
                Hash.Combine(Name.GetHashCode(),
                    Hash.Combine(Hash.CombineValues(ApplicableSymbolKindList),
                        Hash.Combine(Hash.CombineValues(ApplicableAccessibilityList),
                            Hash.CombineValues(RequiredModifierList)))));
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

        public bool ShouldReuseInSerialization => false;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteGuid(ID);
            writer.WriteString(Name);
            writer.WriteArray(ApplicableSymbolKindList, (w, v) => v.WriteTo(w));
            writer.WriteArray(ApplicableAccessibilityList, (w, v) => w.WriteInt32((int)v));
            writer.WriteArray(RequiredModifierList, (w, v) => v.WriteTo(w));
        }

        public static SymbolSpecification ReadFrom(ObjectReader reader)
        {
            return new SymbolSpecification(
                reader.ReadGuid(),
                reader.ReadString(),
                reader.ReadArray(r => SymbolKindOrTypeKind.ReadFrom(r)),
                reader.ReadArray(r => (Accessibility)r.ReadInt32()),
                reader.ReadArray(r => ModifierKind.ReadFrom(r)));
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

        public struct SymbolKindOrTypeKind : IEquatable<SymbolKindOrTypeKind>, ISymbolMatcher, IObjectWritable
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
                   TypeKind.HasValue ? symbol is ITypeSymbol type && type.TypeKind == TypeKind.Value :
                   MethodKind.HasValue ? symbol is IMethodSymbol method && method.MethodKind == MethodKind.Value :
                   throw ExceptionUtilities.Unreachable;

            internal XElement CreateXElement()
                => SymbolKind.HasValue ? new XElement(nameof(SymbolKind), SymbolKind) :
                   TypeKind.HasValue ? new XElement(nameof(TypeKind), GetTypeKindString(TypeKind.Value)) :
                   MethodKind.HasValue ? new XElement(nameof(MethodKind), GetMethodKindString(MethodKind.Value)) :
                   throw ExceptionUtilities.Unreachable;

            private static string GetTypeKindString(TypeKind typeKind)
            {
                // We have two members in TypeKind that point to the same value, Struct and Structure. Because of this,
                // Enum.ToString(), which under the covers uses a binary search, isn't stable which one it will pick and it can
                // change if other TypeKinds are added. This ensures we keep using the same string consistently.
                return typeKind switch
                {
                    CodeAnalysis.TypeKind.Structure => nameof(CodeAnalysis.TypeKind.Struct),
                    _ => typeKind.ToString()
                };
            }

            private static string GetMethodKindString(MethodKind methodKind)
            {
                // We ehave some members in TypeKind that point to the same value. Because of this,
                // Enum.ToString(), which under the covers uses a binary search, isn't stable which one it will pick and it can
                // change if other MethodKinds are added. This ensures we keep using the same string consistently.
                return methodKind switch
                {

                    CodeAnalysis.MethodKind.SharedConstructor => nameof(CodeAnalysis.MethodKind.StaticConstructor),
                    CodeAnalysis.MethodKind.AnonymousFunction => nameof(CodeAnalysis.MethodKind.LambdaMethod),
                    _ => methodKind.ToString()
                };
            }

            public bool ShouldReuseInSerialization => false;

            public void WriteTo(ObjectWriter writer)
            {
                if (SymbolKind != null)
                {
                    writer.WriteInt32(1);
                    writer.WriteInt32((int)SymbolKind);
                }
                else if (TypeKind != null)
                {
                    writer.WriteInt32(2);
                    writer.WriteInt32((int)TypeKind);
                }
                else if (MethodKind != null)
                {
                    writer.WriteInt32(3);
                    writer.WriteInt32((int)MethodKind);
                }
                else
                {
                    writer.WriteInt32(0);
                }
            }

            public static SymbolKindOrTypeKind ReadFrom(ObjectReader reader)
            {
                return reader.ReadInt32() switch
                {
                    0 => default,
                    1 => new SymbolKindOrTypeKind((SymbolKind)reader.ReadInt32()),
                    2 => new SymbolKindOrTypeKind((TypeKind)reader.ReadInt32()),
                    3 => new SymbolKindOrTypeKind((MethodKind)reader.ReadInt32()),
                    var v => throw ExceptionUtilities.UnexpectedValue(v),
                };
            }

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
            {
                return Hash.Combine((int)SymbolKind.GetValueOrDefault(),
                    Hash.Combine((int)TypeKind.GetValueOrDefault(), (int)MethodKind.GetValueOrDefault()));
            }
        }

        public struct ModifierKind : ISymbolMatcher, IEquatable<ModifierKind>, IObjectWritable
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

            public bool ShouldReuseInSerialization => false;

            public void WriteTo(ObjectWriter writer)
                => writer.WriteInt32((int)ModifierKindWrapper);

            public static ModifierKind ReadFrom(ObjectReader reader)
                => new ModifierKind((ModifierKindEnum)reader.ReadInt32());

            public override bool Equals(object obj)
                => obj is ModifierKind kind && Equals(kind);

            public override int GetHashCode()
                => (int)ModifierKindWrapper;

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
