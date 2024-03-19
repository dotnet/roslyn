// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    [Export(typeof(IMoveToNamespaceOptionsService)), Shared]
    [PartNotDiscoverable]
    internal class TestMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        private MoveToNamespaceOptionsResult OptionsResult { get; set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestMoveToNamespaceOptionsService()
        {
        }

        public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(string defaultNamespace, ImmutableArray<string> availableNamespaces, ISyntaxFacts syntaxFactsService)
            => OptionsResult;

        internal void SetOptions(MoveToNamespaceOptionsResult moveToNamespaceOptions)
            => OptionsResult = moveToNamespaceOptions;
    }
}
