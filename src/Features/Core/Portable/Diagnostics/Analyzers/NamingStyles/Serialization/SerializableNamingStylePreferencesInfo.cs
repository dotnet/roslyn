// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    /// <summary>
    /// Contains all information related to Naming Style Preferences.
    /// 1. Symbol Specifications
    /// 2. Name Style
    /// 3. Naming Rule (points to Symbol Specification IDs)
    /// </summary>
    internal class SerializableNamingStylePreferencesInfo
    {
        public List<SymbolSpecification> SymbolSpecifications;
        public List<NamingStyle> NamingStyles;
        public List<SerializableNamingRule> NamingRules;
        private readonly static int _serializationVersion = 1;

        internal SerializableNamingStylePreferencesInfo()
        {
            SymbolSpecifications = new List<SymbolSpecification>();
            NamingStyles = new List<NamingStyle>();
            NamingRules = new List<SerializableNamingRule>();
        }

        internal NamingStyle GetNamingStyle(Guid namingStyleID)
        {
            return NamingStyles.Single(s => s.ID == namingStyleID);
        }

        internal SymbolSpecification GetSymbolSpecification(Guid symbolSpecificationID)
        {
            return SymbolSpecifications.Single(s => s.ID == symbolSpecificationID);
        }

        public NamingStylePreferencesInfo GetPreferencesInfo()
        {
            return new NamingStylePreferencesInfo(NamingRules.Select(r => r.GetRule(this)).ToImmutableArray());
        }

        internal XElement CreateXElement()
        {
            return new XElement("NamingPreferencesInfo", 
                new XAttribute("SerializationVersion", _serializationVersion),
                CreateSymbolSpecificationListXElement(),
                CreateNamingStyleListXElement(),
                CreateNamingRuleTreeXElement());
        }

        private XElement CreateNamingRuleTreeXElement()
        {
            var namingRulesElement = new XElement(nameof(NamingRules));

            foreach (var namingRule in NamingRules)
            {
                namingRulesElement.Add(namingRule.CreateXElement());
            }

            return namingRulesElement;
        }

        private XElement CreateNamingStyleListXElement()
        {
            var namingStylesElement = new XElement(nameof(NamingStyles));

            foreach (var namingStyle in NamingStyles)
            {
                namingStylesElement.Add(namingStyle.CreateXElement());
            }

            return namingStylesElement;
        }

        private XElement CreateSymbolSpecificationListXElement()
        {
            var symbolSpecificationsElement = new XElement(nameof(SymbolSpecifications));

            foreach (var symbolSpecification in SymbolSpecifications)
            {
                symbolSpecificationsElement.Add(symbolSpecification.CreateXElement());
            }

            return symbolSpecificationsElement;
        }

        internal static SerializableNamingStylePreferencesInfo FromXElement(XElement namingPreferencesInfoElement)
        {
            var namingPreferencesInfo = new SerializableNamingStylePreferencesInfo();

            namingPreferencesInfo.SetSymbolSpecificationListFromXElement(namingPreferencesInfoElement.Element(nameof(SymbolSpecifications)));
            namingPreferencesInfo.SetNamingStyleListFromXElement(namingPreferencesInfoElement.Element(nameof(NamingStyles)));
            namingPreferencesInfo.SetNamingRuleTreeFromXElement(namingPreferencesInfoElement.Element(nameof(NamingRules)));

            return namingPreferencesInfo;
        }

        private void SetSymbolSpecificationListFromXElement(XElement symbolSpecificationsElement)
        {
            foreach (var symbolSpecificationElement in symbolSpecificationsElement.Elements(nameof(SymbolSpecification)))
            {
                SymbolSpecifications.Add(SymbolSpecification.FromXElement(symbolSpecificationElement));
            }
        }

        private void SetNamingStyleListFromXElement(XElement namingStylesElement)
        {
            foreach (var namingStyleElement in namingStylesElement.Elements(nameof(NamingStyle)))
            {
                NamingStyles.Add(NamingStyle.FromXElement(namingStyleElement));
            }
        }

        private void SetNamingRuleTreeFromXElement(XElement namingRulesElement)
        {
            foreach (var namingRuleElement in namingRulesElement.Elements(nameof(SerializableNamingRule)))
            {
                NamingRules.Add(SerializableNamingRule.FromXElement(namingRuleElement));
            }
        }
    }
}
