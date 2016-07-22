// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolCategorization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class SymbolSpecification
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public IEnumerable<SymbolKindOrTypeKind> ApplicableSymbolKindList { get; private set; }
        public IEnumerable<AccessibilityKind> ApplicableAccessibilityList { get; private set; }
        public ModifierKindEnum RequiredModifierList { get; private set; }
        public IEnumerable<string> RequiredCustomTagList { get; private set; }

        internal SymbolSpecification()
        {
            ID = Guid.NewGuid();

            ApplicableSymbolKindList = new List<SymbolKindOrTypeKind>
                {
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
                    new SymbolKindOrTypeKind(SymbolKind.Event),
                };

            ApplicableAccessibilityList = new List<AccessibilityKind>
                {
                    new AccessibilityKind(Accessibility.Public),
                    new AccessibilityKind(Accessibility.Internal),
                    new AccessibilityKind(Accessibility.Private),
                    new AccessibilityKind(Accessibility.Protected),
                    new AccessibilityKind(Accessibility.ProtectedAndInternal),
                    new AccessibilityKind(Accessibility.ProtectedOrInternal),
                };

            RequiredModifierList = ModifierKindEnum.None;

            RequiredCustomTagList = new List<string>();
        }

        public SymbolSpecification(Guid id, string symbolSpecName,
            IEnumerable<SymbolKindOrTypeKind> symbolKindList,
            IEnumerable<AccessibilityKind> accessibilityKindList,
            ModifierKindEnum modifierList,
            IEnumerable<string> customTagList)
        {
            ID = id;
            Name = symbolSpecName;
            ApplicableAccessibilityList = accessibilityKindList;
            RequiredModifierList = modifierList;
            ApplicableSymbolKindList = symbolKindList;
            RequiredCustomTagList = customTagList;
        }

        public SymbolSpecification(string symbolSpecName, SymbolKindOrTypeKind kind, ModifierKindEnum modifierList, List<AccessibilityKind> accessibilityList, string guidString)
            : this(Guid.Parse(guidString), symbolSpecName, SpecializedCollections.SingletonEnumerable(kind), accessibilityList, modifierList, SpecializedCollections.EmptyEnumerable<string>())
        {

        }

        internal bool AppliesTo(ISymbol symbol, ISymbolCategorizationService categorizationService)
        {
            if (ApplicableSymbolKindList.Any() && !ApplicableSymbolKindList.Any(k => k.AppliesTo(symbol)))
            {
                return false;
            }
            
            // Modifiers must match exactly
            if (RequiredModifierList != ModifierKind.GetModifiers(symbol))
            {
                return false;
            }

            if (ApplicableAccessibilityList.Any() && !ApplicableAccessibilityList.Any(k => k.MatchesSymbol(symbol)))
            {
                return false;
            }

            // TODO: More efficient to find the categorizers that are relevant and only check those
            var applicableCategories = categorizationService.GetCategorizers().SelectMany(c => c.Categorize(symbol));
            if (!RequiredCustomTagList.All(t => applicableCategories.Contains(t)))
            {
                return false;
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
                CreateModifiersXElement(),
                CreateCustomTagsXElement());
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
            modifiersElement.Value = RequiredModifierList.ToString();

            return modifiersElement;
        }

        private XElement CreateCustomTagsXElement()
        {
            var customTagsElement = new XElement(nameof(RequiredCustomTagList));

            foreach (var customTag in RequiredCustomTagList)
            {
                customTagsElement.Add(new XElement("CustomTag", customTag));
            }

            return customTagsElement;
        }

        internal static SymbolSpecification FromXElement(XElement symbolSpecificationElement)
        {
            var result = new SymbolSpecification();
            result.ID = Guid.Parse(symbolSpecificationElement.Attribute(nameof(ID)).Value);
            result.Name = symbolSpecificationElement.Attribute(nameof(Name)).Value;

            result.PopulateSymbolKindListFromXElement(symbolSpecificationElement.Element(nameof(ApplicableSymbolKindList)));
            result.PopulateAccessibilityListFromXElement(symbolSpecificationElement.Element(nameof(ApplicableAccessibilityList)));
            result.PopulateModifierListFromXElement(symbolSpecificationElement.Element(nameof(RequiredModifierList)));
            result.PopulateCustomTagListFromXElement(symbolSpecificationElement.Element(nameof(RequiredCustomTagList)));

            return result;
        }

        private void PopulateSymbolKindListFromXElement(XElement symbolKindListElement)
        {
            var applicableSymbolKindList = new List<SymbolKindOrTypeKind>();
            foreach (var symbolKindElement in symbolKindListElement.Elements(nameof(SymbolKind)))
            {
                applicableSymbolKindList.Add(SymbolKindOrTypeKind.AddSymbolKindFromXElement(symbolKindElement));
            }

            foreach (var typeKindElement in symbolKindListElement.Elements(nameof(TypeKind)))
            {
                applicableSymbolKindList.Add(SymbolKindOrTypeKind.AddTypeKindFromXElement(typeKindElement));
            }
            ApplicableSymbolKindList = applicableSymbolKindList;
        }

        private void PopulateAccessibilityListFromXElement(XElement accessibilityListElement)
        {
            var applicableAccessibilityList = new List<AccessibilityKind>();
            foreach (var accessibilityElement in accessibilityListElement.Elements(nameof(AccessibilityKind)))
            {
                applicableAccessibilityList.Add(AccessibilityKind.FromXElement(accessibilityElement));
            }
            ApplicableAccessibilityList = applicableAccessibilityList;
        }

        private void PopulateModifierListFromXElement(XElement modifierListElement)
        {
            ModifierKindEnum modifiers = (ModifierKindEnum)Enum.Parse(typeof(ModifierKindEnum), modifierListElement.Value);
            RequiredModifierList = modifiers;
        }

        private void PopulateCustomTagListFromXElement(XElement customTagListElement)
        {
            var requiredCustomTagList = new List<string>();
            foreach (var customTag in customTagListElement.Elements("CustomTag"))
            {
                requiredCustomTagList.Add(customTag.Value);
            }
            RequiredCustomTagList = requiredCustomTagList;
        }

        public class SymbolKindOrTypeKind
        {
            public SymbolKind? SymbolKind { get; set; }
            public TypeKind? TypeKind { get; set; }

            public SymbolKindOrTypeKind(SymbolKind symbolKind)
            {
                SymbolKind = symbolKind;
            }

            public SymbolKindOrTypeKind(TypeKind typeKind)
            {
                TypeKind = typeKind;
            }

            public bool AppliesTo(ISymbol symbol)
            {
                if (SymbolKind.HasValue)
                {
                    return symbol.IsKind(SymbolKind.Value);
                }
                else
                {
                    var typeSymbol = symbol as ITypeSymbol;
                    return typeSymbol != null && typeSymbol.TypeKind == TypeKind.Value;
                }
            }

            internal XElement CreateXElement()
            {
                if (SymbolKind.HasValue)
                {
                    return new XElement(nameof(SymbolKind), SymbolKind);
                }
                else
                {
                    return new XElement(nameof(TypeKind), TypeKind);
                }
            }

            internal static SymbolKindOrTypeKind AddSymbolKindFromXElement(XElement symbolKindElement)
            {
                return new SymbolKindOrTypeKind((SymbolKind)Enum.Parse(typeof(SymbolKind), symbolKindElement.Value));
            }

            internal static SymbolKindOrTypeKind AddTypeKindFromXElement(XElement typeKindElement)
            {
                return new SymbolKindOrTypeKind((TypeKind)Enum.Parse(typeof(TypeKind), typeKindElement.Value));
            }
        }

        public class AccessibilityKind
        {
            public Accessibility Accessibility { get; set; }

            public AccessibilityKind(Accessibility accessibility)
            {
                Accessibility = accessibility;
            }

            public bool MatchesSymbol(ISymbol symbol)
            {
                return symbol.DeclaredAccessibility == Accessibility;
            }

            internal XElement CreateXElement()
            {
                return new XElement(nameof(AccessibilityKind), Accessibility);
            }

            internal static AccessibilityKind FromXElement(XElement accessibilityElement)
            {
                return new AccessibilityKind((Accessibility)Enum.Parse(typeof(Accessibility), accessibilityElement.Value));
            }
        }

        public class ModifierKind
        {
            public ModifierKindEnum ModifierKindWrapper;

            private DeclarationModifiers _modifier;
            internal DeclarationModifiers Modifier
            {
                get
                {
                    if (_modifier == DeclarationModifiers.None)
                    {
                        _modifier = new DeclarationModifiers(
                            isAbstract: ModifierKindWrapper == ModifierKindEnum.IsAbstract,
                            isStatic: ModifierKindWrapper == ModifierKindEnum.IsStatic,
                            isAsync: ModifierKindWrapper == ModifierKindEnum.IsAsync,
                            isReadOnly: ModifierKindWrapper == ModifierKindEnum.IsReadOnly,
                            isConst: ModifierKindWrapper == ModifierKindEnum.IsConst);
                    }

                    return _modifier;
                }
                set
                {
                    _modifier = value;

                    if (value.IsAbstract)
                    {
                        ModifierKindWrapper = ModifierKindEnum.IsAbstract;
                    }
                    else if (value.IsStatic)
                    {
                        ModifierKindWrapper = ModifierKindEnum.IsStatic;
                    }
                    else if (value.IsAsync)
                    {
                        ModifierKindWrapper = ModifierKindEnum.IsAsync;
                    }
                    else if (value.IsReadOnly)
                    {
                        ModifierKindWrapper = ModifierKindEnum.IsReadOnly;
                    }
                    else if (value.IsConst)
                    {
                        ModifierKindWrapper = ModifierKindEnum.IsConst;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            public ModifierKind(DeclarationModifiers modifier)
            {
                this.Modifier = modifier;
            }

            public ModifierKind(ModifierKindEnum modifierKind)
            {
                ModifierKindWrapper = modifierKind;
            }

            public bool MatchesSymbol(ISymbol symbol)
            {
                if ((Modifier.IsAbstract && symbol.IsAbstract) ||
                    (Modifier.IsStatic && symbol.IsStatic))
                {
                    return true;
                }

                var method = symbol as IMethodSymbol;
                var field = symbol as IFieldSymbol;
                var local = symbol as ILocalSymbol;

                if (Modifier.IsAsync && method != null && method.IsAsync)
                {
                    return true;
                }

                if (Modifier.IsReadOnly && field != null && field.IsReadOnly)
                {
                    return true;
                }

                if (Modifier.IsConst && (field != null && field.IsConst) || (local != null && local.IsConst))
                {
                    return true;
                }

                return false;
            }

            public static ModifierKindEnum GetModifiers(ISymbol symbol)
            {
                var result = ModifierKindEnum.None;

                if (symbol.IsAbstract)
                {
                    result = result | ModifierKindEnum.IsAbstract;
                }

                if (symbol.IsStatic)
                {
                    result |= ModifierKindEnum.IsStatic;
                }

                var method = symbol as IMethodSymbol;
                var field = symbol as IFieldSymbol;
                var local = symbol as ILocalSymbol;

                if (method != null && method.IsAsync)
                {
                    result |= ModifierKindEnum.IsAsync;
                }

                if (field != null && field.IsReadOnly)
                {
                    result |= ModifierKindEnum.IsReadOnly;
                }

                if ((field != null && field.IsConst) || (local != null && local.IsConst))
                {
                    result |= ModifierKindEnum.IsConst;
                }

                return result;
            }

            internal XElement CreateXElement()
            {
                return new XElement(nameof(ModifierKind), ModifierKindWrapper);
            }

            internal static ModifierKind FromXElement(XElement modifierElement)
            {
                return new ModifierKind((ModifierKindEnum)(ModifierKindEnum)Enum.Parse((Type)typeof(ModifierKindEnum), (string)modifierElement.Value));
            }
        }

        public enum ModifierKindEnum : short
        {
            None = 0,
            IsAbstract = 1,
            IsStatic = 2,
            IsAsync = 4,
            IsReadOnly = 8,
            IsConst = 16
        }
    }
}