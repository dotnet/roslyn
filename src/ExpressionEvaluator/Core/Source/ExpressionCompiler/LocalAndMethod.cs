// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// The name of a local or argument and the name of
    /// the corresponding method to access that object.
    /// </summary>
    internal abstract class LocalAndMethod
    {
        public readonly string LocalName;
        public readonly string MethodName;
        public readonly DkmClrCompilationResultFlags Flags;

        public LocalAndMethod(string localName, string methodName, DkmClrCompilationResultFlags flags)
        {
            this.LocalName = localName;
            this.MethodName = methodName;
            this.Flags = flags;
        }

        public abstract CustomTypeInfo GetCustomTypeInfo();
    }
}
