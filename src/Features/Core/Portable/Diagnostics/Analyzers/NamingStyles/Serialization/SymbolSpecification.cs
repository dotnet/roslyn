// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolCategorization;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class SymbolSpecification
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public List<SymbolKindOrTypeKind> ApplicableSymbolKindList { get; private set; }
        public List<AccessibilityKind> ApplicableAccessibilityList { get; private set; }
        public List<ModifierKind> RequiredModifierList { get; private set; }
        public List<string> RequiredCustomTagList { get; private set; }

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

            RequiredModifierList = new List<ModifierKind>();

            RequiredCustomTagList = new List<string>();
        }

        public SymbolSpecification(Guid id, string symbolSpecName, List<SymbolKindOrTypeKind> symbolKindList, List<AccessibilityKind> accessibilityKindList, List<ModifierKind> modifierList, List<string> customTagList)
        {
            ID = id;
            Name = symbolSpecName;
            ApplicableAccessibilityList = accessibilityKindList;
            RequiredModifierList = modifierList;
            ApplicableSymbolKindList = symbolKindList;
            RequiredCustomTagList = customTagList;
        }

        internal bool AppliesTo(ISymbol symbol, ISymbolCategorizationService categorizationService)
        {
            if (ApplicableSymbolKindList.Any() && !ApplicableSymbolKindList.Any(k => k.AppliesTo(symbol)))
            {
                return false;
            }

            if (!RequiredModifierList.All(m => m.MatchesSymbol(symbol)))
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

            foreach (var modifier in RequiredModifierList)
            {
                modifiersElement.Add(modifier.CreateXElement());
            }

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
            ApplicableSymbolKindList = new List<SymbolKindOrTypeKind>();
            foreach (var symbolKindElement in symbolKindListElement.Elements(nameof(SymbolKind)))
            {
                ApplicableSymbolKindList.Add(SymbolKindOrTypeKind.AddSymbolKindFromXElement(symbolKindElement));
            }

            foreach (var typeKindElement in symbolKindListElement.Elements(nameof(TypeKind)))
            {
                ApplicableSymbolKindList.Add(SymbolKindOrTypeKind.AddTypeKindFromXElement(typeKindElement));
            }
        }

        private void PopulateAccessibilityListFromXElement(XElement accessibilityListElement)
        {
            ApplicableAccessibilityList = new List<AccessibilityKind>();
            foreach (var accessibilityElement in accessibilityListElement.Elements(nameof(AccessibilityKind)))
            {
                ApplicableAccessibilityList.Add(AccessibilityKind.FromXElement(accessibilityElement));
            }
        }

        private void PopulateModifierListFromXElement(XElement modifierListElement)
        {
            RequiredModifierList = new List<ModifierKind>();
            foreach (var modifierElement in modifierListElement.Elements(nameof(ModifierKind)))
            {
                RequiredModifierList.Add(ModifierKind.FromXElement(modifierElement));
            }
        }

        private void PopulateCustomTagListFromXElement(XElement customTagListElement)
        {
            RequiredCustomTagList = new List<string>();
            foreach (var customTag in customTagListElement.Elements("CustomTag"))
            {
                RequiredCustomTagList.Add(customTag.Value);
            }
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

            internal XElement CreateXElement()
            {
                return new XElement(nameof(ModifierKind), ModifierKindWrapper);
            }

            internal static ModifierKind FromXElement(XElement modifierElement)
            {
                return new ModifierKind((ModifierKindEnum)(ModifierKindEnum)Enum.Parse((Type)typeof(ModifierKindEnum), (string)modifierElement.Value));
            }
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