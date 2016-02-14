// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
