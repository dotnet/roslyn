// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    partial class NamingRuleTreeItemViewModel : ITreeDisplayItemWithImages
    {
        public ImageMoniker ExpandedIconMoniker => GetMoniker();

        public FontStyle FontStyle => FontStyles.Normal;

        public FontWeight FontWeight => FontWeights.Normal;

        public ImageMoniker IconMoniker => GetMoniker();

        public bool IsCut => false;

        public ImageMoniker OverlayIconMoniker => default(ImageMoniker);

        public ImageMoniker StateIconMoniker => default(ImageMoniker);

        public string StateToolTipText => null;

        public string Text => Title;

        public object ToolTipContent => null;

        public string ToolTipText => null;

        private ImageMoniker GetMoniker()
        {
            if (EnforcementLevel == null)
            {
                return KnownMonikers.FolderOpened;
            }

            switch (EnforcementLevel.Value)
            {
                case CodeAnalysis.DiagnosticSeverity.Hidden:
                    return KnownMonikers.None;
                case CodeAnalysis.DiagnosticSeverity.Info:
                    return KnownMonikers.StatusInformation;
                case CodeAnalysis.DiagnosticSeverity.Warning:
                    return KnownMonikers.StatusWarning;
                case CodeAnalysis.DiagnosticSeverity.Error:
                    return KnownMonikers.StatusError;
                default:
                    break;
            }

            return KnownMonikers.Rule;
        }
    }
}
