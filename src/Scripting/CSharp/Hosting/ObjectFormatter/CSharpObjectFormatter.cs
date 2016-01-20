// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    public class CSharpObjectFormatter : CommonObjectFormatter
    {
        protected override CommonTypeNameFormatter TypeNameFormatter { get; }
        protected override CommonPrimitiveFormatter PrimitiveFormatter { get; }
        protected override MemberFilter Filter { get; }

        public CSharpObjectFormatter()
        {
            PrimitiveFormatter = new CSharpPrimitiveFormatter();
            TypeNameFormatter = new CSharpTypeNameFormatter(PrimitiveFormatter);
            Filter = new CSharpMemberFilter();
        }

        protected override string FormatRefKind(ParameterInfo parameter)
        {
            return parameter.IsOut
                ? parameter.IsIn
                    ? "ref"
                    : "out"
                : "";
        }
    }
}
