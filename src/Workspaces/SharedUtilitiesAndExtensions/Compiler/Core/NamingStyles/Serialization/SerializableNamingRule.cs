// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    [DataContract]
    internal sealed record class SerializableNamingRule
    {
        [DataMember(Order = 0)]
        public Guid SymbolSpecificationID { get; init; }

        [DataMember(Order = 1)]
        public Guid NamingStyleID { get; init; }

        [DataMember(Order = 2)]
        public ReportDiagnostic EnforcementLevel { get; init; }

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

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteGuid(SymbolSpecificationID);
            writer.WriteGuid(NamingStyleID);
            writer.WriteInt32((int)(EnforcementLevel.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));
        }

        public static SerializableNamingRule ReadFrom(ObjectReader reader)
        {
            return new SerializableNamingRule
            {
                SymbolSpecificationID = reader.ReadGuid(),
                NamingStyleID = reader.ReadGuid(),
                EnforcementLevel = ((DiagnosticSeverity)reader.ReadInt32()).ToReportDiagnostic(),
            };
        }
    }
}
