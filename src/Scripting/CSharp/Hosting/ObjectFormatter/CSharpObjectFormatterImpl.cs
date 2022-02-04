// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
