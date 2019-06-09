// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface
{
    [ExportWorkspaceService(typeof(IExtractInterfaceOptionsService), ServiceLayer.Default), Shared]
    internal class TestExtractInterfaceOptionsService : IExtractInterfaceOptionsService
    {
        [ImportingConstructor]
        public TestExtractInterfaceOptionsService()
        {
        }

        public IEnumerable<ISymbol> AllExtractableMembers { get; private set; }
        public string DefaultInterfaceName { get; private set; }
        public List<string> ConflictingTypeNames { get; private set; }
        public string DefaultNamespace { get; private set; }
        public string GeneratedNameTypeParameterSuffix { get; set; }

        public bool IsCancelled { get; set; }
        public string ChosenInterfaceName { get; set; }
        public string ChosenFileName { get; set; }
        public IEnumerable<ISymbol> ChosenMembers { get; set; }
        public bool SameFile { get; set; }

        public Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            ISyntaxFactsService syntaxFactsService,
            INotificationService notificationService,
            List<ISymbol> extractableMembers,
            string defaultInterfaceName,
            List<string> conflictingTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName)
        {
            this.AllExtractableMembers = extractableMembers;
            this.DefaultInterfaceName = defaultInterfaceName;
            this.ConflictingTypeNames = conflictingTypeNames;
            this.DefaultNamespace = defaultNamespace;
            this.GeneratedNameTypeParameterSuffix = generatedNameTypeParameterSuffix;

            var result = IsCancelled
                ? ExtractInterfaceOptionsResult.Cancelled
                : new ExtractInterfaceOptionsResult(
                    isCancelled: false,
                    includedMembers: (ChosenMembers ?? AllExtractableMembers).AsImmutable(),
                    interfaceName: ChosenInterfaceName ?? defaultInterfaceName,
                    fileName: ChosenFileName ?? defaultInterfaceName,
                    location: SameFile ? ExtractInterfaceOptionsResult.ExtractLocation.SameFile : ExtractInterfaceOptionsResult.ExtractLocation.NewFile);

            return Task.FromResult(result);
        }
    }
}
