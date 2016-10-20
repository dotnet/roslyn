// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal interface INamingStyle
    {
        Guid ID { get; }
        string CreateName(IEnumerable<string> words);
        IEnumerable<string> MakeCompliant(string name);
        bool IsNameCompliant(string name, out string failureReason);
    }
}