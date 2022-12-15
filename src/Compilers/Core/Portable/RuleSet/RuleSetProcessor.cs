// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This type is responsible for parsing a ruleset xml file and producing a <see cref="RuleSet"/> object.
    /// </summary>
    internal class RuleSetProcessor
    {
        // Strings for the RuleSet node
        private const string RuleSetNodeName = "RuleSet";
        private const string RuleSetNameAttributeName = "Name";
        private const string RuleSetToolsVersionAttributeName = "ToolsVersion";

        // Strings for the Rules node
        private const string RulesNodeName = "Rules";
        private const string RulesAnalyzerIdAttributeName = "AnalyzerId";
        private const string RulesNamespaceAttributeName = "RuleNamespace";

        // Strings for the Rule node
        private const string RuleNodeName = "Rule";
        private const string RuleIdAttributeName = "Id";

        // Strings for the Include node
        private const string IncludeNodeName = "Include";
        private const string IncludePathAttributeName = "Path";

        // Strings for the IncludeAll node
        private const string IncludeAllNodeName = "IncludeAll";

        // Strings for the Action attribute and its values
        private const string RuleActionAttributeName = "Action";
        private const string RuleActionNoneValue = "None";
        private const string RuleActionHiddenValue = "Hidden";
        private const string RuleActionInfoValue = "Info";
        private const string RuleActionWarningValue = "Warning";
        private const string RuleActionErrorValue = "Error";
        private const string RuleActionDefaultValue = "Default";

        /// <summary>
        /// Creates and loads the rule set from a file
        /// </summary>
        /// <param name="filePath">The file path to load the rule set</param>
#nullable enable
        public static RuleSet LoadFromFile(string filePath)
        {
            // First read the file without doing any validation
            filePath = FileUtilities.NormalizeAbsolutePath(filePath);
            XmlReaderSettings settings = GetDefaultXmlReaderSettings();

            XDocument? ruleSetDocument = null;
            XElement? ruleSetNode = null;

            using (Stream stream = FileUtilities.OpenRead(filePath))
            using (XmlReader xmlReader = XmlReader.Create(stream, settings))
            {
                try
                {
                    ruleSetDocument = XDocument.Load(xmlReader);
                }
                catch (Exception e)
                {
                    throw new InvalidRuleSetException(e.Message);
                }

                // Find the top level rule set node
                List<XElement> nodeList = ruleSetDocument.Elements(RuleSetNodeName).ToList();
                Debug.Assert(nodeList.Count == 1, "Multiple top-level nodes!");
                Debug.Assert(nodeList[0].Name == RuleSetNodeName);
                ruleSetNode = nodeList[0];
            }

            return ReadRuleSet(ruleSetNode, filePath);
        }

        /// <summary>
        /// Load the rule set from the XML node
        /// </summary>
        /// <param name="ruleSetNode">The rule set node from which to create a rule set object</param>
        /// <param name="filePath">The file path to the rule set file</param>
        /// <returns>A rule set object with data from the given XML node</returns>
        private static RuleSet ReadRuleSet(XElement ruleSetNode, string filePath)
        {
            var specificOptions = ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>();
            var generalOption = ReportDiagnostic.Default;
            var includes = ImmutableArray.CreateBuilder<RuleSetInclude>();

            ValidateAttribute(ruleSetNode, RuleSetToolsVersionAttributeName);
            ValidateAttribute(ruleSetNode, RuleSetNameAttributeName);

            // Loop through each rules or include node in this rule set
            foreach (XElement childNode in ruleSetNode.Elements())
            {
                if (childNode.Name == RulesNodeName)
                {
                    var rules = ReadRules(childNode);
                    foreach (var rule in rules)
                    {
                        var ruleId = rule.Key;
                        var action = rule.Value;

                        ReportDiagnostic existingAction;
                        if (specificOptions.TryGetValue(ruleId, out existingAction))
                        {
                            if (existingAction != action)
                            {
                                throw new InvalidRuleSetException(string.Format(CodeAnalysisResources.RuleSetHasDuplicateRules, ruleId, existingAction, action));
                            }
                        }
                        else
                        {
                            specificOptions.Add(ruleId, action);
                        }
                    }
                }
                else if (childNode.Name == IncludeNodeName)
                {
                    includes.Add(ReadRuleSetInclude(childNode));
                }
                else if (childNode.Name == IncludeAllNodeName)
                {
                    generalOption = ReadIncludeAll(childNode);
                }
            }

            return new RuleSet(filePath, generalOption, specificOptions.ToImmutable(), includes.ToImmutable());
        }
#nullable disable

        /// <summary>
        /// Load the rules from the XML node
        /// </summary>
        /// <param name="rulesNode">The rules node from which to loop through each child rule node</param>
        /// <returns>A list of rule objects with data from the given XML node</returns>
        private static List<KeyValuePair<string, ReportDiagnostic>> ReadRules(XElement rulesNode)
        {
            _ = ReadNonEmptyAttribute(rulesNode, RulesAnalyzerIdAttributeName);
            _ = ReadNonEmptyAttribute(rulesNode, RulesNamespaceAttributeName);

            var rules = new List<KeyValuePair<string, ReportDiagnostic>>();

            // Loop through each rule node
            foreach (XElement ruleNode in rulesNode.Elements())
            {
                if (ruleNode.Name == RuleNodeName)
                {
                    rules.Add(ReadRule(ruleNode));
                }
                else
                {
                    // Schema validation should prevent us from getting here
                    Debug.Assert(false, "Unknown child node in Rules node");
                }
            }

            return rules;
        }

        /// <summary>
        /// Load the rule from the XML node
        /// </summary>
        /// <param name="ruleNode">The rule node from which to create a rule object</param>
        /// <returns>A rule object with data from the given XML node</returns>
        private static KeyValuePair<string, ReportDiagnostic> ReadRule(XElement ruleNode)
        {
            string ruleId = ReadNonEmptyAttribute(ruleNode, RuleIdAttributeName);
            ReportDiagnostic action = ReadAction(ruleNode, allowDefault: false);

            return new KeyValuePair<string, ReportDiagnostic>(ruleId, action);
        }

        /// <summary>
        /// Load the included rule set from the XML node
        /// </summary>
        /// <param name="includeNode">The include node from which to create a RuleSetInclude object</param>
        /// <returns>A RuleSetInclude object with data from the given XML node</returns>
        private static RuleSetInclude ReadRuleSetInclude(XElement includeNode)
        {
            string includePath = ReadNonEmptyAttribute(includeNode, IncludePathAttributeName);
            ReportDiagnostic action = ReadAction(includeNode, allowDefault: true);

            return new RuleSetInclude(includePath, action);
        }

        /// <summary>
        /// Reads the action from the given node
        /// </summary>
        /// <param name="node">The node to read the action, it can be a rule node or an include node.</param>
        /// <param name="allowDefault">Whether or not the default value is allowed.</param>
        /// <returns>The rule action</returns>
        private static ReportDiagnostic ReadAction(XElement node, bool allowDefault)
        {
            string action = ReadNonEmptyAttribute(node, RuleActionAttributeName);

            if (string.Equals(action, RuleActionWarningValue))
            {
                return ReportDiagnostic.Warn;
            }
            else if (string.Equals(action, RuleActionErrorValue))
            {
                return ReportDiagnostic.Error;
            }
            else if (string.Equals(action, RuleActionInfoValue))
            {
                return ReportDiagnostic.Info;
            }
            else if (string.Equals(action, RuleActionHiddenValue))
            {
                return ReportDiagnostic.Hidden;
            }
            else if (string.Equals(action, RuleActionNoneValue))
            {
                return ReportDiagnostic.Suppress;
            }
            else if (string.Equals(action, RuleActionDefaultValue) && allowDefault)
            {
                return ReportDiagnostic.Default;
            }

            throw new InvalidRuleSetException(string.Format(CodeAnalysisResources.RuleSetBadAttributeValue, RuleActionAttributeName, action));
        }

        /// <summary>
        /// Load the IncludedAll from the XML node
        /// </summary>
        /// <param name="includeAllNode">The IncludeAll node from which to create a IncludeAll object</param>
        /// <returns>A IncludeAll object with data from the given XML node</returns>
        private static ReportDiagnostic ReadIncludeAll(XElement includeAllNode)
        {
            return ReadAction(includeAllNode, allowDefault: false);
        }

        /// <summary>
        /// Reads an attribute from a node and validates that it is not empty.
        /// </summary>
        /// <param name="node">The XML node that contains the attribute</param>
        /// <param name="attributeName">The name of the attribute to read</param>
        /// <returns>The attribute value</returns>
        private static string ReadNonEmptyAttribute(XElement node, string attributeName)
        {
            XAttribute attribute = node.Attribute(attributeName);
            if (attribute == null)
            {
                throw new InvalidRuleSetException(string.Format(CodeAnalysisResources.RuleSetMissingAttribute, node.Name, attributeName));
            }

            if (string.IsNullOrEmpty(attribute.Value))
            {
                throw new InvalidRuleSetException(string.Format(CodeAnalysisResources.RuleSetBadAttributeValue, attributeName, attribute.Value));
            }

            return attribute.Value;
        }

        /// <summary>
        /// Gets the default settings to read the ruleset xml file.
        /// </summary>
        private static XmlReaderSettings GetDefaultXmlReaderSettings()
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();

            xmlReaderSettings.CheckCharacters = true;
            xmlReaderSettings.CloseInput = true;
            xmlReaderSettings.ConformanceLevel = ConformanceLevel.Document;
            xmlReaderSettings.IgnoreComments = true;
            xmlReaderSettings.IgnoreProcessingInstructions = true;
            xmlReaderSettings.IgnoreWhitespace = true;
            xmlReaderSettings.DtdProcessing = DtdProcessing.Prohibit;

            return xmlReaderSettings;
        }

        private static void ValidateAttribute(XElement node, string attributeName)
        {
            ReadNonEmptyAttribute(node, attributeName);
        }
    }
}
