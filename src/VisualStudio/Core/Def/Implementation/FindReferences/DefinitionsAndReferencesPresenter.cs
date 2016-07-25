// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    [Export(typeof(IDefinitionsAndReferencesPresenter))]
    internal sealed class DefinitionsAndReferencesPresenter : ForegroundThreadAffinitizedObject, IDefinitionsAndReferencesPresenter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LibraryManager _manager;

        [ImportingConstructor]
        private DefinitionsAndReferencesPresenter(SVsServiceProvider serviceProvider) :
            base(assertIsForeground: true)
        {
            _serviceProvider = serviceProvider;

            // VS service should only be used in UI thread.
            _manager = (LibraryManager)serviceProvider.GetService(typeof(LibraryManager));
        }

        public void DisplayResult(DefinitionsAndReferences definitionsAndReferences)
        {
            _manager.PresentDefinitionsAndReferences(definitionsAndReferences);
        }
    }
}