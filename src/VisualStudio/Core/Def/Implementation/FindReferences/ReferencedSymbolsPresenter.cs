// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    [Export(typeof(IReferencedSymbolsPresenter))]
    internal sealed class ReferencedSymbolsPresenter : ForegroundThreadAffinitizedObject, IReferencedSymbolsPresenter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LibraryManager _manager;

        [ImportingConstructor]
        private ReferencedSymbolsPresenter(SVsServiceProvider serviceProvider) :
            base(assertIsForeground: true)
        {
            _serviceProvider = serviceProvider;

            // VS service should only be used in UI thread.
            _manager = (LibraryManager)serviceProvider.GetService(typeof(LibraryManager));
        }

        public void DisplayResult(Solution solution, IEnumerable<ReferencedSymbol> symbols)
        {
            var firstResult = symbols.FirstOrDefault();
            string title;
            if (firstResult != null)
            {
                title = firstResult.Definition.Name;
                if (title == string.Empty)
                {
                    // Anonymous types have no name.
                    title = firstResult.Definition.ToDisplayString();
                }
            }
            else
            {
                // PresentFindReferencesResult ignores the title for an empty result, but "VS library" system throws if it is an empty string.
                title = "None";
            }

            _manager.PresentReferencedSymbols(title, solution, symbols);
        }
    }
}
