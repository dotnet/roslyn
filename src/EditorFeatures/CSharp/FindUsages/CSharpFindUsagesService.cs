// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.FindUsages
{
    [ExportLanguageService(typeof(IFindUsagesService), LanguageNames.CSharp), Shared]
    internal class CSharpFindUsagesService : AbstractFindUsagesService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpFindUsagesService(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }
    }
}
