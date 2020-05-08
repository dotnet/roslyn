// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal sealed class SerializableNamingRule : IEquatable<SerializableNamingRule>
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

        public override bool Equals(object obj)
        {
            return Equals(obj as SerializableNamingRule);
        }

        public bool Equals(SerializableNamingRule other)
        {
            return other != null
                && SymbolSpecificationID.Equals(other.SymbolSpecificationID)
                && NamingStyleID.Equals(other.NamingStyleID)
                && EnforcementLevel == other.EnforcementLevel;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(SymbolSpecificationID.GetHashCode(),
                Hash.Combine(NamingStyleID.GetHashCode(),
                    (int)EnforcementLevel));
        }
    }
}
