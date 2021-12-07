// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum LambdaConversionResult
    {
        Success,
        BadTargetType,
        BadParameterCount,
        MissingSignatureWithOutParameter,
        MismatchedReturnType,
        MismatchedParameterType,
        RefInImplicitlyTypedLambda,
        StaticTypeInImplicitlyTypedLambda,
        ExpressionTreeMustHaveDelegateTypeArgument,
        ExpressionTreeFromAnonymousMethod,
        BindingFailed
    }
}
