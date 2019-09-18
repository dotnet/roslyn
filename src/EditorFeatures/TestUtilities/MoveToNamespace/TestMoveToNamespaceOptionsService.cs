// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    [Export(typeof(IMoveToNamespaceOptionsService)), Shared]
    [PartNotDiscoverable]
    class TestMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        private MoveToNamespaceOptionsResult OptionsResult { get; set; }

        [ImportingConstructor]
        public TestMoveToNamespaceOptionsService()
        {
        }

        public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(string defaultNamespace, ImmutableArray<string> availableNamespaces, ISyntaxFactsService syntaxFactsService)
            => OptionsResult;

        internal void SetOptions(MoveToNamespaceOptionsResult moveToNamespaceOptions)
            => OptionsResult = moveToNamespaceOptions;
    }
}
