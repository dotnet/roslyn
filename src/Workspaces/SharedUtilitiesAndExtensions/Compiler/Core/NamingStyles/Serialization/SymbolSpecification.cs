// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using System.Runtime.Serialization;
using System.Diagnostics;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

[DataContract]
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal sealed class SymbolSpecification(
    Guid id,
    string name,
    ImmutableArray<SymbolSpecification.SymbolKindOrTypeKind> symbolKindList,
    ImmutableArray<Accessibility> accessibilityList = default,
    ImmutableArray<SymbolSpecification.ModifierKind> modifiers = default) : IEquatable<SymbolSpecification>
{
    private static readonly SymbolSpecification DefaultSymbolSpecificationTemplate = CreateDefaultSymbolSpecification();

    [DataMember(Order = 0)]
    public Guid ID { get; } = id;

    [DataMember(Order = 1)]
    public string Name { get; } = name;

    [DataMember(Order = 2)]
    public ImmutableArray<SymbolKindOrTypeKind> ApplicableSymbolKindList { get; } = symbolKindList.IsDefault ? DefaultSymbolSpecificationTemplate.ApplicableSymbolKindList : symbolKindList;

    [DataMember(Order = 3)]
    public ImmutableArray<Accessibility> ApplicableAccessibilityList { get; } = accessibilityList.IsDefault ? DefaultSymbolSpecificationTemplate.ApplicableAccessibilityList : accessibilityList;

    [DataMember(Order = 4)]
    public ImmutableArray<ModifierKind> RequiredModifierList { get; } = modifiers.IsDefault ? DefaultSymbolSpecificationTemplate.RequiredModifierList : modifiers;

    private string GetDebuggerDisplay()
        => Name;

    public static SymbolSpecification CreateDefaultSymbolSpecification()
    {
        // This is used to create new, empty symbol specifications for users to then customize.
        // Since these customized specifications will eventually coexist with all the other
        // existing specifications, always use a new, distinct guid.

        return new SymbolSpecification(
            id: Guid.NewGuid(),
            name: null,
            symbolKindList:
            [
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
                new SymbolKindOrTypeKind(SymbolKind.Local),
            ],
            accessibilityList:
            [
                Accessibility.NotApplicable,
                Accessibility.Public,
                Accessibility.Internal,
                Accessibility.Private,
                Accessibility.Protected,
                Accessibility.ProtectedAndInternal,
                Accessibility.ProtectedOrInternal,
            ],
            modifiers: []);
    }

    public bool AppliesTo(ISymbol symbol)
        => AnyMatches(this.ApplicableSymbolKindList, symbol) &&
           AllMatches(this.RequiredModifierList, symbol) &&
           AnyMatches(this.ApplicableAccessibilityList, symbol);

    public bool AppliesTo(SymbolKind symbolKind, Accessibility accessibility)
        => this.AppliesTo(new SymbolKindOrTypeKind(symbolKind), new DeclarationModifiers(), accessibility);

    public bool AppliesTo(SymbolKindOrTypeKind kind, DeclarationModifiers modifiers, Accessibility? accessibility)
    {
        if (!ApplicableSymbolKindList.Any(static (k, kind) => k.Equals(kind), kind))
        {
            return false;
        }

        var collapsedModifiers = CollapseModifiers(RequiredModifierList);
        if ((modifiers & collapsedModifiers) != collapsedModifiers)
        {
            return false;
        }

        if (accessibility.HasValue && !ApplicableAccessibilityList.Any(static (k, accessibility) => k == accessibility, accessibility))
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
            reader.ReadArray(SymbolKindOrTypeKind.ReadFrom),
            reader.ReadArray(r => (Accessibility)r.ReadInt32()),
            reader.ReadArray(ModifierKind.ReadFrom));
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
        => new(
            id: Guid.Parse(symbolSpecificationElement.Attribute(nameof(ID)).Value),
            name: symbolSpecificationElement.Attribute(nameof(Name)).Value,
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

    [DataContract]
    public readonly record struct SymbolKindOrTypeKind : ISymbolMatcher
    {
        public enum SymbolCategory : byte
        {
            Invalid = 0,
            Other = 1,
            Type = 2,
            Method = 3,
        }

        [DataMember(Order = 0)]
        private readonly SymbolCategory _category;

        [DataMember(Order = 1)]
        private readonly byte _kind;

        // public for serialization
        public SymbolKindOrTypeKind(SymbolCategory category, byte kind)
        {
            _category = category;
            _kind = kind;
        }

        public SymbolKindOrTypeKind(SymbolKind symbolKind)
            : this(SymbolCategory.Other, checked((byte)symbolKind))
        {
        }

        public SymbolKindOrTypeKind(TypeKind typeKind)
            : this(SymbolCategory.Type, checked((byte)typeKind))
        {
        }

        public SymbolKindOrTypeKind(MethodKind methodKind)
            : this(SymbolCategory.Method, checked((byte)methodKind))
        {
        }

        public SymbolKind? SymbolKind => (_category == SymbolCategory.Other) ? (SymbolKind)_kind : null;
        public TypeKind? TypeKind => (_category == SymbolCategory.Type) ? (TypeKind)_kind : null;
        public MethodKind? MethodKind => (_category == SymbolCategory.Method) ? (MethodKind)_kind : null;

        public bool MatchesSymbol(ISymbol symbol)
            => _category switch
            {
                SymbolCategory.Other => symbol.IsKind((SymbolKind)_kind),
                SymbolCategory.Type => symbol is ITypeSymbol type && type.TypeKind == (TypeKind)_kind,
                SymbolCategory.Method => symbol is IMethodSymbol method && method.MethodKind == (MethodKind)_kind,
                _ => false
            };

        internal XElement CreateXElement()
            => _category switch
            {
                SymbolCategory.Other => new XElement(nameof(SymbolKind), (SymbolKind)_kind),
                SymbolCategory.Type => new XElement(nameof(TypeKind), GetTypeKindString((TypeKind)_kind)),
                SymbolCategory.Method => new XElement(nameof(MethodKind), GetMethodKindString((MethodKind)_kind)),
                _ => throw ExceptionUtilities.Unreachable()
            };

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

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteInt32((int)_category);

            // handle default(T)
            if (_category != SymbolCategory.Invalid)
            {
                writer.WriteInt32(_kind);
            }
        }

        public static SymbolKindOrTypeKind ReadFrom(ObjectReader reader)
        {
            var category = (SymbolCategory)reader.ReadInt32();
            var kind = (byte)((category != SymbolCategory.Invalid) ? reader.ReadInt32() : 0);
            return new SymbolKindOrTypeKind(category, kind);
        }

        internal static SymbolKindOrTypeKind AddSymbolKindFromXElement(XElement symbolKindElement)
        {
            var symbolKind = (SymbolKind)Enum.Parse(typeof(SymbolKind), symbolKindElement.Value);
            return symbolKind switch
            {
                // Handle cases where SymbolKind.Method was persisted as SymbolCategory.Other by automatically
                // converting them to MethodKind.Ordinary.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1861733
                CodeAnalysis.SymbolKind.Method => new(CodeAnalysis.MethodKind.Ordinary),
                _ => new(symbolKind),
            };
        }

        internal static SymbolKindOrTypeKind AddTypeKindFromXElement(XElement typeKindElement)
            => new((TypeKind)Enum.Parse(typeof(TypeKind), typeKindElement.Value));

        internal static SymbolKindOrTypeKind AddMethodKindFromXElement(XElement methodKindElement)
            => new((MethodKind)Enum.Parse(typeof(MethodKind), methodKindElement.Value));
    }

    [DataContract]
    public readonly struct ModifierKind : ISymbolMatcher, IEquatable<ModifierKind>
    {
        [DataMember(Order = 0)]
        public readonly ModifierKindEnum ModifierKindWrapper;

        internal DeclarationModifiers Modifier { get; }

        public ModifierKind(DeclarationModifiers modifier)
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

        public ModifierKind(ModifierKindEnum modifierKind)
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
            => new(nameof(ModifierKind), ModifierKindWrapper);

        internal static ModifierKind FromXElement(XElement modifierElement)
            => new((ModifierKindEnum)Enum.Parse(typeof(ModifierKindEnum), modifierElement.Value));

        public void WriteTo(ObjectWriter writer)
            => writer.WriteInt32((int)ModifierKindWrapper);

        public static ModifierKind ReadFrom(ObjectReader reader)
            => new((ModifierKindEnum)reader.ReadInt32());

        public override bool Equals(object obj)
            => obj is ModifierKind kind && Equals(kind);

        public override int GetHashCode()
            => (int)ModifierKindWrapper;

        public bool Equals(ModifierKind other)
            => ModifierKindWrapper == other.ModifierKindWrapper;
    }

    public enum ModifierKindEnum : byte
    {
        IsAbstract,
        IsStatic,
        IsAsync,
        IsReadOnly,
        IsConst,
    }
}
