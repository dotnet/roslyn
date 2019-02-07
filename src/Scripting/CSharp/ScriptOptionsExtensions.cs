// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting
{
    public static class ScriptOptionsExtensions
    {
        public static ScriptOptions WithLanguageVersion(this ScriptOptions options, LanguageVersion languageVersion)
        {
            return options.WithParseOptions(new CSharpParseOptions(kind: SourceCodeKind.Script, languageVersion: languageVersion));
        }
    }
}
