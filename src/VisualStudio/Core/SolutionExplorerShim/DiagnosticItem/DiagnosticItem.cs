// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
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
                string language;
                string projectType;
                _analyzerItem.AnalyzersFolder.Workspace.GetLanguageAndProjectType(_analyzerItem.AnalyzersFolder.ProjectId, out language, out projectType);

                // we use message format here since we don't have actual instance of diagnostic here. 
                // (which means we do not have a message)
                return BrowserHelper.CreateBingQueryUri(Descriptor.Id, Descriptor.GetBingHelpMessage(), language, projectType);
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void SetSeverity(ReportDiagnostic value, string pathToRuleSet)
        {
            UpdateRuleSetFile(pathToRuleSet, value);
        }

        private void UpdateRuleSetFile(string pathToRuleSet, ReportDiagnostic value)
        {
            var ruleSetDocument = XDocument.Load(pathToRuleSet);

            ruleSetDocument.SetSeverity(_analyzerItem.AnalyzerReference.Display, _descriptor.Id, value);

            ruleSetDocument.Save(pathToRuleSet);
        }
    }
}
