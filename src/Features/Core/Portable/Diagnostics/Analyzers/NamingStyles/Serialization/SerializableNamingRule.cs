// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class SerializableNamingRule
    {
        public string Title;
        public List<SerializableNamingRule> Children;
        public Guid SymbolSpecificationID;
        public Guid NamingStyleID;
        public DiagnosticSeverity EnforcementLevel;

        internal SerializableNamingRule()
        {
            Children = new List<SerializableNamingRule>();
        }

        public NamingRule GetRule(SerializableNamingStylePreferencesInfo info)
        {
            return new NamingRule(
                Title,
                Children.Select(c => c.GetRule(info)).ToImmutableArray(),
                info.GetSymbolSpecification(SymbolSpecificationID),
                info.GetNamingStyle(NamingStyleID),
                EnforcementLevel);
        }

        internal XElement CreateXElement()
        {
            var element = new XElement(nameof(SerializableNamingRule),
                new XAttribute(nameof(Title), Title),
                new XAttribute(nameof(SymbolSpecificationID), SymbolSpecificationID),
                new XAttribute(nameof(NamingStyleID), NamingStyleID),
                new XAttribute(nameof(EnforcementLevel), EnforcementLevel));

            foreach (var child in Children)
            {
                element.Add(child.CreateXElement());
            }

            return element;
        }

        internal static SerializableNamingRule FromXElement(XElement namingRuleElement)
        {
            var result = new SerializableNamingRule();
            result.Title = namingRuleElement.Attribute(nameof(Title)).Value;
            result.EnforcementLevel = (DiagnosticSeverity)Enum.Parse(typeof(DiagnosticSeverity), namingRuleElement.Attribute(nameof(EnforcementLevel)).Value);
            result.NamingStyleID = Guid.Parse(namingRuleElement.Attribute(nameof(NamingStyleID)).Value);
            result.SymbolSpecificationID = Guid.Parse(namingRuleElement.Attribute(nameof(SymbolSpecificationID)).Value);

            result.Children = new List<SerializableNamingRule>();
            foreach (var childElement in namingRuleElement.Elements(nameof(SerializableNamingRule)))
            {
                result.Children.Add(FromXElement(childElement));
            }

            return result;
        }
    }
}
