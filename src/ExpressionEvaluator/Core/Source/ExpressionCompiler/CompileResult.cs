// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class CompileResult
    {
        internal readonly byte[] Assembly; // [] rather than ReadOnlyCollection<> to allow caller to create Stream easily
        internal readonly string TypeName;
        internal readonly string MethodName;
        internal readonly ReadOnlyCollection<string>? FormatSpecifiers;

        internal CompileResult(
            byte[] assembly,
            string typeName,
            string methodName,
            ReadOnlyCollection<string>? formatSpecifiers)
        {
            Assembly = assembly;
            TypeName = typeName;
            MethodName = methodName;
            FormatSpecifiers = formatSpecifiers;
        }

        public abstract Guid GetCustomTypeInfo(out ReadOnlyCollection<byte>? payload);
    }
}
