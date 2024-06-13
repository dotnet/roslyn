// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AbstractDiagnosticsAdornmentTaggerProvider<TTag>
    {
        protected sealed class RoslynErrorTag(string errorType, Workspace workspace, DiagnosticData data) : ErrorTag(errorType, CreateToolTipContent(workspace, data)), IEquatable<RoslynErrorTag>
        {
            private readonly DiagnosticData _data = data;

            private static object CreateToolTipContent(Workspace workspace, DiagnosticData diagnostic)
            {
                Action? navigationAction = null;
                string? tooltip = null;
                if (workspace != null)
                {
                    var helpLinkUri = diagnostic.GetValidHelpLinkUri();
                    if (helpLinkUri != null)
                    {
                        navigationAction = new QuickInfoHyperLink(workspace, helpLinkUri).NavigationAction;
                        tooltip = diagnostic.HelpLink;
                    }
                }

                var diagnosticIdTextRun = navigationAction is null
                    ? new ClassifiedTextRun(ClassificationTypeNames.Text, diagnostic.Id)
                    : new ClassifiedTextRun(ClassificationTypeNames.Text, diagnostic.Id, navigationAction, tooltip);

                return new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ClassifiedTextElement(
                        diagnosticIdTextRun,
                        new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                        new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        new ClassifiedTextRun(ClassificationTypeNames.Text, diagnostic.Message)));
            }

            public override bool Equals(object? obj)
                => Equals(obj as RoslynErrorTag);

            public bool Equals(RoslynErrorTag? other)
            {
                return other != null &&
                    this.ErrorType == other.ErrorType &&
                    this._data.HelpLink == other._data.HelpLink &&
                    this._data.Id == other._data.Id &&
                    this._data.Message == other._data.Message;
            }

            // Intentionally throwing, we have never supported this facility, and there is no contract around placing
            // these tags in sets or maps.
            public override int GetHashCode()
                => throw new NotImplementedException();
        }
    }
}
