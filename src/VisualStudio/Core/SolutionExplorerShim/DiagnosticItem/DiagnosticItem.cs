// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class DiagnosticItem : BaseItem
    {
        private readonly DiagnosticDescriptor _descriptor;
        private ReportDiagnostic _effectiveSeverity;
        private readonly AnalyzerItem _analyzerItem;
        private readonly IContextMenuController _contextMenuController;

        public override event PropertyChangedEventHandler PropertyChanged;

        public DiagnosticItem(AnalyzerItem analyzerItem, DiagnosticDescriptor descriptor, ReportDiagnostic effectiveSeverity, IContextMenuController contextMenuController)
            : base(string.Format("{0}: {1}", descriptor.Id, descriptor.Title))
        {
            _analyzerItem = analyzerItem;
            _descriptor = descriptor;
            _effectiveSeverity = effectiveSeverity;
            _contextMenuController = contextMenuController;
        }

        public override ImageMoniker IconMoniker
        {
            get
            {
                return MapEffectiveSeverityToIconMoniker(_effectiveSeverity);
            }
        }

        public AnalyzerItem AnalyzerItem
        {
            get
            {
                return _analyzerItem;
            }
        }

        public DiagnosticDescriptor Descriptor
        {
            get
            {
                return _descriptor;
            }
        }

        public ReportDiagnostic EffectiveSeverity
        {
            get
            {
                return _effectiveSeverity;
            }
        }

        public override object GetBrowseObject()
        {
            return new BrowseObject(this);
        }

        public override IContextMenuController ContextMenuController
        {
            get
            {
                return _contextMenuController;
            }
        }

        public Uri GetHelpLink()
        {
            Uri link;
            if (BrowserHelper.TryGetUri(Descriptor.HelpLinkUri, out link))
            {
                return link;
            }

            if (!string.IsNullOrWhiteSpace(Descriptor.Id))
            {
                // we use message format here since we don't have actual instance of diagnostic here. 
                // (which means we do not have a message)
                return BrowserHelper.CreateBingQueryUri(Descriptor.Id, Descriptor.MessageFormat.ToString(DiagnosticData.USCultureInfo));
            }

            return null;
        }

        internal void UpdateEffectiveSeverity(ReportDiagnostic newEffectiveSeverity)
        {
            if (_effectiveSeverity != newEffectiveSeverity)
            {
                _effectiveSeverity = newEffectiveSeverity;

                NotifyPropertyChanged(nameof(EffectiveSeverity));
                NotifyPropertyChanged(nameof(IconMoniker));
            }
        }

        private ImageMoniker MapEffectiveSeverityToIconMoniker(ReportDiagnostic effectiveSeverity)
        {
            switch (effectiveSeverity)
            {
                case ReportDiagnostic.Error:
                    return KnownMonikers.CodeErrorRule;
                case ReportDiagnostic.Warn:
                    return KnownMonikers.CodeWarningRule;
                case ReportDiagnostic.Info:
                    return KnownMonikers.CodeInformationRule;
                case ReportDiagnostic.Hidden:
                    return KnownMonikers.CodeHiddenRule;
                case ReportDiagnostic.Suppress:
                    return KnownMonikers.CodeSuppressedRule;
                default:
                    return default(ImageMoniker);
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        internal void SetSeverity(ReportDiagnostic value, string pathToRuleSet)
        {
            UpdateRuleSetFile(pathToRuleSet, value);
        }

        private void UpdateRuleSetFile(string pathToRuleSet, ReportDiagnostic value)
        {
            var ruleSetDocument = XDocument.Load(pathToRuleSet);

            var newAction = ConvertReportDiagnosticToAction(value);

            var analyzerID = _analyzerItem.AnalyzerReference.Display;
            var rules = FindOrCreateRulesElement(ruleSetDocument, analyzerID);
            var rule = FindOrCreateRuleElement(rules, _descriptor.Id);
            rule.Attribute("Action").Value = newAction;

            var allMatchingRules = ruleSetDocument.Root
                                   .Descendants("Rule")
                                   .Where(r => r.Attribute("Id").Value.Equals(_descriptor.Id));

            foreach (var matchingRule in allMatchingRules)
            {
                matchingRule.Attribute("Action").Value = newAction;
            }

            ruleSetDocument.Save(pathToRuleSet);
        }

        private XElement FindOrCreateRuleElement(XElement rules, string id)
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

        private XElement FindOrCreateRulesElement(XDocument ruleSetDocument, string analyzerID)
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
                    throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
