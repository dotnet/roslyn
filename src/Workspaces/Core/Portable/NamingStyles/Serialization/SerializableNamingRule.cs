// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class SerializableNamingRule
    {
        public readonly string Name;
        public readonly Guid SymbolSpecificationID;
        public readonly Guid NamingStyleID;
        public readonly ReportDiagnostic EnforcementLevel;

        public SerializableNamingRule(string name, Guid symbolSpecificationID, Guid namingStyleID, ReportDiagnostic enforcementLevel)
        {
            Name = name;
            SymbolSpecificationID = symbolSpecificationID;
            NamingStyleID = namingStyleID;
            EnforcementLevel = enforcementLevel;
        }

        public NamingRule GetRule(NamingStylePreferences info)
        {
            return new NamingRule(
                Name,
                info.GetSymbolSpecification(SymbolSpecificationID),
                info.GetNamingStyle(NamingStyleID),
                EnforcementLevel);
        }

        internal XElement CreateXElement()
        {
            var element = new XElement(nameof(SerializableNamingRule),
                new XAttribute(nameof(Name), Name),
                new XAttribute(nameof(SymbolSpecificationID), SymbolSpecificationID),
                new XAttribute(nameof(NamingStyleID), NamingStyleID),
                new XAttribute(nameof(EnforcementLevel), EnforcementLevel.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));

            return element;
        }

        internal static SerializableNamingRule FromXElement(XElement namingRuleElement)
        {
            return new SerializableNamingRule(
                namingRuleElement.Attribute(nameof(Name))?.Value,
                Guid.Parse(namingRuleElement.Attribute(nameof(SymbolSpecificationID)).Value),
                Guid.Parse(namingRuleElement.Attribute(nameof(NamingStyleID)).Value),
                ((DiagnosticSeverity)Enum.Parse(typeof(DiagnosticSeverity), namingRuleElement.Attribute(nameof(EnforcementLevel)).Value)).ToReportDiagnostic());
        }
    }
}
