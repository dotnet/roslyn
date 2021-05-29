// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class ExpressionCompilerConstants
    {
        internal const string TypeVariablesLocalName = "<>TypeVariables";
        internal const string TypeVariablesClassName = "<>c__TypeVariables";
        internal const string IntrinsicAssemblyNamespace = "Microsoft.VisualStudio.Debugger.Clr";
        internal const string IntrinsicAssemblyTypeName = "IntrinsicMethods";
        internal const string IntrinsicAssemblyTypeMetadataName = IntrinsicAssemblyNamespace + "." + IntrinsicAssemblyTypeName;
        internal const string GetExceptionMethodName = "GetException";
        internal const string GetStowedExceptionMethodName = "GetStowedException";
        internal const string GetObjectAtAddressMethodName = "GetObjectAtAddress";
        internal const string GetReturnValueMethodName = "GetReturnValue";
        internal const string CreateVariableMethodName = "CreateVariable";
        internal const string GetVariableValueMethodName = "GetObjectByAlias";
        internal const string GetVariableAddressMethodName = "GetVariableAddress";
    }
}
