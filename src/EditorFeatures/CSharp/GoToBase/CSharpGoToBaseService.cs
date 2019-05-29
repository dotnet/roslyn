// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.GoToBase;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToBase
{
    [ExportLanguageService(typeof(IGoToBaseService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToBaseService : AbstractGoToBaseService
    {
        [ImportingConstructor]
        public CSharpGoToBaseService()
        {
        }
    }
}
