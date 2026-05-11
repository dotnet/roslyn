// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Keep names and codes in sync with Roslyn's ErrorCode.cs. Add as necessary.
/// </summary>
public enum ErrorCode
{
    ERR_BadIndexLHS = 21,
    ERR_NoImplicitConv = 29,
    ERR_NameNotInContext = 103,
    ERR_BadSKunknown = 119,
    ERR_ObjectRequired = 120,
    WRN_UnreferencedField = 169,
    ERR_IllegalStatement = 201,
    WRN_UnreferencedVarAssg = 219,
    ERR_DottedTypeNameNotFoundInNS = 234,
    ERR_SingleTypeNameNotFound = 246,
    ERR_BadArity = 305,
    ERR_CantInferMethTypeArgs = 411,
    WRN_UnreferencedFieldAssg = 414,
    WRN_InvalidAttributeLocation = 658,
    ERR_VariableUsedBeforeDeclaration = 841,
    ERR_SemicolonExpected = 1002,
    ERR_SyntaxError = 1003,
    ERR_EndOfPPLineExpected = 1025,
    ERR_CloseParenExpected = 1026,
    ERR_EndifDirectiveExpected = 1027,
    ERR_UnexpectedDirective = 1028,
    ERR_TypeExpected = 1031,
    ERR_PPDefFollowsToken = 1032,
    ERR_BadDirectivePlacement = 1040,
    ERR_DottedTypeNameNotFoundInNSFwd = 1069,
    ERR_BadArgCount = 1501,
    ERR_BadArgType = 1503,
    ERR_RbraceExpected = 1513,
    ERR_InvalidMemberDecl = 1519,
    ERR_InvalidExprTerm = 1525,
    ERR_CantConvAnonMethReturns = 1662,
    WRN_AsyncLacksAwaits = 1998,
    ERR_NoCorrespondingArgument = 7036,
    ERR_RetNoObjectRequiredLambda = 8030,
    ERR_TupleTooFewElements = 8124,
    WRN_NullReferenceReceiver = 8602,
    WRN_UninitializedNonNullableField = 8618,
    WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode = 8669,
    ERR_FeatureNotAvailableInVersion10 = 8936,
    ERR_IllegalAtSequence = 9008,
}
