﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
