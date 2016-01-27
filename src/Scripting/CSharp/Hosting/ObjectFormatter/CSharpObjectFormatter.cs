// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    public class CSharpObjectFormatter : CommonObjectFormatter
    {
        public static CSharpObjectFormatter Instance { get; } = new CSharpObjectFormatter();

        internal override CommonTypeNameFormatter TypeNameFormatter { get; }
        internal override CommonPrimitiveFormatter PrimitiveFormatter { get; }
        internal override MemberFilter Filter { get; }

        internal CSharpObjectFormatter()
        {
            PrimitiveFormatter = new CSharpPrimitiveFormatter();
            TypeNameFormatter = new CSharpTypeNameFormatter(PrimitiveFormatter);
            Filter = new CSharpMemberFilter();
        }

        internal override string FormatRefKind(ParameterInfo parameter)
        {
            return parameter.IsOut
                ? parameter.IsIn
                    ? "ref"
                    : "out"
                : "";
        }
    }
}
