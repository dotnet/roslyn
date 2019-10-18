// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.Configuration;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal abstract partial class BaseDiagnosticItem : BaseItem
    {
        public string Language { get; }
        public DiagnosticDescriptor Descriptor { get; }
        public ReportDiagnostic EffectiveSeverity { get; private set; }

        public override event PropertyChangedEventHandler PropertyChanged;

        public BaseDiagnosticItem(DiagnosticDescriptor descriptor, ReportDiagnostic effectiveSeverity, string language)
            : base(descriptor.Id + ": " + descriptor.Title)
        {
            Descriptor = descriptor;
            EffectiveSeverity = effectiveSeverity;
            Language = language;
        }

        public override ImageMoniker IconMoniker
            => MapEffectiveSeverityToIconMoniker(EffectiveSeverity);

        public abstract ProjectId ProjectId { get; }
        protected abstract AnalyzerReference AnalyzerReference { get; }

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
                // we use message format here since we don't have actual instance of diagnostic here. 
                // (which means we do not have a message)
                return BrowserHelper.CreateBingQueryUri(Descriptor, Language);
            }

            return null;
        }

        internal void UpdateEffectiveSeverity(ReportDiagnostic newEffectiveSeverity)
        {
            if (EffectiveSeverity != newEffectiveSeverity)
            {
                EffectiveSeverity = newEffectiveSeverity;

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

        internal void SetRuleSetSeverity(ReportDiagnostic value, string pathToRuleSet)
        {
            var ruleSetDocument = XDocument.Load(pathToRuleSet);

            ruleSetDocument.SetSeverity(AnalyzerReference.Display, Descriptor.Id, value);

            ruleSetDocument.Save(pathToRuleSet);
        }

        internal Task<Solution> GetSolutionWithUpdatedAnalyzerConfigSeverityAsync(ReportDiagnostic value, Project project, CancellationToken cancellationToken)
        {
            var effectiveSeverity = value.ToDiagnosticSeverity() ?? Descriptor.DefaultSeverity;
            var diagnostic = Diagnostic.Create(Descriptor, Location.None, effectiveSeverity, additionalLocations: null, properties: null);
            return ConfigurationUpdater.ConfigureSeverityAsync(value, diagnostic, project, cancellationToken);
        }
    }
}
