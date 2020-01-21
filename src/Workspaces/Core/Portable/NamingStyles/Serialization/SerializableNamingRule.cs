// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class SerializableNamingRule
    {
        public Guid SymbolSpecificationID;
        public Guid NamingStyleID;
        public ReportDiagnostic EnforcementLevel;

        public NamingRule GetRule(NamingStylePreferences info)
        {
            return new NamingRule(
                info.GetSymbolSpecification(SymbolSpecificationID),
                info.GetNamingStyle(NamingStyleID),
                EnforcementLevel);
        }

        internal XElement CreateXElement()
        {
            var element = new XElement(nameof(SerializableNamingRule),
                new XAttribute(nameof(SymbolSpecificationID), SymbolSpecificationID),
                new XAttribute(nameof(NamingStyleID), NamingStyleID),
                new XAttribute(nameof(EnforcementLevel), EnforcementLevel.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));

            return element;
        }

        internal static SerializableNamingRule FromXElement(XElement namingRuleElement)
        {
            return new SerializableNamingRule()
            {
                EnforcementLevel = ((DiagnosticSeverity)Enum.Parse(typeof(DiagnosticSeverity), namingRuleElement.Attribute(nameof(EnforcementLevel)).Value)).ToReportDiagnostic(),
                NamingStyleID = Guid.Parse(namingRuleElement.Attribute(nameof(NamingStyleID)).Value),
                SymbolSpecificationID = Guid.Parse(namingRuleElement.Attribute(nameof(SymbolSpecificationID)).Value)
            };
        }
    }
}
