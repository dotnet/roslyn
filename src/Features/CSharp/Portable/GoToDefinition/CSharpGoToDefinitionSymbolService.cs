﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToDefinitionSymbolService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToDefinitionSymbolService : AbstractGoToDefinitionSymbolService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGoToDefinitionSymbolService()
        {
        }

        protected override ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation)
            => symbol;
    }
}
