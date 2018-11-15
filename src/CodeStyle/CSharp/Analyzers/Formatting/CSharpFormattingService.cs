// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class CSharpFormattingService : AbstractFormattingService
    {
        public static readonly CSharpFormattingService Instance = new CSharpFormattingService();
    }
}
