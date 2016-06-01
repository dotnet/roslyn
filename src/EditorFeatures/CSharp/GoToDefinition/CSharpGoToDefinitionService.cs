// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToDefinitionService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToDefinitionService : AbstractGoToDefinitionService
    {
        [ImportingConstructor]
        public CSharpGoToDefinitionService(
            [ImportMany]IEnumerable<Lazy<INavigableItemsPresenter>> presenters,
            [ImportMany]IEnumerable<Lazy<INavigableDefinitionProvider>> externalDefinitionProviders) : base(presenters, externalDefinitionProviders)
        {
        }

        protected override ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation)
        {
            return symbol;
        }
    }
}
