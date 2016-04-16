// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.GoToDefinition
{
    [Export(typeof(INavigableItemsPresenter))]
    internal class NavigableItemsPresenter : INavigableItemsPresenter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LibraryManager _manager;

        [ImportingConstructor]
        private NavigableItemsPresenter(
            SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _manager = (LibraryManager)serviceProvider.GetService(typeof(LibraryManager));
        }

        public void DisplayResult(string title, IEnumerable<INavigableItem> items)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            _manager.PresentNavigableItems(title, items);
        }
    }
}
