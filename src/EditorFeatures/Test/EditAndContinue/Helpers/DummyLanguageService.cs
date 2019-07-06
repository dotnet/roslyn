// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal interface IDummyLanguageService : ILanguageService { }

    [ExportLanguageService(typeof(IDummyLanguageService), LanguageName), Shared]
    internal class DummyLanguageService : IDummyLanguageService
    {
        public const string LanguageName = "Dummy";

        [ImportingConstructor]
        public DummyLanguageService()
        {
        }

        // do nothing

    }
}
