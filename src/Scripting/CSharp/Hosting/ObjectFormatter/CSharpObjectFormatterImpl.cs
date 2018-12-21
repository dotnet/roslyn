// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using MemberFilter = Microsoft.CodeAnalysis.Scripting.Hosting.MemberFilter;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    internal class CSharpObjectFormatterImpl : CommonObjectFormatter
    {
        protected override CommonTypeNameFormatter TypeNameFormatter { get; }
        protected override CommonPrimitiveFormatter PrimitiveFormatter { get; }
        protected override MemberFilter Filter { get; }

        internal CSharpObjectFormatterImpl()
        {
            PrimitiveFormatter = new CSharpPrimitiveFormatter();
            TypeNameFormatter = new CSharpTypeNameFormatter(PrimitiveFormatter);
            Filter = new CSharpMemberFilter();
        }

        protected override string FormatRefKind(ParameterInfo parameter) => parameter.IsOut ? "out" : "ref";
    }
}
