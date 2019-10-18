// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace RulesetToEditorconfigConverter
{
    public static class Converter
    {
        private const string RuleSetNodeName = "RuleSet";
        private const string RuleSetNameAttributeName = "Name";
        private const string RuleSetDescriptionAttributeName = "Description";
        private const string RulesNodeName = "Rules";
        private const string RuleNodeName = "Rule";
        private const string RuleIdAttributeName = "Id";

        private static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                ShowUsage();
                return 1;
            }

            var rulesetFilePath = args[0];
            var editorconfigFilePath = args.Length == 2 ?
                args[1] :
                Path.Combine(Environment.CurrentDirectory, ".editorconfig");
            try
            {
                GenerateEditorconfig(rulesetFilePath, editorconfigFilePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                return 2;
            }

            Console.WriteLine($"Successfully converted to '{editorconfigFilePath}'");
            return 0;

            static void ShowUsage()
            {
                Console.WriteLine("Usage: dotnet.exe RulesetToEditorconfigConverter.dll <%ruleset_file%> [<%path_to_editorconfig%>]");
                return;
            }
        }

        /// <summary>
        /// Converts a ruleset file at the given <paramref name="rulesetFilePath"/> into an
        /// .editorconfig at the given <paramref name="editorconfigFilePath"/>
        /// </summary>
        /// <exception cref="IOException">Exception while performing any I/O on given file paths.</exception>
        /// <exception cref="InvalidRuleSetException">Exception for invalid ruleset files.</exception>
        public static void GenerateEditorconfig(string rulesetFilePath, string editorconfigFilePath)
        {
            if (Directory.Exists(editorconfigFilePath))
            {
                editorconfigFilePath = Path.Combine(editorconfigFilePath, ".editorconfig");
            }

            // Find the top level rule set node
            var rulesetNode = GetTopLevelRulesetNode(rulesetFilePath);
            var name = rulesetNode.Attribute(RuleSetNameAttributeName)?.Value ?? Path.GetFileName(rulesetFilePath);
            var description = rulesetNode.Attribute(RuleSetDescriptionAttributeName)?.Value ?? Path.GetFileName(rulesetFilePath);

            var builder = new StringBuilder();
            builder.AppendLine(@"# NOTE: Requires **VS2019 16.3** or later");
            builder.AppendLine();
            builder.AppendLine($@"# {name}");
            builder.AppendLine($@"# Description: {description}");
            builder.AppendLine();
            builder.AppendLine(@"# Code files");
            builder.AppendLine(@"[*.{cs,vb}]");
            builder.AppendLine();

            var ruleset = RuleSet.LoadEffectiveRuleSetFromFile(rulesetFilePath);
            var uniqueRulesetPaths = new HashSet<string>();
            var ruleIdToComments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ProcessComments(ruleset, uniqueRulesetPaths, ruleIdToComments);

            if (ruleset.GeneralDiagnosticOption != ReportDiagnostic.Default)
            {
                builder.AppendLine();
                builder.AppendLine(@"# Default severity for analyzer diagnostics - Requires **VS2019 16.5** or later");
                builder.AppendLine($@"dotnet_analyzer_diagnostic.severity = {ruleset.GeneralDiagnosticOption.ToAnalyzerConfigString()}");
            }

            foreach (var kvp in ruleset.SpecificDiagnosticOptions.OrderBy(kvp => kvp.Key))
            {
                var id = kvp.Key;
                var severity = kvp.Value;

                builder.AppendLine();

                if (ruleIdToComments.TryGetValue(id, out var comment))
                {
                    AppendComment(builder, comment);
                }

                builder.AppendLine($@"dotnet_diagnostic.{id}.severity = {severity.ToAnalyzerConfigString()}");
            }

            File.WriteAllText(editorconfigFilePath, builder.ToString());
            return;

            static XElement GetTopLevelRulesetNode(string rulesetFilePath)
            {
                using Stream stream = File.OpenRead(rulesetFilePath);
                using XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings() { XmlResolver = null });
                var ruleSetDocument = XDocument.Load(xmlReader);

                // Find the top level rule set node
                var rulesetNode = ruleSetDocument.Elements(RuleSetNodeName).FirstOrDefault();
                Debug.Assert(rulesetNode.Name == RuleSetNodeName);
                return rulesetNode;
            }

            static void ProcessComments(RuleSet ruleset, HashSet<string> processedRulesetPaths, Dictionary<string, string> ruleIdToComments)
            {
                processedRulesetPaths.Add(ruleset.FilePath);

                foreach (string rulesetIncludePath in RuleSet.GetEffectiveIncludesFromFile(ruleset.FilePath))
                {
                    if (!processedRulesetPaths.Contains(rulesetIncludePath))
                    {
                        RuleSet includedRuleset = RuleSet.LoadEffectiveRuleSetFromFile(rulesetIncludePath);
                        ProcessComments(includedRuleset, processedRulesetPaths, ruleIdToComments);
                    }
                }

                PopulateRuleIdToComments(ruleset.FilePath, ruleIdToComments);
            }

            static void PopulateRuleIdToComments(string rulesetFilePath, Dictionary<string, string> ruleIdToComments)
            {
                // Find the top level rule set node
                var rulesetNode = GetTopLevelRulesetNode(rulesetFilePath);
                if (rulesetNode == null)
                {
                    return;
                }

                Debug.Assert(rulesetNode.Name == RuleSetNodeName);
                var currentXmlComment = new StringBuilder();
                string currentRuleId = null;
                foreach (var childNode in rulesetNode.Nodes().OfType<XElement>())
                {
                    if (childNode.Name != RulesNodeName)
                    {
                        currentXmlComment.Clear();
                        currentRuleId = null;
                        continue;
                    }

                    foreach (var node in childNode.Nodes())
                    {
                        if (node is XElement ruleNode &&
                            ruleNode.Name == RuleNodeName)
                        {
                            XAttribute ruleId = ruleNode.Attribute(RuleIdAttributeName);
                            if (ruleId != null)
                            {
                                foreach (var comment in ruleNode.Nodes().OfType<XComment>())
                                {
                                    AppendComment(comment);
                                }

                                currentRuleId = ruleId.Value;
                            }
                        }
                        else if (node is XComment xComment)
                        {
                            AppendComment(xComment);
                        }
                        else if (node is XText xtext)
                        {
                            if (xtext.Value.Contains("\r") || xtext.Value.Contains("\n"))
                            {
                                // Indicates start of new Rule/XmlComment.
                                UpdateCurrentRuleIdPostCommentAndResetState();
                            }
                        }
                        else
                        {
                            currentXmlComment.Clear();
                        }
                    }
                }

                UpdateCurrentRuleIdPostCommentAndResetState();
                return;

                void AppendComment(XComment comment)
                {
                    if (currentXmlComment.Length > 0)
                    {
                        currentXmlComment.AppendLine();
                    }

                    currentXmlComment.Append(comment.Value);
                }

                // Saves the current comment as a post comment for current rule ID
                // and reset the current rule ID and current comment.
                void UpdateCurrentRuleIdPostCommentAndResetState()
                {
                    if (currentRuleId != null)
                    {
                        ruleIdToComments[currentRuleId] = currentXmlComment.ToString();
                        currentXmlComment.Clear();
                        currentRuleId = null;
                    }
                }
            }

            static void AppendComment(StringBuilder builder, string comment)
            {
                if (comment.Length > 0)
                {
                    foreach (var commentPart in comment.Split('\r', '\n'))
                    {
                        var trimmedCommentPart = commentPart.Trim();
                        if (trimmedCommentPart.Length > 0)
                        {
                            builder.AppendLine($@"# {trimmedCommentPart}");
                        }
                    }
                }
            }
        }
    }
}
