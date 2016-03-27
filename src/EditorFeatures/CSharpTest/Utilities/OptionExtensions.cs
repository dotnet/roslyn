// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    internal static class OptionExtensions
    {
        internal static IDictionary<OptionKey, object> With(this IDictionary<OptionKey, object> options, OptionKey option, object value)
        {
            options.Add(option, value);
            return options;
        }
    }
}
