// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal static class RuleSetDocumentExtensions
{
    internal static void SetSeverity(this XDocument ruleSet, string analyzerId, string ruleId, ReportDiagnostic value)
    {
        var newAction = ConvertReportDiagnosticToAction(value);

        var rules = FindOrCreateRulesElement(ruleSet, analyzerId);
        var rule = FindOrCreateRuleElement(rules, ruleId);

        if (value == ReportDiagnostic.Default)
        {
            // If the new severity is 'Default' we just delete the entry for the rule from the ruleset file.
            // In the absence of an explicit entry in the ruleset file, the rule reverts back to its 'Default'
            // severity (so far as the 'current' ruleset file is concerned - the rule's effective severity
            // could still be decided by other factors such as project settings or a base ruleset file).
            rule.Remove();
        }
        else
        {
            rule.Attribute("Action").Value = newAction;
        }

        var allMatchingRules = ruleSet.Root
                                   .Descendants("Rule")
                                   .Where(r => r.Attribute("Id").Value.Equals(ruleId))
                                   .ToList();

        foreach (var matchingRule in allMatchingRules)
        {
            if (value == ReportDiagnostic.Default)
            {
                // If the new severity is 'Default' we just delete the entry for the rule from the ruleset file.
                // In the absence of an explicit entry in the ruleset file, the rule reverts back to its 'Default'
                // severity (so far as the 'current' ruleset file is concerned - the rule's effective severity
                // could still be decided by other factors such as project settings or a base ruleset file).
                matchingRule.Remove();
            }
            else
            {
                matchingRule.Attribute("Action").Value = newAction;
            }
        }
    }

    private static string ConvertReportDiagnosticToAction(ReportDiagnostic value)
    {
        switch (value)
        {
            case ReportDiagnostic.Default:
                return "Default";
            case ReportDiagnostic.Error:
                return "Error";
            case ReportDiagnostic.Warn:
                return "Warning";
            case ReportDiagnostic.Info:
                return "Info";
            case ReportDiagnostic.Hidden:
                return "Hidden";
            case ReportDiagnostic.Suppress:
                return "None";
            default:
                throw ExceptionUtilities.Unreachable();
        }
    }

    private static XElement FindOrCreateRuleElement(XElement rules, string id)
    {
        var ruleElement = rules
                          .Elements("Rule")
                          .FirstOrDefault(r => r.Attribute("Id").Value.Equals(id));

        if (ruleElement == null)
        {
            ruleElement = new XElement("Rule",
                                new XAttribute("Id", id),
                                new XAttribute("Action", "Warning"));
            rules.Add(ruleElement);
        }

        return ruleElement;
    }

    private static XElement FindOrCreateRulesElement(XDocument ruleSetDocument, string analyzerID)
    {
        var rulesElement = ruleSetDocument.Root
                           .Elements("Rules")
                           .FirstOrDefault(r => r.Attribute("AnalyzerId").Value.Equals(analyzerID));

        if (rulesElement == null)
        {
            rulesElement = new XElement("Rules",
                                new XAttribute("AnalyzerId", analyzerID),
                                new XAttribute("RuleNamespace", analyzerID));
            ruleSetDocument.Root.Add(rulesElement);
        }

        return rulesElement;
    }
}
