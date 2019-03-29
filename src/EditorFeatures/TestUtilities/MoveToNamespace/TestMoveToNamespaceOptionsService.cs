// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    [ExportWorkspaceService(typeof(IMoveToNamespaceOptionsService)), Shared]
    class TestMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        internal static readonly MoveToNamespaceOptionsResult DefaultOptions = new MoveToNamespaceOptionsResult("TestNewNamespaceValue");

        private MoveToNamespaceOptionsResult _optionsResult = DefaultOptions;

        public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(string defaultNamespace, ImmutableArray<string> availableNamespaces, ISyntaxFactsService syntaxFactsService)
            => _optionsResult;

        internal void SetOptions(MoveToNamespaceOptionsResult moveToNamespaceOptions)
            => _optionsResult = moveToNamespaceOptions;
    }
}
