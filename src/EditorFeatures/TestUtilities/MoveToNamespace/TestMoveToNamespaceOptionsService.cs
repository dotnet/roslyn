// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias WORKSPACES;

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveToNamespace;
using WORKSPACES::Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    [Export(typeof(IMoveToNamespaceOptionsService))]
    [PartNotDiscoverable]
    class TestMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        public static readonly string NamespaceValue = "TestNewNamespaceValue";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestMoveToNamespaceOptionsService()
        {
        }

        public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(string defaultNamespace, ImmutableArray<string> availableNamespaces, ISyntaxFactsService syntaxFactsService)
            => new MoveToNamespaceOptionsResult(NamespaceValue);
    }
}
