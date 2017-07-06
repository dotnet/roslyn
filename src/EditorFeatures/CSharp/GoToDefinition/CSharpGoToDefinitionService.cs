﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToDefinitionService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToDefinitionService : AbstractGoToDefinitionService
    {
        [ImportingConstructor]
        public CSharpGoToDefinitionService(
            [ImportMany]IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters) 
            : base(streamingPresenters)
        {
        }

        protected override ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation)
        {
            return symbol;
        }
    }
}
