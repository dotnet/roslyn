// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    [Export(typeof(IFindReferencesPresenter))]
    internal sealed class FindReferencesPresenter : ForegroundThreadAffinitizedObject, IFindReferencesPresenter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LibraryManager _manager;

        [ImportingConstructor]
        private FindReferencesPresenter(SVsServiceProvider serviceProvider) :
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