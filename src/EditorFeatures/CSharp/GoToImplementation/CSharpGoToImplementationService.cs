// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.GoToImplementation;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToImplementation
{
    [ExportLanguageService(typeof(IGoToImplementationService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpGoToImplementationService : AbstractGoToImplementationService
    {
        [ImportingConstructor]
        public CSharpGoToImplementationService(
            [ImportMany]IEnumerable<Lazy<INavigableItemsPresenter>> presenters,
            [ImportMany]IEnumerable<Lazy<INavigableDefinitionProvider>> externalDefinitionProviders) : base(presenters, externalDefinitionProviders)
        {
        }
    }
}
