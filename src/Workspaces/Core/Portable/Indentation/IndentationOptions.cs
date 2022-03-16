﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Indentation
{
    [DataContract]
    internal readonly record struct IndentationOptions(
        [property: DataMember(Order = 0)] SyntaxFormattingOptions FormattingOptions,
        [property: DataMember(Order = 1)] AutoFormattingOptions AutoFormattingOptions);
}
