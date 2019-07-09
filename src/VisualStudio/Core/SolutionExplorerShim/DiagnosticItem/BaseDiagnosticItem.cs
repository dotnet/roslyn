// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal abstract partial class BaseDiagnosticItem : BaseItem
    {
        protected readonly DiagnosticDescriptor _descriptor;
        protected ReportDiagnostic _effectiveSeverity;

        public override event PropertyChangedEventHandler PropertyChanged;


        public BaseDiagnosticItem(DiagnosticDescriptor descriptor, ReportDiagnostic effectiveSeverity)
            : base(string.Format("{0}: {1}", descriptor.Id, descriptor.Title))
        {
            _descriptor = descriptor;
            _effectiveSeverity = effectiveSeverity;
        }

        public override ImageMoniker IconMoniker
        {
            get
            {
                return MapEffectiveSeverityToIconMoniker(_effectiveSeverity);
            }
        }

        protected abstract Microsoft.CodeAnalysis.Workspace Workspace { get; }
        public abstract ProjectId ProjectId { get; }
        protected abstract AnalyzerReference AnalyzerReference { get; }

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

        public Uri GetHelpLink()
        {
            if (BrowserHelper.TryGetUri(Descriptor.HelpLinkUri, out var link))
            {
                return link;
            }

            if (!string.IsNullOrWhiteSpace(Descriptor.Id))
            {
                Workspace.GetLanguageAndProjectType(ProjectId, out var language, out var projectType);

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
            => effectiveSeverity switch
            {
                ReportDiagnostic.Error => KnownMonikers.CodeErrorRule,
                ReportDiagnostic.Warn => KnownMonikers.CodeWarningRule,
                ReportDiagnostic.Info => KnownMonikers.CodeInformationRule,
                ReportDiagnostic.Hidden => KnownMonikers.CodeHiddenRule,
                ReportDiagnostic.Suppress => KnownMonikers.CodeSuppressedRule,
                _ => default,
            };

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

            ruleSetDocument.SetSeverity(AnalyzerReference.Display, _descriptor.Id, value);

            ruleSetDocument.Save(pathToRuleSet);
        }
    }
}
