// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
