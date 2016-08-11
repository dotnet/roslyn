// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo.Presentation
{
    [ExportQuickInfoPresentationProvider(QuickInfoElementKinds.Symbol)]
    internal class GroupPresentationProvider : QuickInfoPresentationProvider
    {
        private readonly ImmutableArray<Lazy<QuickInfoPresentationProvider, QuickInfoPresentationProviderInfo>> _presentationProviders;

        [ImportingConstructor]
        public GroupPresentationProvider(
            IEnumerable<Lazy<QuickInfoPresentationProvider, QuickInfoPresentationProviderInfo>> presentationProviders)
        {
            _presentationProviders = presentationProviders.ToImmutableArray();
        }

        public override FrameworkElement CreatePresentation(QuickInfoElement element, ITextSnapshot snapshot)
        {
            FrameworkElement symbol = null;
            FrameworkElement warning = null;
            FrameworkElement description = null;
            FrameworkElement documentation = null;
            FrameworkElement typeParameterMap = null;
            FrameworkElement anonymousTypes = null;
            FrameworkElement usage = null;
            FrameworkElement exception = null;
            List<FrameworkElement> other = null;

            foreach (var e in element.Elements)
            {
                var content = CreateContent(e, snapshot);
                switch (e.Kind)
                {
                    case QuickInfoElementKinds.Symbol:
                        symbol = content;
                        break;
                    case QuickInfoElementKinds.Warning:
                        warning = content;
                        break;
                    case QuickInfoElementKinds.Description:
                        description = content;
                        break;
                    case QuickInfoElementKinds.Documentation:
                        documentation = content;
                        break;
                    case QuickInfoElementKinds.TypeParameterMap:
                        typeParameterMap = content;
                        break;
                    case QuickInfoElementKinds.AnonymousTypes:
                        anonymousTypes = content;
                        break;
                    case QuickInfoElementKinds.Usage:
                        usage = content;
                        break;
                    case QuickInfoElementKinds.Exception:
                        exception = content;
                        break;

                    default:
                        if (other == null)
                        {
                            other = new List<FrameworkElement>();
                        }

                        other.Add(content);
                        break;
                }
            }

            return new QuickInfoDisplayPanel(
                symbolGlyph: symbol,
                warningGlyph: warning,
                mainDescription: description,
                documentation: documentation,
                typeParameterMap: typeParameterMap,
                anonymousTypes: anonymousTypes,
                usageText: usage,
                exceptionText: exception,
                other: other);
        }

        private FrameworkElement CreateContent(QuickInfoElement element, ITextSnapshot snapshot)
        {
            QuickInfoPresentationProvider provider;

            if (_presentationProviders.TryGetValue(element.Kind, out provider)
                || _presentationProviders.TryGetValue(QuickInfoElementKinds.Text, out provider))
            {
                return provider.CreatePresentation(element, snapshot);
            }

            return null;
        }
    }
}