// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(IParseOptionsService), LanguageNames.CSharp), Shared]
    internal class CSharpParseOptionsService : IParseOptionsService
    {
        public string GetLanguageVersion(ParseOptions options)
        {
            return ((CSharpParseOptions)options).LanguageVersion.Display();
        }

        public bool CanApplyChange(ParseOptions old, ParseOptions @new)
        {
            // only changes to C# LanguageVersion are supported at this point
            if (old is CSharpParseOptions oldCsharp && @new is CSharpParseOptions newCsharp)
            {
                return oldCsharp.WithLanguageVersion(newCsharp.LanguageVersion) == newCsharp;
            }
            return false;
        }
    }
}
