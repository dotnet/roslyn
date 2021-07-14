// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    public sealed class CSharpObjectFormatter : ObjectFormatter
    {
        public static CSharpObjectFormatter Instance { get; } = new CSharpObjectFormatter();

        private static readonly ObjectFormatter s_impl = new CSharpObjectFormatterImpl();

        private CSharpObjectFormatter()
        {
        }

        public override string FormatObject(object obj, PrintOptions options) => s_impl.FormatObject(obj, options);

        public override string FormatException(Exception e) => s_impl.FormatException(e);
    }
}
