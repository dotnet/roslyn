// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct CompileResult
    {
        internal CompileResult(
            byte[] assembly,
            string typeName,
            string methodName,
            ReadOnlyCollection<string> formatSpecifiers)
        {
            this.Assembly = assembly;
            this.TypeName = typeName;
            this.MethodName = methodName;
            this.FormatSpecifiers = formatSpecifiers;
        }

        internal readonly byte[] Assembly; // [] rather than ReadOnlyCollection<> to allow caller to create Stream easily
        internal readonly string TypeName;
        internal readonly string MethodName;
        internal readonly ReadOnlyCollection<string> FormatSpecifiers;
    }
}
