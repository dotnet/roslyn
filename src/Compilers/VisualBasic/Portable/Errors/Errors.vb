' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'//-------------------------------------------------------------------------------------------------
'//
'//  Error code and strings for Compiler errors
'//
'//  ERRIDs should be defined in the following ranges:
'//
'//   500   -   999    - non localized ERRID (main DLL)
'//   30000 - 59999    - localized ERRID     (intl DLL)
'//
'//  The convention for naming ERRID's that take replacement strings is to give
'//  them a number following the name (from 1-9) that indicates how many
'//  arguments they expect.
'//
'//  DO NOT USE ANY NUMBERS EXCEPT THOSE EXPLICITLY LISTED AS BEING AVAILABLE.
'//  IF YOU REUSE A NUMBER, LOCALIZATION WILL BE SCREWED UP!
'//
'//-------------------------------------------------------------------------------------------------

' //-------------------------------------------------------------------------------------------------
' //
' //
' //  Manages the parse and compile errors.
' //
' //-------------------------------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Enum ERRID
        Void = InternalErrorCode.Void
        Unknown = InternalErrorCode.Unknown
        ERR_None = 0
        ' ERR_InitError = 2000 unused in Roslyn
        ERR_FileNotFound = 2001
        ' WRN_FileAlreadyIncluded = 2002  'unused in Roslyn.
        'ERR_DuplicateResponseFile = 2003   unused in Roslyn.
        'ERR_NoMemory = 2004
        ERR_ArgumentRequired = 2006
        WRN_BadSwitch = 2007
        ERR_NoSources = 2008
        ERR_SwitchNeedsBool = 2009
        'ERR_CompileFailed = 2010       unused in Roslyn.
        ERR_NoResponseFile = 2011
        ERR_CantOpenFileWrite = 2012
        ERR_InvalidSwitchValue = 2014
        ERR_BinaryFile = 2015
        ERR_BadCodepage = 2016
        ERR_LibNotFound = 2017
        'ERR_MaximumErrors = 2020       unused in Roslyn.
        ERR_IconFileAndWin32ResFile = 2023
        'WRN_ReservedReference = 2024       ' unused by native compiler due to bug. 
        WRN_NoConfigInResponseFile = 2025
        ' WRN_InvalidWarningId = 2026       ' unused in Roslyn.
        'ERR_WatsonSendNotOptedIn = 2027
        ' WRN_SwitchNoBool = 2028     'unused in Roslyn
        ERR_NoSourcesOut = 2029
        ERR_NeedModule = 2030
        ERR_InvalidAssemblyName = 2031
        FTL_InvalidInputFileName = 2032 ' new in Roslyn
        ERR_ConflictingManifestSwitches = 2033
        WRN_IgnoreModuleManifest = 2034
        'ERR_NoDefaultManifest = 2035
        'ERR_InvalidSwitchValue1 = 2036
        WRN_BadUILang = 2038 ' new in Roslyn
        ERR_VBCoreNetModuleConflict = 2042
        ERR_InvalidFormatForGuidForOption = 2043
        ERR_MissingGuidForOption = 2044
        ERR_BadChecksumAlgorithm = 2045
        ERR_MutuallyExclusiveOptions = 2046
        ERR_BadSwitchValue = 2047

        '// The naming convention is that if your error requires arguments, to append
        '// the number of args taken, e.g. AmbiguousName2
        '//
        ERR_InvalidInNamespace = 30001
        ERR_UndefinedType1 = 30002
        ERR_MissingNext = 30003
        ERR_IllegalCharConstant = 30004

        '//If you make any change involving these errors, such as creating more specific versions for use
        '//in other contexts, please make sure to appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember
        ERR_UnreferencedAssemblyEvent3 = 30005
        ERR_UnreferencedModuleEvent3 = 30006
        ' ERR_UnreferencedAssemblyBase3 = 30007
        ' ERR_UnreferencedModuleBase3 = 30008           - This has been superceded by ERR_UnreferencedModuleEvent3
        ' ERR_UnreferencedAssemblyImplements3 = 30009
        'ERR_UnreferencedModuleImplements3 = 30010      - This has been superceded by ERR_UnreferencedModuleEvent3

        'ERR_CodegenError = 30011
        ERR_LbExpectedEndIf = 30012
        ERR_LbNoMatchingIf = 30013
        ERR_LbBadElseif = 30014
        ERR_InheritsFromRestrictedType1 = 30015
        ERR_InvOutsideProc = 30016
        ERR_DelegateCantImplement = 30018
        ERR_DelegateCantHandleEvents = 30019
        ERR_IsOperatorRequiresReferenceTypes1 = 30020
        ERR_TypeOfRequiresReferenceType1 = 30021
        ERR_ReadOnlyHasSet = 30022
        ERR_WriteOnlyHasGet = 30023
        ERR_InvInsideProc = 30024
        ERR_EndProp = 30025
        ERR_EndSubExpected = 30026
        ERR_EndFunctionExpected = 30027
        ERR_LbElseNoMatchingIf = 30028
        ERR_CantRaiseBaseEvent = 30029
        ERR_TryWithoutCatchOrFinally = 30030
        ' ERR_FullyQualifiedNameTooLong1 = 30031 ' Deprecated in favor of ERR_TooLongMetadataName
        ERR_EventsCantBeFunctions = 30032
        ' ERR_IdTooLong = 30033 ' Deprecated in favor of ERR_TooLongMetadataName
        ERR_MissingEndBrack = 30034
        ERR_Syntax = 30035
        ERR_Overflow = 30036
        ERR_IllegalChar = 30037
        ERR_StrictDisallowsObjectOperand1 = 30038
        ERR_LoopControlMustNotBeProperty = 30039
        ERR_MethodBodyNotAtLineStart = 30040
        ERR_MaximumNumberOfErrors = 30041
        ERR_UseOfKeywordNotInInstanceMethod1 = 30043
        ERR_UseOfKeywordFromStructure1 = 30044
        ERR_BadAttributeConstructor1 = 30045
        ERR_ParamArrayWithOptArgs = 30046
        ERR_ExpectedArray1 = 30049
        ERR_ParamArrayNotArray = 30050
        ERR_ParamArrayRank = 30051
        ERR_ArrayRankLimit = 30052
        ERR_AsNewArray = 30053
        ERR_TooManyArgs1 = 30057
        ERR_ExpectedCase = 30058
        ERR_RequiredConstExpr = 30059
        ERR_RequiredConstConversion2 = 30060
        ERR_InvalidMe = 30062
        ERR_ReadOnlyAssignment = 30064
        ERR_ExitSubOfFunc = 30065
        ERR_ExitPropNot = 30066
        ERR_ExitFuncOfSub = 30067
        ERR_LValueRequired = 30068
        ERR_ForIndexInUse1 = 30069
        ERR_NextForMismatch1 = 30070
        ERR_CaseElseNoSelect = 30071
        ERR_CaseNoSelect = 30072
        ERR_CantAssignToConst = 30074
        ERR_NamedSubscript = 30075
        ERR_ExpectedEndIf = 30081
        ERR_ExpectedEndWhile = 30082
        ERR_ExpectedLoop = 30083
        ERR_ExpectedNext = 30084
        ERR_ExpectedEndWith = 30085
        ERR_ElseNoMatchingIf = 30086
        ERR_EndIfNoMatchingIf = 30087
        ERR_EndSelectNoSelect = 30088
        ERR_ExitDoNotWithinDo = 30089
        ERR_EndWhileNoWhile = 30090
        ERR_LoopNoMatchingDo = 30091
        ERR_NextNoMatchingFor = 30092
        ERR_EndWithWithoutWith = 30093
        ERR_MultiplyDefined1 = 30094
        ERR_ExpectedEndSelect = 30095
        ERR_ExitForNotWithinFor = 30096
        ERR_ExitWhileNotWithinWhile = 30097
        ERR_ReadOnlyProperty1 = 30098
        ERR_ExitSelectNotWithinSelect = 30099
        ERR_BranchOutOfFinally = 30101
        ERR_QualNotObjectRecord1 = 30103
        ERR_TooFewIndices = 30105
        ERR_TooManyIndices = 30106
        ERR_EnumNotExpression1 = 30107
        ERR_TypeNotExpression1 = 30108
        ERR_ClassNotExpression1 = 30109
        ERR_StructureNotExpression1 = 30110
        ERR_InterfaceNotExpression1 = 30111
        ERR_NamespaceNotExpression1 = 30112
        ERR_BadNamespaceName1 = 30113
        ERR_XmlPrefixNotExpression = 30114
        ERR_MultipleExtends = 30121
        'ERR_NoStopInDebugger = 30122
        'ERR_NoEndInDebugger = 30123
        ERR_PropMustHaveGetSet = 30124
        ERR_WriteOnlyHasNoWrite = 30125
        ERR_ReadOnlyHasNoGet = 30126
        ERR_BadAttribute1 = 30127
        ' ERR_BadSecurityAttribute1 = 30128 ' we're now reporting more detailed diagnostics: ERR_SecurityAttributeMissingAction or ERR_SecurityAttributeInvalidAction
        'ERR_BadAssemblyAttribute1 = 30129
        'ERR_BadModuleAttribute1 = 30130
        ' ERR_ModuleSecurityAttributeNotAllowed1 = 30131    ' We now report ERR_SecurityAttributeInvalidTarget instead.
        ERR_LabelNotDefined1 = 30132
        'ERR_NoGotosInDebugger = 30133
        'ERR_NoLabelsInDebugger = 30134
        'ERR_NoSyncLocksInDebugger = 30135
        ERR_ErrorCreatingWin32ResourceFile = 30136
        'ERR_ErrorSavingWin32ResourceFile = 30137   abandoned. no longer "saving" a temporary resource file.
        ERR_UnableToCreateTempFile = 30138  'changed from ERR_UnableToCreateTempFileInPath1. now takes only one argument
        'ERR_ErrorSettingManifestOption = 30139
        'ERR_ErrorCreatingManifest = 30140
        'ERR_UnableToCreateALinkAPI = 30141
        'ERR_UnableToGenerateRefToMetaDataFile1 = 30142
        'ERR_UnableToEmbedResourceFile1 = 30143 ' We now report ERR_UnableToOpenResourceFile1 instead.
        'ERR_UnableToLinkResourceFile1 = 30144 ' We now report ERR_UnableToOpenResourceFile1 instead.
        'ERR_UnableToEmitAssembly = 30145
        'ERR_UnableToSignAssembly = 30146
        'ERR_NoReturnsInDebugger = 30147
        ERR_RequiredNewCall2 = 30148
        ERR_UnimplementedMember3 = 30149
        ' ERR_UnimplementedProperty3 = 30154
        ERR_BadWithRef = 30157
        ' ERR_ExpectedNewableClass1 = 30166 unused in Roslyn. We now report nothing
        ' ERR_TypeConflict7 = 30175         unused in Roslyn. We now report BC30179
        ERR_DuplicateAccessCategoryUsed = 30176
        ERR_DuplicateModifierCategoryUsed = 30177
        ERR_DuplicateSpecifier = 30178
        ERR_TypeConflict6 = 30179
        ERR_UnrecognizedTypeKeyword = 30180
        ERR_ExtraSpecifiers = 30181
        ERR_UnrecognizedType = 30182
        ERR_InvalidUseOfKeyword = 30183
        ERR_InvalidEndEnum = 30184
        ERR_MissingEndEnum = 30185
        'ERR_NoUsingInDebugger = 30186
        ERR_ExpectedDeclaration = 30188
        ERR_ParamArrayMustBeLast = 30192
        ERR_SpecifiersInvalidOnInheritsImplOpt = 30193
        ERR_ExpectedSpecifier = 30195
        ERR_ExpectedComma = 30196
        ERR_ExpectedAs = 30197
        ERR_ExpectedRparen = 30198
        ERR_ExpectedLparen = 30199
        ERR_InvalidNewInType = 30200
        ERR_ExpectedExpression = 30201
        ERR_ExpectedOptional = 30202
        ERR_ExpectedIdentifier = 30203
        ERR_ExpectedIntLiteral = 30204
        ERR_ExpectedEOS = 30205
        ERR_ExpectedForOptionStmt = 30206
        ERR_InvalidOptionCompare = 30207
        ERR_ExpectedOptionCompare = 30208
        ERR_StrictDisallowImplicitObject = 30209
        ERR_StrictDisallowsImplicitProc = 30210
        ERR_StrictDisallowsImplicitArgs = 30211
        ERR_InvalidParameterSyntax = 30213
        ERR_ExpectedSubFunction = 30215
        ERR_ExpectedStringLiteral = 30217
        ERR_MissingLibInDeclare = 30218
        ERR_DelegateNoInvoke1 = 30220
        ERR_MissingIsInTypeOf = 30224
        ERR_DuplicateOption1 = 30225
        ERR_ModuleCantInherit = 30230
        ERR_ModuleCantImplement = 30231
        ERR_BadImplementsType = 30232
        ERR_BadConstFlags1 = 30233
        ERR_BadWithEventsFlags1 = 30234
        ERR_BadDimFlags1 = 30235
        ERR_DuplicateParamName1 = 30237
        ERR_LoopDoubleCondition = 30238
        ERR_ExpectedRelational = 30239
        ERR_ExpectedExitKind = 30240
        ERR_ExpectedNamedArgument = 30241
        ERR_BadMethodFlags1 = 30242
        ERR_BadEventFlags1 = 30243
        ERR_BadDeclareFlags1 = 30244
        ERR_BadLocalConstFlags1 = 30246
        ERR_BadLocalDimFlags1 = 30247
        ERR_ExpectedConditionalDirective = 30248
        ERR_ExpectedEQ = 30249
        ERR_ConstructorNotFound1 = 30251
        ERR_InvalidEndInterface = 30252
        ERR_MissingEndInterface = 30253
        ERR_InheritsFrom2 = 30256
        ERR_InheritanceCycle1 = 30257
        ERR_InheritsFromNonClass = 30258
        ERR_MultiplyDefinedType3 = 30260
        ERR_BadOverrideAccess2 = 30266
        ERR_CantOverrideNotOverridable2 = 30267
        ERR_DuplicateProcDef1 = 30269
        ERR_BadInterfaceMethodFlags1 = 30270
        ERR_NamedParamNotFound2 = 30272
        ERR_BadInterfacePropertyFlags1 = 30273
        ERR_NamedArgUsedTwice2 = 30274
        ERR_InterfaceCantUseEventSpecifier1 = 30275
        ERR_TypecharNoMatch2 = 30277
        ERR_ExpectedSubOrFunction = 30278
        ERR_BadEmptyEnum1 = 30280
        ERR_InvalidConstructorCall = 30282
        ERR_CantOverrideConstructor = 30283
        ERR_OverrideNotNeeded3 = 30284
        ERR_ExpectedDot = 30287
        ERR_DuplicateLocals1 = 30288
        ERR_InvInsideEndsProc = 30289
        ERR_LocalSameAsFunc = 30290
        ERR_RecordEmbeds2 = 30293
        ERR_RecordCycle2 = 30294
        ERR_InterfaceCycle1 = 30296
        ERR_SubNewCycle2 = 30297
        ERR_SubNewCycle1 = 30298
        ERR_InheritsFromCantInherit3 = 30299
        ERR_OverloadWithOptional2 = 30300
        ERR_OverloadWithReturnType2 = 30301
        ERR_TypeCharWithType1 = 30302
        ERR_TypeCharOnSub = 30303
        ERR_OverloadWithDefault2 = 30305
        ERR_MissingSubscript = 30306
        ERR_OverrideWithDefault2 = 30307
        ERR_OverrideWithOptional2 = 30308
        ERR_FieldOfValueFieldOfMarshalByRef3 = 30310
        ERR_TypeMismatch2 = 30311
        ERR_CaseAfterCaseElse = 30321
        ERR_ConvertArrayMismatch4 = 30332
        ERR_ConvertObjectArrayMismatch3 = 30333
        ERR_ForLoopType1 = 30337
        ERR_OverloadWithByref2 = 30345
        ERR_InheritsFromNonInterface = 30354
        ERR_BadInterfaceOrderOnInherits = 30357
        ERR_DuplicateDefaultProps1 = 30359
        ERR_DefaultMissingFromProperty2 = 30361
        ERR_OverridingPropertyKind2 = 30362
        ERR_NewInInterface = 30363
        ERR_BadFlagsOnNew1 = 30364
        ERR_OverloadingPropertyKind2 = 30366
        ERR_NoDefaultNotExtend1 = 30367
        ERR_OverloadWithArrayVsParamArray2 = 30368
        ERR_BadInstanceMemberAccess = 30369
        ERR_ExpectedRbrace = 30370
        ERR_ModuleAsType1 = 30371
        ERR_NewIfNullOnNonClass = 30375
        'ERR_NewIfNullOnAbstractClass1 = 30376
        ERR_CatchAfterFinally = 30379
        ERR_CatchNoMatchingTry = 30380
        ERR_FinallyAfterFinally = 30381
        ERR_FinallyNoMatchingTry = 30382
        ERR_EndTryNoTry = 30383
        ERR_ExpectedEndTry = 30384
        ERR_BadDelegateFlags1 = 30385
        ERR_NoConstructorOnBase2 = 30387
        ERR_InaccessibleSymbol2 = 30389
        ERR_InaccessibleMember3 = 30390
        ERR_CatchNotException1 = 30392
        ERR_ExitTryNotWithinTry = 30393
        ERR_BadRecordFlags1 = 30395
        ERR_BadEnumFlags1 = 30396
        ERR_BadInterfaceFlags1 = 30397
        ERR_OverrideWithByref2 = 30398
        ERR_MyBaseAbstractCall1 = 30399
        ERR_IdentNotMemberOfInterface4 = 30401
        ERR_ImplementingInterfaceWithDifferentTupleNames5 = 30402
        '//We intentionally use argument '3' for the delegate name. This makes generating overload resolution errors
        '//easy. To make it more clear that were doing this, we name the message DelegateBindingMismatch3_2.
        '//This differentiates its from DelegateBindingMismatch3_3, which actually takes 3 parameters instead of 2.
        '//This is a workaround, but it makes the logic for reporting overload resolution errors easier error report more straight forward.
        'ERR_DelegateBindingMismatch3_2 = 30408
        ERR_WithEventsRequiresClass = 30412
        ERR_WithEventsAsStruct = 30413
        ERR_ConvertArrayRankMismatch2 = 30414
        ERR_RedimRankMismatch = 30415
        ERR_StartupCodeNotFound1 = 30420
        ERR_ConstAsNonConstant = 30424
        ERR_InvalidEndSub = 30429
        ERR_InvalidEndFunction = 30430
        ERR_InvalidEndProperty = 30431
        ERR_ModuleCantUseMethodSpecifier1 = 30433
        ERR_ModuleCantUseEventSpecifier1 = 30434
        ERR_StructCantUseVarSpecifier1 = 30435
        'ERR_ModuleCantUseMemberSpecifier1 = 30436 Now reporting BC30735
        ERR_InvalidOverrideDueToReturn2 = 30437
        ERR_ConstantWithNoValue = 30438
        ERR_ExpressionOverflow1 = 30439
        'ERR_ExpectedEndTryCatch = 30441 - No Longer Reported. Removed per bug 926779
        'ERR_ExpectedEndTryFinally = 30442 - No Longer Reported. Removed per bug 926779
        ERR_DuplicatePropertyGet = 30443
        ERR_DuplicatePropertySet = 30444
        ' ERR_ConstAggregate = 30445        Now giving BC30424
        ERR_NameNotDeclared1 = 30451
        ERR_BinaryOperands3 = 30452
        ERR_ExpectedProcedure = 30454
        ERR_OmittedArgument2 = 30455
        ERR_NameNotMember2 = 30456
        'ERR_NoTypeNamesAvailable = 30458
        ERR_EndClassNoClass = 30460
        ERR_BadClassFlags1 = 30461
        ERR_ImportsMustBeFirst = 30465
        ERR_NonNamespaceOrClassOnImport2 = 30467
        ERR_TypecharNotallowed = 30468
        ERR_ObjectReferenceNotSupplied = 30469
        ERR_MyClassNotInClass = 30470
        ERR_IndexedNotArrayOrProc = 30471
        ERR_EventSourceIsArray = 30476
        ERR_SharedConstructorWithParams = 30479
        ERR_SharedConstructorIllegalSpec1 = 30480
        ERR_ExpectedEndClass = 30481
        ERR_UnaryOperand2 = 30487
        ERR_BadFlagsWithDefault1 = 30490
        ERR_VoidValue = 30491
        ERR_ConstructorFunction = 30493
        'ERR_LineTooLong = 30494 - No longer reported. Removed per 926916
        ERR_InvalidLiteralExponent = 30495
        ERR_NewCannotHandleEvents = 30497
        ERR_CircularEvaluation1 = 30500
        ERR_BadFlagsOnSharedMeth1 = 30501
        ERR_BadFlagsOnSharedProperty1 = 30502
        ERR_BadFlagsOnStdModuleProperty1 = 30503
        ERR_SharedOnProcThatImpl = 30505
        ERR_NoWithEventsVarOnHandlesList = 30506
        ERR_AccessMismatch6 = 30508
        ERR_InheritanceAccessMismatch5 = 30509
        ERR_NarrowingConversionDisallowed2 = 30512
        ERR_NoArgumentCountOverloadCandidates1 = 30516
        ERR_NoViableOverloadCandidates1 = 30517
        ERR_NoCallableOverloadCandidates2 = 30518
        ERR_NoNonNarrowingOverloadCandidates2 = 30519
        ERR_ArgumentNarrowing3 = 30520
        ERR_NoMostSpecificOverload2 = 30521
        ERR_NotMostSpecificOverload = 30522
        ERR_OverloadCandidate2 = 30523
        ERR_NoGetProperty1 = 30524
        ERR_NoSetProperty1 = 30526
        'ERR_ArrayType2 = 30528
        ERR_ParamTypingInconsistency = 30529
        ERR_ParamNameFunctionNameCollision = 30530
        ERR_DateToDoubleConversion = 30532
        ERR_DoubleToDateConversion = 30533
        ERR_ZeroDivide = 30542
        ERR_TryAndOnErrorDoNotMix = 30544
        ERR_PropertyAccessIgnored = 30545
        ERR_InterfaceNoDefault1 = 30547
        ERR_InvalidAssemblyAttribute1 = 30548
        ERR_InvalidModuleAttribute1 = 30549
        ERR_AmbiguousInUnnamedNamespace1 = 30554
        ERR_DefaultMemberNotProperty1 = 30555
        ERR_AmbiguousInNamespace2 = 30560
        ERR_AmbiguousInImports2 = 30561
        ERR_AmbiguousInModules2 = 30562
        ' ERR_AmbiguousInApplicationObject2 = 30563 ' comment out in Dev10
        ERR_ArrayInitializerTooFewDimensions = 30565
        ERR_ArrayInitializerTooManyDimensions = 30566
        ERR_InitializerTooFewElements1 = 30567
        ERR_InitializerTooManyElements1 = 30568
        ERR_NewOnAbstractClass = 30569
        ERR_DuplicateNamedImportAlias1 = 30572
        ERR_DuplicatePrefix = 30573
        ERR_StrictDisallowsLateBinding = 30574
        ' ERR_PropertyMemberSyntax = 30576  unused in Roslyn
        ERR_AddressOfOperandNotMethod = 30577
        ERR_EndExternalSource = 30578
        ERR_ExpectedEndExternalSource = 30579
        ERR_NestedExternalSource = 30580
        ERR_AddressOfNotDelegate1 = 30581
        ERR_SyncLockRequiresReferenceType1 = 30582
        ERR_MethodAlreadyImplemented2 = 30583
        ERR_DuplicateInInherits1 = 30584
        ERR_NamedParamArrayArgument = 30587
        ERR_OmittedParamArrayArgument = 30588
        ERR_ParamArrayArgumentMismatch = 30589
        ERR_EventNotFound1 = 30590
        'ERR_NoDefaultSource = 30591
        ERR_ModuleCantUseVariableSpecifier1 = 30593
        ERR_SharedEventNeedsSharedHandler = 30594
        ERR_ExpectedMinus = 30601
        ERR_InterfaceMemberSyntax = 30602
        ERR_InvInsideInterface = 30603
        ERR_InvInsideEndsInterface = 30604
        ERR_BadFlagsInNotInheritableClass1 = 30607
        ERR_UnimplementedMustOverride = 30609 ' substituted into ERR_BaseOnlyClassesMustBeExplicit2
        ERR_BaseOnlyClassesMustBeExplicit2 = 30610
        ERR_NegativeArraySize = 30611
        ERR_MyClassAbstractCall1 = 30614
        ERR_EndDisallowedInDllProjects = 30615
        ERR_BlockLocalShadowing1 = 30616
        ERR_ModuleNotAtNamespace = 30617
        ERR_NamespaceNotAtNamespace = 30618
        ERR_InvInsideEndsEnum = 30619
        ERR_InvalidOptionStrict = 30620
        ERR_EndStructureNoStructure = 30621
        ERR_EndModuleNoModule = 30622
        ERR_EndNamespaceNoNamespace = 30623
        ERR_ExpectedEndStructure = 30624
        ERR_ExpectedEndModule = 30625
        ERR_ExpectedEndNamespace = 30626
        ERR_OptionStmtWrongOrder = 30627
        ERR_StructCantInherit = 30628
        ERR_NewInStruct = 30629
        ERR_InvalidEndGet = 30630
        ERR_MissingEndGet = 30631
        ERR_InvalidEndSet = 30632
        ERR_MissingEndSet = 30633
        ERR_InvInsideEndsProperty = 30634
        ERR_DuplicateWriteabilityCategoryUsed = 30635
        ERR_ExpectedGreater = 30636
        ERR_AttributeStmtWrongOrder = 30637
        ERR_NoExplicitArraySizes = 30638
        ERR_BadPropertyFlags1 = 30639
        ERR_InvalidOptionExplicit = 30640
        ERR_MultipleParameterSpecifiers = 30641
        ERR_MultipleOptionalParameterSpecifiers = 30642
        ERR_UnsupportedProperty1 = 30643
        ERR_InvalidOptionalParameterUsage1 = 30645
        ERR_ReturnFromNonFunction = 30647
        ERR_UnterminatedStringLiteral = 30648
        ERR_UnsupportedType1 = 30649
        ERR_InvalidEnumBase = 30650
        ERR_ByRefIllegal1 = 30651

        '//If you make any change involving these errors, such as creating more specific versions for use
        '//in other contexts, please make sure to appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember
        ERR_UnreferencedAssembly3 = 30652
        ERR_UnreferencedModule3 = 30653

        ERR_ReturnWithoutValue = 30654
        ' ERR_CantLoadStdLibrary1 = 30655   roslyn doesn't use special messages when well-known assemblies cannot be loaded.
        ERR_UnsupportedField1 = 30656
        ERR_UnsupportedMethod1 = 30657
        ERR_NoNonIndexProperty1 = 30658
        ERR_BadAttributePropertyType1 = 30659
        ERR_LocalsCannotHaveAttributes = 30660
        ERR_PropertyOrFieldNotDefined1 = 30661
        ERR_InvalidAttributeUsage2 = 30662
        ERR_InvalidMultipleAttributeUsage1 = 30663
        ERR_CantThrowNonException = 30665
        ERR_MustBeInCatchToRethrow = 30666
        ERR_ParamArrayMustBeByVal = 30667
        ERR_UseOfObsoleteSymbol2 = 30668
        ERR_RedimNoSizes = 30670
        ERR_InitWithMultipleDeclarators = 30671
        ERR_InitWithExplicitArraySizes = 30672
        ERR_EndSyncLockNoSyncLock = 30674
        ERR_ExpectedEndSyncLock = 30675
        ERR_NameNotEvent2 = 30676
        ERR_AddOrRemoveHandlerEvent = 30677
        ERR_UnrecognizedEnd = 30678

        ERR_ArrayInitForNonArray2 = 30679

        ERR_EndRegionNoRegion = 30680
        ERR_ExpectedEndRegion = 30681
        ERR_InheritsStmtWrongOrder = 30683
        ERR_AmbiguousAcrossInterfaces3 = 30685
        ERR_DefaultPropertyAmbiguousAcrossInterfaces4 = 30686
        ERR_InterfaceEventCantUse1 = 30688
        ERR_ExecutableAsDeclaration = 30689
        ERR_StructureNoDefault1 = 30690
        ' ERR_TypeMemberAsExpression2 = 30691 Now giving BC30109
        ERR_MustShadow2 = 30695
        'ERR_OverloadWithOptionalTypes2 = 30696
        ERR_OverrideWithOptionalTypes2 = 30697
        'ERR_UnableToGetTempPath = 30698
        'ERR_NameNotDeclaredDebug1 = 30699
        '// This error should never be seen.
        'ERR_NoSideEffects = 30700
        'ERR_InvalidNothing = 30701
        'ERR_IndexOutOfRange1 = 30702
        'ERR_RuntimeException2 = 30703
        'ERR_RuntimeException = 30704
        'ERR_ObjectReferenceIsNothing1 = 30705
        '// This error should never be seen.
        'ERR_ExpressionNotValidInEE = 30706
        'ERR_UnableToEvaluateExpression = 30707
        'ERR_UnableToEvaluateLoops = 30708
        'ERR_NoDimsInDebugger = 30709
        ERR_ExpectedEndOfExpression = 30710
        'ERR_SetValueNotAllowedOnNonLeafFrame = 30711
        'ERR_UnableToClassInformation1 = 30712
        'ERR_NoExitInDebugger = 30713
        'ERR_NoResumeInDebugger = 30714
        'ERR_NoCatchInDebugger = 30715
        'ERR_NoFinallyInDebugger = 30716
        'ERR_NoTryInDebugger = 30717
        'ERR_NoSelectInDebugger = 30718
        'ERR_NoCaseInDebugger = 30719
        'ERR_NoOnErrorInDebugger = 30720
        'ERR_EvaluationAborted = 30721
        'ERR_EvaluationTimeout = 30722
        'ERR_EvaluationNoReturnValue = 30723
        'ERR_NoErrorStatementInDebugger = 30724
        'ERR_NoThrowStatementInDebugger = 30725
        'ERR_NoWithContextInDebugger = 30726
        ERR_StructsCannotHandleEvents = 30728
        ERR_OverridesImpliesOverridable = 30730
        'ERR_NoAddressOfInDebugger = 30731
        'ERR_EvaluationOfWebMethods = 30732
        ERR_LocalNamedSameAsParam1 = 30734
        ERR_ModuleCantUseTypeSpecifier1 = 30735
        'ERR_EvaluationBadStartPoint = 30736
        ERR_InValidSubMainsFound1 = 30737
        ERR_MoreThanOneValidMainWasFound2 = 30738
        'ERR_NoRaiseEventOfInDebugger = 30739
        'ERR_InvalidCast2 = 30741
        ERR_CannotConvertValue2 = 30742
        'ERR_ArrayElementIsNothing = 30744
        'ERR_InternalCompilerError = 30747
        'ERR_InvalidCast1 = 30748
        'ERR_UnableToGetValue = 30749
        'ERR_UnableToLoadType1 = 30750
        'ERR_UnableToGetTypeInformationFor1 = 30751
        ERR_OnErrorInSyncLock = 30752
        ERR_NarrowingConversionCollection2 = 30753
        ERR_GotoIntoTryHandler = 30754
        ERR_GotoIntoSyncLock = 30755
        ERR_GotoIntoWith = 30756
        ERR_GotoIntoFor = 30757
        ERR_BadAttributeNonPublicConstructor = 30758
        'ERR_ArrayElementIsNothing1 = 30759
        'ERR_ObjectReferenceIsNothing = 30760
        ' ERR_StarliteDisallowsLateBinding = 30762   
        ' ERR_StarliteBadDeclareFlags = 30763
        ' ERR_NoStarliteOverloadResolution = 30764
        'ERR_NoSupportFileIOKeywords1 = 30766
        ' ERR_NoSupportGetStatement = 30767 - starlite error message
        ' ERR_NoSupportLineKeyword = 30768      cut from Roslyn
        ' ERR_StarliteDisallowsEndStatement = 30769 cut from Roslyn
        ERR_DefaultEventNotFound1 = 30770
        ERR_InvalidNonSerializedUsage = 30772
        'ERR_NoContinueInDebugger = 30780
        ERR_ExpectedContinueKind = 30781
        ERR_ContinueDoNotWithinDo = 30782
        ERR_ContinueForNotWithinFor = 30783
        ERR_ContinueWhileNotWithinWhile = 30784
        ERR_DuplicateParameterSpecifier = 30785
        ERR_ModuleCantUseDLLDeclareSpecifier1 = 30786
        ERR_StructCantUseDLLDeclareSpecifier1 = 30791
        ERR_TryCastOfValueType1 = 30792
        ERR_TryCastOfUnconstrainedTypeParam1 = 30793
        ERR_AmbiguousDelegateBinding2 = 30794
        ERR_SharedStructMemberCannotSpecifyNew = 30795
        ERR_GenericSubMainsFound1 = 30796
        ERR_GeneralProjectImportsError3 = 30797
        ERR_InvalidTypeForAliasesImport2 = 30798

        ERR_UnsupportedConstant2 = 30799

        ERR_ObsoleteArgumentsNeedParens = 30800
        ERR_ObsoleteLineNumbersAreLabels = 30801
        ERR_ObsoleteStructureNotType = 30802
        'ERR_ObsoleteDecimalNotCurrency = 30803 cut from Roslyn
        ERR_ObsoleteObjectNotVariant = 30804
        'ERR_ObsoleteArrayBounds = 30805    unused in Roslyn
        ERR_ObsoleteLetSetNotNeeded = 30807
        ERR_ObsoletePropertyGetLetSet = 30808
        ERR_ObsoleteWhileWend = 30809
        'ERR_ObsoleteStaticMethod = 30810   cut from Roslyn
        ERR_ObsoleteRedimAs = 30811
        ERR_ObsoleteOptionalWithoutValue = 30812
        ERR_ObsoleteGosub = 30814
        'ERR_ObsoleteFileIOKeywords1 = 30815    cut from Roslyn
        'ERR_ObsoleteDebugKeyword1 = 30816      cut from Roslyn
        ERR_ObsoleteOnGotoGosub = 30817
        'ERR_ObsoleteMathCompatKeywords1 = 30818    cut from Roslyn
        'ERR_ObsoleteMathKeywords2 = 30819          cut from Roslyn
        'ERR_ObsoleteLsetKeyword1 = 30820           cut from Roslyn
        'ERR_ObsoleteRsetKeyword1 = 30821           cut from Roslyn
        'ERR_ObsoleteNullKeyword1 = 30822           cut from Roslyn
        'ERR_ObsoleteEmptyKeyword1 = 30823          cut from Roslyn
        ERR_ObsoleteEndIf = 30826
        ERR_ObsoleteExponent = 30827
        ERR_ObsoleteAsAny = 30828
        ERR_ObsoleteGetStatement = 30829
        'ERR_ObsoleteLineKeyword = 30830            cut from Roslyn
        ERR_OverrideWithArrayVsParamArray2 = 30906

        '// CONSIDER :harishk - improve this error message
        ERR_CircularBaseDependencies4 = 30907
        ERR_NestedBase2 = 30908
        ERR_AccessMismatchOutsideAssembly4 = 30909
        ERR_InheritanceAccessMismatchOutside3 = 30910
        ERR_UseOfObsoletePropertyAccessor3 = 30911
        ERR_UseOfObsoletePropertyAccessor2 = 30912
        ERR_AccessMismatchImplementedEvent6 = 30914
        ERR_AccessMismatchImplementedEvent4 = 30915
        ERR_InheritanceCycleInImportedType1 = 30916
        ERR_NoNonObsoleteConstructorOnBase3 = 30917
        ERR_NoNonObsoleteConstructorOnBase4 = 30918
        ERR_RequiredNonObsoleteNewCall3 = 30919
        ERR_RequiredNonObsoleteNewCall4 = 30920
        ERR_InheritsTypeArgAccessMismatch7 = 30921
        ERR_InheritsTypeArgAccessMismatchOutside5 = 30922
        'ERR_AccessMismatchTypeArgImplEvent7 = 30923    unused in Roslyn
        'ERR_AccessMismatchTypeArgImplEvent5 = 30924    unused in Roslyn
        ERR_PartialTypeAccessMismatch3 = 30925
        ERR_PartialTypeBadMustInherit1 = 30926
        ERR_MustOverOnNotInheritPartClsMem1 = 30927
        ERR_BaseMismatchForPartialClass3 = 30928
        ERR_PartialTypeTypeParamNameMismatch3 = 30931
        ERR_PartialTypeConstraintMismatch1 = 30932
        ERR_LateBoundOverloadInterfaceCall1 = 30933
        ERR_RequiredAttributeConstConversion2 = 30934
        ERR_AmbiguousOverrides3 = 30935
        ERR_OverriddenCandidate1 = 30936
        ERR_AmbiguousImplements3 = 30937
        ERR_AddressOfNotCreatableDelegate1 = 30939
        'ERR_ReturnFromEventMethod = 30940  unused in Roslyn
        'ERR_BadEmptyStructWithCustomEvent1 = 30941
        ERR_ComClassGenericMethod = 30943
        ERR_SyntaxInCastOp = 30944
        'ERR_UnimplementedBadMemberEvent = 30945    Cut in Roslyn
        'ERR_EvaluationStopRequested = 30946
        'ERR_EvaluationSuspendRequested = 30947
        'ERR_EvaluationUnscheduledFiber = 30948
        ERR_ArrayInitializerForNonConstDim = 30949
        ERR_DelegateBindingFailure3 = 30950
        'ERR_DelegateBindingTypeInferenceFails2 = 30952
        'ERR_ConstraintViolationError1 = 30953
        'ERR_ConstraintsFailedForInferredArgs2 = 30954
        'ERR_TypeMismatchDLLProjectMix6 = 30955
        'ERR_EvaluationPriorTimeout = 30957
        'ERR_EvaluationENCObjectOutdated = 30958
        ' Obsolete ERR_TypeRefFromMetadataToVBUndef = 30960
        'ERR_TypeMismatchMixedDLLs6 = 30961
        'ERR_ReferencedAssemblyCausesCycle3 = 30962
        'ERR_AssemblyRefAssembly2 = 30963
        'ERR_AssemblyRefProject2 = 30964
        'ERR_ProjectRefAssembly2 = 30965
        'ERR_ProjectRefProject2 = 30966
        'ERR_ReferencedAssembliesAmbiguous4 = 30967
        'ERR_ReferencedAssembliesAmbiguous6 = 30968
        'ERR_ReferencedProjectsAmbiguous4 = 30969
        'ERR_GeneralErrorMixedDLLs5 = 30970
        'ERR_GeneralErrorDLLProjectMix5 = 30971
        ERR_StructLayoutAttributeNotAllowed = 30972
        'ERR_ClassNotLoadedDuringDebugging = 30973
        'ERR_UnableToEvaluateComment = 30974
        'ERR_ForIndexInUse = 30975
        'ERR_NextForMismatch = 30976
        ERR_IterationVariableShadowLocal1 = 30978
        ERR_InvalidOptionInfer = 30979
        ERR_CircularInference1 = 30980
        ERR_InAccessibleOverridingMethod5 = 30981
        ERR_NoSuitableWidestType1 = 30982
        ERR_AmbiguousWidestType3 = 30983
        ERR_ExpectedAssignmentOperatorInInit = 30984
        ERR_ExpectedQualifiedNameInInit = 30985
        ERR_ExpectedLbrace = 30987
        ERR_UnrecognizedTypeOrWith = 30988
        ERR_DuplicateAggrMemberInit1 = 30989
        ERR_NonFieldPropertyAggrMemberInit1 = 30990
        ERR_SharedMemberAggrMemberInit1 = 30991
        ERR_ParameterizedPropertyInAggrInit1 = 30992
        ERR_NoZeroCountArgumentInitCandidates1 = 30993
        ERR_AggrInitInvalidForObject = 30994
        'ERR_BadWithRefInConstExpr = 30995
        ERR_InitializerExpected = 30996
        ERR_LineContWithCommentOrNoPrecSpace = 30999
        ' ERR_MemberNotFoundForNoPia = 31000    not used in Roslyn. This looks to be a VB EE message
        ERR_InvInsideEnum = 31001
        ERR_InvInsideBlock = 31002
        ERR_UnexpectedExpressionStatement = 31003
        ERR_WinRTEventWithoutDelegate = 31004
        ERR_SecurityCriticalAsyncInClassOrStruct = 31005
        ERR_SecurityCriticalAsync = 31006
        ERR_BadModuleFile1 = 31007
        ERR_BadRefLib1 = 31011
        'ERR_UnableToLoadDll1 = 31013
        'ERR_BadDllEntrypoint2 = 31014
        'ERR_BadOutputFile1 = 31019
        'ERR_BadOutputStream = 31020
        'ERR_DeadBackgroundThread = 31021
        'ERR_XMLParserError = 31023
        'ERR_UnableToCreateMetaDataAPI = 31024
        'ERR_UnableToOpenFile1 = 31027
        ERR_EventHandlerSignatureIncompatible2 = 31029
        ERR_ConditionalCompilationConstantNotValid = 31030
        'ERR_ProjectCCError0 = 31031
        ERR_InterfaceImplementedTwice1 = 31033
        ERR_InterfaceNotImplemented1 = 31035
        ERR_AmbiguousImplementsMember3 = 31040
        'ERR_BadInterfaceMember = 31041
        ERR_ImplementsOnNew = 31042
        ERR_ArrayInitInStruct = 31043
        ERR_EventTypeNotDelegate = 31044
        ERR_ProtectedTypeOutsideClass = 31047
        ERR_DefaultPropertyWithNoParams = 31048
        ERR_InitializerInStruct = 31049
        ERR_DuplicateImport1 = 31051
        ERR_BadModuleFlags1 = 31052
        ERR_ImplementsStmtWrongOrder = 31053
        ERR_MemberConflictWithSynth4 = 31058
        ERR_SynthMemberClashesWithSynth7 = 31059
        ERR_SynthMemberClashesWithMember5 = 31060
        ERR_MemberClashesWithSynth6 = 31061
        ERR_SetHasOnlyOneParam = 31063
        ERR_SetValueNotPropertyType = 31064
        ERR_SetHasToBeByVal1 = 31065
        ERR_StructureCantUseProtected = 31067
        ERR_BadInterfaceDelegateSpecifier1 = 31068
        ERR_BadInterfaceEnumSpecifier1 = 31069
        ERR_BadInterfaceClassSpecifier1 = 31070
        ERR_BadInterfaceStructSpecifier1 = 31071
        'ERR_WarningTreatedAsError = 31072
        'ERR_DelegateConstructorMissing1 = 31074    unused in Roslyn
        ERR_UseOfObsoleteSymbolNoMessage1 = 31075
        ERR_MetaDataIsNotAssembly = 31076
        ERR_MetaDataIsNotModule = 31077
        ERR_ReferenceComparison3 = 31080
        ERR_CatchVariableNotLocal1 = 31082
        ERR_ModuleMemberCantImplement = 31083
        ERR_EventDelegatesCantBeFunctions = 31084
        ERR_InvalidDate = 31085
        ERR_CantOverride4 = 31086
        ERR_CantSpecifyArraysOnBoth = 31087
        ERR_NotOverridableRequiresOverrides = 31088
        ERR_PrivateTypeOutsideType = 31089
        ERR_TypeRefResolutionError3 = 31091
        ERR_ParamArrayWrongType = 31092
        ERR_CoClassMissing2 = 31094
        ERR_InvalidMeReference = 31095
        ERR_InvalidImplicitMeReference = 31096
        ERR_RuntimeMemberNotFound2 = 31097
        'ERR_RuntimeClassNotFound1 = 31098
        ERR_BadPropertyAccessorFlags = 31099
        ERR_BadPropertyAccessorFlagsRestrict = 31100
        ERR_OnlyOneAccessorForGetSet = 31101
        ERR_NoAccessibleSet = 31102
        ERR_NoAccessibleGet = 31103
        ERR_WriteOnlyNoAccessorFlag = 31104
        ERR_ReadOnlyNoAccessorFlag = 31105
        ERR_BadPropertyAccessorFlags1 = 31106
        ERR_BadPropertyAccessorFlags2 = 31107
        ERR_BadPropertyAccessorFlags3 = 31108
        ERR_InAccessibleCoClass3 = 31109
        ERR_MissingValuesForArraysInApplAttrs = 31110
        ERR_ExitEventMemberNotInvalid = 31111
        ERR_InvInsideEndsEvent = 31112
        'ERR_EventMemberSyntax = 31113  abandoned per KevinH analysis. Roslyn bug 1637
        ERR_MissingEndEvent = 31114
        ERR_MissingEndAddHandler = 31115
        ERR_MissingEndRemoveHandler = 31116
        ERR_MissingEndRaiseEvent = 31117
        'ERR_EndAddHandlerNotAtLineStart = 31118
        'ERR_EndRemoveHandlerNotAtLineStart = 31119
        'ERR_EndRaiseEventNotAtLineStart = 31120
        ERR_CustomEventInvInInterface = 31121
        ERR_CustomEventRequiresAs = 31122
        ERR_InvalidEndEvent = 31123
        ERR_InvalidEndAddHandler = 31124
        ERR_InvalidEndRemoveHandler = 31125
        ERR_InvalidEndRaiseEvent = 31126
        ERR_DuplicateAddHandlerDef = 31127
        ERR_DuplicateRemoveHandlerDef = 31128
        ERR_DuplicateRaiseEventDef = 31129
        ERR_MissingAddHandlerDef1 = 31130
        ERR_MissingRemoveHandlerDef1 = 31131
        ERR_MissingRaiseEventDef1 = 31132
        ERR_EventAddRemoveHasOnlyOneParam = 31133
        ERR_EventAddRemoveByrefParamIllegal = 31134
        ERR_SpecifiersInvOnEventMethod = 31135
        ERR_AddRemoveParamNotEventType = 31136
        ERR_RaiseEventShapeMismatch1 = 31137
        ERR_EventMethodOptionalParamIllegal1 = 31138
        ERR_CantReferToMyGroupInsideGroupType1 = 31139
        ERR_InvalidUseOfCustomModifier = 31140
        ERR_InvalidOptionStrictCustom = 31141
        ERR_ObsoleteInvalidOnEventMember = 31142
        ERR_DelegateBindingIncompatible2 = 31143
        ERR_ExpectedXmlName = 31146
        ERR_UndefinedXmlPrefix = 31148
        ERR_DuplicateXmlAttribute = 31149
        ERR_MismatchedXmlEndTag = 31150
        ERR_MissingXmlEndTag = 31151
        ERR_ReservedXmlPrefix = 31152
        ERR_MissingVersionInXmlDecl = 31153
        ERR_IllegalAttributeInXmlDecl = 31154
        ERR_QuotedEmbeddedExpression = 31155
        ERR_VersionMustBeFirstInXmlDecl = 31156
        ERR_AttributeOrder = 31157
        'ERR_UnexpectedXmlName = 31158
        ERR_ExpectedXmlEndEmbedded = 31159
        ERR_ExpectedXmlEndPI = 31160
        ERR_ExpectedXmlEndComment = 31161
        ERR_ExpectedXmlEndCData = 31162
        ERR_ExpectedSQuote = 31163
        ERR_ExpectedQuote = 31164
        ERR_ExpectedLT = 31165
        ERR_StartAttributeValue = 31166
        ERR_ExpectedDiv = 31167
        ERR_NoXmlAxesLateBinding = 31168
        ERR_IllegalXmlStartNameChar = 31169
        ERR_IllegalXmlNameChar = 31170
        ERR_IllegalXmlCommentChar = 31171
        ERR_EmbeddedExpression = 31172
        ERR_ExpectedXmlWhiteSpace = 31173
        ERR_IllegalProcessingInstructionName = 31174
        ERR_DTDNotSupported = 31175
        'ERR_IllegalXmlChar = 31176     unused in Dev10
        ERR_IllegalXmlWhiteSpace = 31177
        ERR_ExpectedSColon = 31178
        ERR_ExpectedXmlBeginEmbedded = 31179
        ERR_XmlEntityReference = 31180
        ERR_InvalidAttributeValue1 = 31181
        ERR_InvalidAttributeValue2 = 31182
        ERR_ReservedXmlNamespace = 31183
        ERR_IllegalDefaultNamespace = 31184
        'ERR_RequireAggregateInitializationImpl = 31185
        ERR_QualifiedNameNotAllowed = 31186
        ERR_ExpectedXmlns = 31187
        'ERR_DefaultNamespaceNotSupported = 31188 Not reported by Dev10.
        ERR_IllegalXmlnsPrefix = 31189
        ERR_XmlFeaturesNotAvailable = 31190
        'ERR_UnableToEmbedUacManifest = 31191   now reporting ErrorCreatingWin32ResourceFile
        ERR_UnableToReadUacManifest2 = 31192
        'ERR_UseValueForXmlExpression3 = 31193 ' Replaced by WRN_UseValueForXmlExpression3
        ERR_TypeMismatchForXml3 = 31194
        ERR_BinaryOperandsForXml4 = 31195
        'ERR_XmlFeaturesNotAvailableDebugger = 31196
        ERR_FullWidthAsXmlDelimiter = 31197
        'ERR_XmlRequiresParens = 31198 No Longer Reported. Removed per 926946.
        ERR_XmlEndCDataNotAllowedInContent = 31198
        'ERR_UacALink3Missing = 31199               not used in Roslyn.
        'ERR_XmlFeaturesNotSupported = 31200        not detected by the Roslyn compiler
        ERR_EventImplRemoveHandlerParamWrong = 31201
        ERR_MixingWinRTAndNETEvents = 31202
        ERR_AddParamWrongForWinRT = 31203
        ERR_RemoveParamWrongForWinRT = 31204
        ERR_ReImplementingWinRTInterface5 = 31205
        ERR_ReImplementingWinRTInterface4 = 31206
        ERR_XmlEndElementNoMatchingStart = 31207
        ERR_UndefinedTypeOrNamespace1 = 31208
        ERR_BadInterfaceInterfaceSpecifier1 = 31209
        ERR_TypeClashesWithVbCoreType4 = 31210
        ERR_SecurityAttributeMissingAction = 31211
        ERR_SecurityAttributeInvalidAction = 31212
        ERR_SecurityAttributeInvalidActionAssembly = 31213
        ERR_SecurityAttributeInvalidActionTypeOrMethod = 31214
        ERR_PrincipalPermissionInvalidAction = 31215
        ERR_PermissionSetAttributeInvalidFile = 31216
        ERR_PermissionSetAttributeFileReadError = 31217
        ERR_ExpectedWarningKeyword = 31218
        ERR_InvalidHashAlgorithmName = 31219

        '// NOTE: If you add any new errors that may be attached to a symbol during meta-import when it is marked as bad,
        '//       particularly if it applies to method symbols, please appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember.
        '//       Failure to do so may break customer code.
        '// AVAILABLE                             31220-31390

        ERR_InvalidSubsystemVersion = 31391
        ERR_LibAnycpu32bitPreferredConflict = 31392
        ERR_RestrictedAccess = 31393
        ERR_RestrictedConversion1 = 31394
        ERR_NoTypecharInLabel = 31395
        ERR_RestrictedType1 = 31396
        ERR_NoTypecharInAlias = 31398
        ERR_NoAccessibleConstructorOnBase = 31399
        ERR_BadStaticLocalInStruct = 31400
        ERR_DuplicateLocalStatic1 = 31401
        ERR_ImportAliasConflictsWithType2 = 31403
        ERR_CantShadowAMustOverride1 = 31404
        'ERR_OptionalsCantBeStructs = 31405 
        ERR_MultipleEventImplMismatch3 = 31407
        ERR_BadSpecifierCombo2 = 31408
        ERR_MustBeOverloads2 = 31409
        'ERR_CantOverloadOnMultipleInheritance = 31410
        ERR_MustOverridesInClass1 = 31411
        ERR_HandlesSyntaxInClass = 31412
        ERR_SynthMemberShadowsMustOverride5 = 31413
        'ERR_CantImplementNonVirtual3 = 31415   unused in Roslyn
        ' ERR_MemberShadowsSynthMustOverride5 = 31416   unused in Roslyn
        ERR_CannotOverrideInAccessibleMember = 31417
        ERR_HandlesSyntaxInModule = 31418
        ERR_IsNotOpRequiresReferenceTypes1 = 31419
        ERR_ClashWithReservedEnumMember1 = 31420
        ERR_MultiplyDefinedEnumMember2 = 31421
        ERR_BadUseOfVoid = 31422
        ERR_EventImplMismatch5 = 31423
        ERR_ForwardedTypeUnavailable3 = 31424
        ERR_TypeFwdCycle2 = 31425
        ERR_BadTypeInCCExpression = 31426
        ERR_BadCCExpression = 31427
        ERR_VoidArrayDisallowed = 31428
        ERR_MetadataMembersAmbiguous3 = 31429
        ERR_TypeOfExprAlwaysFalse2 = 31430
        ERR_OnlyPrivatePartialMethods1 = 31431
        ERR_PartialMethodsMustBePrivate = 31432
        ERR_OnlyOnePartialMethodAllowed2 = 31433
        ERR_OnlyOneImplementingMethodAllowed3 = 31434
        ERR_PartialMethodMustBeEmpty = 31435
        ERR_PartialMethodsMustBeSub1 = 31437
        ERR_PartialMethodGenericConstraints2 = 31438
        ERR_PartialDeclarationImplements1 = 31439
        ERR_NoPartialMethodInAddressOf1 = 31440
        ERR_ImplementationMustBePrivate2 = 31441
        ERR_PartialMethodParamNamesMustMatch3 = 31442
        ERR_PartialMethodTypeParamNameMismatch3 = 31443
        ERR_PropertyDoesntImplementAllAccessors = 31444
        ERR_InvalidAttributeUsageOnAccessor = 31445
        ERR_NestedTypeInInheritsClause2 = 31446
        ERR_TypeInItsInheritsClause1 = 31447
        ERR_BaseTypeReferences2 = 31448
        ERR_IllegalBaseTypeReferences3 = 31449
        ERR_InvalidCoClass1 = 31450
        ERR_InvalidOutputName = 31451
        ERR_InvalidFileAlignment = 31452
        ERR_InvalidDebugInformationFormat = 31453

        '// NOTE: If you add any new errors that may be attached to a symbol during meta-import when it is marked as bad,
        '//       particularly if it applies to method symbols, please appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember.
        '//       Failure to do so may break customer code.

        '// AVAILABLE                             31451 - 31497
        ERR_ConstantStringTooLong = 31498
        ERR_MustInheritEventNotOverridden = 31499
        ERR_BadAttributeSharedProperty1 = 31500
        ERR_BadAttributeReadOnlyProperty1 = 31501
        ERR_DuplicateResourceName1 = 31502
        ERR_AttributeMustBeClassNotStruct1 = 31503
        ERR_AttributeMustInheritSysAttr = 31504
        'ERR_AttributeMustHaveAttrUsageAttr = 31505  unused in Roslyn.
        ERR_AttributeCannotBeAbstract = 31506
        ' ERR_AttributeCannotHaveMustOverride = 31507 - reported by dev10 but error is redundant. ERR_AttributeCannotBeAbstract covers this case.
        'ERR_CantFindCORSystemDirectory = 31508
        ERR_UnableToOpenResourceFile1 = 31509
        'ERR_BadAttributeConstField1 = 31510
        ERR_BadAttributeNonPublicProperty1 = 31511
        ERR_STAThreadAndMTAThread0 = 31512
        'ERR_STAThreadAndMTAThread1 = 31513

        '//If you make any change involving this error, such as creating a more specific version for use
        '//in a particular context, please make sure to appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember
        ERR_IndirectUnreferencedAssembly4 = 31515

        ERR_BadAttributeNonPublicType1 = 31516
        ERR_BadAttributeNonPublicContType2 = 31517
        'ERR_AlinkManifestFilepathTooLong = 31518       this scenario reports a more generic error
        ERR_BadMetaDataReference1 = 31519
        ' ERR_ErrorApplyingSecurityAttribute1 = 31520 ' ' we're now reporting more detailed diagnostics: ERR_SecurityAttributeMissingAction,  ERR_SecurityAttributeInvalidAction, ERR_SecurityAttributeInvalidActionAssembly or ERR_SecurityAttributeInvalidActionTypeOrMethod
        'ERR_DuplicateModuleAttribute1 = 31521
        ERR_DllImportOnNonEmptySubOrFunction = 31522
        ERR_DllImportNotLegalOnDeclare = 31523
        ERR_DllImportNotLegalOnGetOrSet = 31524
        'ERR_TypeImportedFromDiffAssemVersions3 = 31525
        ERR_DllImportOnGenericSubOrFunction = 31526
        ERR_ComClassOnGeneric = 31527

        '//If you make any change involving this error, such as creating a more specific version for use
        '//in a particular context, please make sure to appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember
        'ERR_IndirectUnreferencedAssembly3 = 31528

        ERR_DllImportOnInstanceMethod = 31529
        ERR_DllImportOnInterfaceMethod = 31530
        ERR_DllImportNotLegalOnEventMethod = 31531

        '//If you make any change involving these errors, such as creating more specific versions for use
        '//in other contexts, please make sure to appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember
        'ERR_IndirectUnreferencedProject3 = 31532
        'ERR_IndirectUnreferencedProject2 = 31533

        ERR_FriendAssemblyBadArguments = 31534
        ERR_FriendAssemblyStrongNameRequired = 31535
        'ERR_FriendAssemblyRejectBinding = 31536    EDMAURER This has been replaced with two, more specific errors ERR_FriendRefNotEqualToThis and ERR_FriendRefSigningMismatch.

        ERR_FriendAssemblyNameInvalid = 31537
        ERR_FriendAssemblyBadAccessOverride2 = 31538

        ERR_AbsentReferenceToPIA1 = 31539
        'ERR_CorlibMissingPIAClasses1 = 31540       EDMAURER Roslyn uses the ordinary missing required type message
        ERR_CannotLinkClassWithNoPIA1 = 31541
        ERR_InvalidStructMemberNoPIA1 = 31542
        ERR_NoPIAAttributeMissing2 = 31543

        ERR_NestedGlobalNamespace = 31544

        'ERR_NewCoClassNoPIA = 31545                EDMAURER Roslyn gives 31541
        'ERR_EventNoPIANoDispID = 31546
        'ERR_EventNoPIANoGuid1 = 31547
        'ERR_EventNoPIANoComEventInterface1 = 31548
        ERR_PIAHasNoAssemblyGuid1 = 31549
        'ERR_StructureExplicitFieldLacksOffset = 31550
        'ERR_CannotLinkEventInterfaceWithNoPIA1 = 31551
        ERR_DuplicateLocalTypes3 = 31552
        ERR_PIAHasNoTypeLibAttribute1 = 31553
        ' ERR_NoPiaEventsMissingSystemCore = 31554      use ordinary missing required type
        ' ERR_SourceInterfaceMustExist = 31555
        ERR_SourceInterfaceMustBeInterface = 31556
        ERR_EventNoPIANoBackingMember = 31557
        ERR_NestedInteropType = 31558       ' used to be ERR_InvalidInteropType
        ERR_IsNestedIn2 = 31559
        ERR_LocalTypeNameClash2 = 31560
        ERR_InteropMethodWithBody1 = 31561

        ERR_UseOfLocalBeforeDeclaration1 = 32000
        ERR_UseOfKeywordFromModule1 = 32001
        'ERR_UseOfKeywordOutsideClass1 = 32002
        'ERR_SymbolFromUnreferencedProject3 = 32004
        ERR_BogusWithinLineIf = 32005
        ERR_CharToIntegralTypeMismatch1 = 32006
        ERR_IntegralToCharTypeMismatch1 = 32007
        ERR_NoDirectDelegateConstruction1 = 32008
        ERR_MethodMustBeFirstStatementOnLine = 32009
        ERR_AttrAssignmentNotFieldOrProp1 = 32010
        ERR_StrictDisallowsObjectComparison1 = 32013
        ERR_NoConstituentArraySizes = 32014
        ERR_FileAttributeNotAssemblyOrModule = 32015
        ERR_FunctionResultCannotBeIndexed1 = 32016
        ERR_ArgumentSyntax = 32017
        ERR_ExpectedResumeOrGoto = 32019
        ERR_ExpectedAssignmentOperator = 32020
        ERR_NamedArgAlsoOmitted2 = 32021
        ERR_CannotCallEvent1 = 32022
        ERR_ForEachCollectionDesignPattern1 = 32023
        ERR_DefaultValueForNonOptionalParam = 32024
        ' ERR_RegionWithinMethod = 32025    removed this limitation in Roslyn
        'ERR_SpecifiersInvalidOnNamespace = 32026   abandoned, now giving 'Specifiers and attributes are not valid on this statement.'
        ERR_ExpectedDotAfterMyBase = 32027
        ERR_ExpectedDotAfterMyClass = 32028
        ERR_StrictArgumentCopyBackNarrowing3 = 32029
        ERR_LbElseifAfterElse = 32030
        'ERR_EndSubNotAtLineStart = 32031
        'ERR_EndFunctionNotAtLineStart = 32032
        'ERR_EndGetNotAtLineStart = 32033
        'ERR_EndSetNotAtLineStart = 32034
        ERR_StandaloneAttribute = 32035
        ERR_NoUniqueConstructorOnBase2 = 32036
        ERR_ExtraNextVariable = 32037
        ERR_RequiredNewCallTooMany2 = 32038
        ERR_ForCtlVarArraySizesSpecified = 32039
        ERR_BadFlagsOnNewOverloads = 32040
        ERR_TypeCharOnGenericParam = 32041
        ERR_TooFewGenericArguments1 = 32042
        ERR_TooManyGenericArguments1 = 32043
        ERR_GenericConstraintNotSatisfied2 = 32044
        ERR_TypeOrMemberNotGeneric1 = 32045
        ERR_NewIfNullOnGenericParam = 32046
        ERR_MultipleClassConstraints1 = 32047
        ERR_ConstNotClassInterfaceOrTypeParam1 = 32048
        ERR_DuplicateTypeParamName1 = 32049
        ERR_UnboundTypeParam2 = 32050
        ERR_IsOperatorGenericParam1 = 32052
        ERR_ArgumentCopyBackNarrowing3 = 32053
        ERR_ShadowingGenericParamWithMember1 = 32054
        ERR_GenericParamBase2 = 32055
        ERR_ImplementsGenericParam = 32056
        'ERR_ExpressionCannotBeGeneric1 = 32058 unused in Roslyn
        ERR_OnlyNullLowerBound = 32059
        ERR_ClassConstraintNotInheritable1 = 32060
        ERR_ConstraintIsRestrictedType1 = 32061
        ERR_GenericParamsOnInvalidMember = 32065
        ERR_GenericArgsOnAttributeSpecifier = 32066
        ERR_AttrCannotBeGenerics = 32067
        ERR_BadStaticLocalInGenericMethod = 32068
        ERR_SyntMemberShadowsGenericParam3 = 32070
        ERR_ConstraintAlreadyExists1 = 32071
        ERR_InterfacePossiblyImplTwice2 = 32072
        ERR_ModulesCannotBeGeneric = 32073
        ERR_GenericClassCannotInheritAttr = 32074
        ERR_DeclaresCantBeInGeneric = 32075
        'ERR_GenericTypeRequiresTypeArgs1 = 32076
        ERR_OverrideWithConstraintMismatch2 = 32077
        ERR_ImplementsWithConstraintMismatch3 = 32078
        ERR_OpenTypeDisallowed = 32079
        ERR_HandlesInvalidOnGenericMethod = 32080
        ERR_MultipleNewConstraints = 32081
        ERR_MustInheritForNewConstraint2 = 32082
        ERR_NoSuitableNewForNewConstraint2 = 32083
        ERR_BadGenericParamForNewConstraint2 = 32084
        ERR_NewArgsDisallowedForTypeParam = 32085
        ERR_DuplicateRawGenericTypeImport1 = 32086
        ERR_NoTypeArgumentCountOverloadCand1 = 32087
        ERR_TypeArgsUnexpected = 32088
        ERR_NameSameAsMethodTypeParam1 = 32089
        ERR_TypeParamNameFunctionNameCollision = 32090
        'ERR_OverloadsMayUnify2 = 32091 unused in Roslyn
        ERR_BadConstraintSyntax = 32092
        ERR_OfExpected = 32093
        ERR_ArrayOfRawGenericInvalid = 32095
        ERR_ForEachAmbiguousIEnumerable1 = 32096
        ERR_IsNotOperatorGenericParam1 = 32097
        ERR_TypeParamQualifierDisallowed = 32098
        ERR_TypeParamMissingCommaOrRParen = 32099
        ERR_TypeParamMissingAsCommaOrRParen = 32100
        ERR_MultipleReferenceConstraints = 32101
        ERR_MultipleValueConstraints = 32102
        ERR_NewAndValueConstraintsCombined = 32103
        ERR_RefAndValueConstraintsCombined = 32104
        ERR_BadTypeArgForStructConstraint2 = 32105
        ERR_BadTypeArgForRefConstraint2 = 32106
        ERR_RefAndClassTypeConstrCombined = 32107
        ERR_ValueAndClassTypeConstrCombined = 32108
        ERR_ConstraintClashIndirectIndirect4 = 32109
        ERR_ConstraintClashDirectIndirect3 = 32110
        ERR_ConstraintClashIndirectDirect3 = 32111
        ERR_ConstraintCycleLink2 = 32112
        ERR_ConstraintCycle2 = 32113
        ERR_TypeParamWithStructConstAsConst = 32114
        ERR_NullableDisallowedForStructConstr1 = 32115
        'ERR_NoAccessibleNonGeneric1 = 32117
        'ERR_NoAccessibleGeneric1 = 32118
        ERR_ConflictingDirectConstraints3 = 32119
        ERR_InterfaceUnifiesWithInterface2 = 32120
        ERR_BaseUnifiesWithInterfaces3 = 32121
        ERR_InterfaceBaseUnifiesWithBase4 = 32122
        ERR_InterfaceUnifiesWithBase3 = 32123

        ERR_OptionalsCantBeStructGenericParams = 32124 'TODO: remove

        'ERR_InterfaceMethodImplsUnify3 = 32125
        ERR_AddressOfNullableMethod = 32126
        ERR_IsOperatorNullable1 = 32127
        ERR_IsNotOperatorNullable1 = 32128
        'ERR_NullableOnEnum = 32129
        'ERR_NoNullableType = 32130 unused in Roslyn

        ERR_ClassInheritsBaseUnifiesWithInterfaces3 = 32131
        ERR_ClassInheritsInterfaceBaseUnifiesWithBase4 = 32132
        ERR_ClassInheritsInterfaceUnifiesWithBase3 = 32133

        ERR_ShadowingTypeOutsideClass1 = 32200
        ERR_PropertySetParamCollisionWithValue = 32201
        'ERR_EventNameTooLong = 32204 ' Deprecated in favor of ERR_TooLongMetadataName
        'ERR_WithEventsNameTooLong = 32205 ' Deprecated in favor of ERR_TooLongMetadataName

        ERR_SxSIndirectRefHigherThanDirectRef3 = 32207
        ERR_DuplicateReference2 = 32208
        'ERR_SxSLowerVerIndirectRefNotEmitted4 = 32209  not used in Roslyn
        ERR_DuplicateReferenceStrong = 32210

        ERR_IllegalCallOrIndex = 32303
        ERR_ConflictDefaultPropertyAttribute = 32304

        'ERR_ClassCannotCreated = 32400

        ERR_BadAttributeUuid2 = 32500
        ERR_ComClassAndReservedAttribute1 = 32501
        ERR_ComClassRequiresPublicClass2 = 32504
        ERR_ComClassReservedDispIdZero1 = 32505
        ERR_ComClassReservedDispId1 = 32506
        ERR_ComClassDuplicateGuids1 = 32507
        ERR_ComClassCantBeAbstract0 = 32508
        ERR_ComClassRequiresPublicClass1 = 32509
        'ERR_DefaultCharSetAttributeNotSupported = 32510

        ERR_UnknownOperator = 33000
        ERR_DuplicateConversionCategoryUsed = 33001
        ERR_OperatorNotOverloadable = 33002
        ERR_InvalidHandles = 33003
        ERR_InvalidImplements = 33004
        ERR_EndOperatorExpected = 33005
        ERR_EndOperatorNotAtLineStart = 33006
        ERR_InvalidEndOperator = 33007
        ERR_ExitOperatorNotValid = 33008
        ERR_ParamArrayIllegal1 = 33009
        ERR_OptionalIllegal1 = 33010
        ERR_OperatorMustBePublic = 33011
        ERR_OperatorMustBeShared = 33012
        ERR_BadOperatorFlags1 = 33013
        ERR_OneParameterRequired1 = 33014
        ERR_TwoParametersRequired1 = 33015
        ERR_OneOrTwoParametersRequired1 = 33016
        ERR_ConvMustBeWideningOrNarrowing = 33017
        ERR_OperatorDeclaredInModule = 33018
        ERR_InvalidSpecifierOnNonConversion1 = 33019
        ERR_UnaryParamMustBeContainingType1 = 33020
        ERR_BinaryParamMustBeContainingType1 = 33021
        ERR_ConvParamMustBeContainingType1 = 33022
        ERR_OperatorRequiresBoolReturnType1 = 33023
        ERR_ConversionToSameType = 33024
        ERR_ConversionToInterfaceType = 33025
        ERR_ConversionToBaseType = 33026
        ERR_ConversionToDerivedType = 33027
        ERR_ConversionToObject = 33028
        ERR_ConversionFromInterfaceType = 33029
        ERR_ConversionFromBaseType = 33030
        ERR_ConversionFromDerivedType = 33031
        ERR_ConversionFromObject = 33032
        ERR_MatchingOperatorExpected2 = 33033
        ERR_UnacceptableLogicalOperator3 = 33034
        ERR_ConditionOperatorRequired3 = 33035
        ERR_CopyBackTypeMismatch3 = 33037
        ERR_ForLoopOperatorRequired2 = 33038
        ERR_UnacceptableForLoopOperator2 = 33039
        ERR_UnacceptableForLoopRelOperator2 = 33040
        ERR_OperatorRequiresIntegerParameter1 = 33041

        ERR_CantSpecifyNullableOnBoth = 33100
        ERR_BadTypeArgForStructConstraintNull = 33101
        ERR_CantSpecifyArrayAndNullableOnBoth = 33102
        ERR_CantSpecifyTypeCharacterOnIIF = 33103
        ERR_IllegalOperandInIIFCount = 33104
        ERR_IllegalOperandInIIFName = 33105
        ERR_IllegalOperandInIIFConversion = 33106
        ERR_IllegalCondTypeInIIF = 33107
        ERR_CantCallIIF = 33108
        ERR_CantSpecifyAsNewAndNullable = 33109
        ERR_IllegalOperandInIIFConversion2 = 33110
        ERR_BadNullTypeInCCExpression = 33111
        ERR_NullableImplicit = 33112

        '// NOTE: If you add any new errors that may be attached to a symbol during meta-import when it is marked as bad,
        '//       particularly if it applies to method symbols, please appropriately modify Bindable::ResolveOverloadingShouldSkipBadMember.
        '//       Failure to do so may break customer code.
        '// AVAILABLE                             33113 - 34999

        ERR_MissingRuntimeHelper = 35000
        'ERR_NoStdModuleAttribute = 35001 ' Note: we're now reporting a use site error in this case.
        'ERR_NoOptionTextAttribute = 35002
        ERR_DuplicateResourceFileName1 = 35003

        ERR_ExpectedDotAfterGlobalNameSpace = 36000
        ERR_NoGlobalExpectedIdentifier = 36001
        ERR_NoGlobalInHandles = 36002
        ERR_ElseIfNoMatchingIf = 36005
        ERR_BadAttributeConstructor2 = 36006
        ERR_EndUsingWithoutUsing = 36007
        ERR_ExpectedEndUsing = 36008
        ERR_GotoIntoUsing = 36009
        ERR_UsingRequiresDisposePattern = 36010
        ERR_UsingResourceVarNeedsInitializer = 36011
        ERR_UsingResourceVarCantBeArray = 36012
        ERR_OnErrorInUsing = 36013
        ERR_PropertyNameConflictInMyCollection = 36015
        ERR_InvalidImplicitVar = 36016

        ERR_ObjectInitializerRequiresFieldName = 36530
        ERR_ExpectedFrom = 36531
        ERR_LambdaBindingMismatch1 = 36532
        ERR_CannotLiftByRefParamQuery1 = 36533
        ERR_ExpressionTreeNotSupported = 36534
        ERR_CannotLiftStructureMeQuery = 36535
        ERR_InferringNonArrayType1 = 36536
        ERR_ByRefParamInExpressionTree = 36538
        'ERR_ObjectInitializerBadValue = 36543

        '// If you change this message, make sure to change message for QueryDuplicateAnonTypeMemberName1 as well!
        ERR_DuplicateAnonTypeMemberName1 = 36547

        ERR_BadAnonymousTypeForExprTree = 36548
        ERR_CannotLiftAnonymousType1 = 36549

        ERR_ExtensionOnlyAllowedOnModuleSubOrFunction = 36550
        ERR_ExtensionMethodNotInModule = 36551
        ERR_ExtensionMethodNoParams = 36552
        ERR_ExtensionMethodOptionalFirstArg = 36553
        ERR_ExtensionMethodParamArrayFirstArg = 36554
        '// If you change this message, make sure to change message for  QueryAnonymousTypeFieldNameInference as well!
        'ERR_BadOrCircularInitializerReference = 36555
        ERR_AnonymousTypeFieldNameInference = 36556
        ERR_NameNotMemberOfAnonymousType2 = 36557
        ERR_ExtensionAttributeInvalid = 36558
        ERR_AnonymousTypePropertyOutOfOrder1 = 36559
        '// If you change this message, make sure to change message for  QueryAnonymousTypeDisallowsTypeChar as well!
        ERR_AnonymousTypeDisallowsTypeChar = 36560
        ERR_ExtensionMethodUncallable1 = 36561
        ERR_ExtensionMethodOverloadCandidate3 = 36562
        ERR_DelegateBindingMismatch = 36563
        ERR_DelegateBindingTypeInferenceFails = 36564
        ERR_TooManyArgs = 36565
        ERR_NamedArgAlsoOmitted1 = 36566
        ERR_NamedArgUsedTwice1 = 36567
        ERR_NamedParamNotFound1 = 36568
        ERR_OmittedArgument1 = 36569
        ERR_UnboundTypeParam1 = 36572
        ERR_ExtensionMethodOverloadCandidate2 = 36573
        ERR_AnonymousTypeNeedField = 36574
        ERR_AnonymousTypeNameWithoutPeriod = 36575
        ERR_AnonymousTypeExpectedIdentifier = 36576
        'ERR_NoAnonymousTypeInitializersInDebugger = 36577
        'ERR_TooFewGenericArguments = 36578
        'ERR_TooManyGenericArguments = 36579
        'ERR_DelegateBindingMismatch3_3 = 36580 unused in Roslyn
        'ERR_DelegateBindingTypeInferenceFails3 = 36581
        ERR_TooManyArgs2 = 36582
        ERR_NamedArgAlsoOmitted3 = 36583
        ERR_NamedArgUsedTwice3 = 36584
        ERR_NamedParamNotFound3 = 36585
        ERR_OmittedArgument3 = 36586
        ERR_UnboundTypeParam3 = 36589
        ERR_TooFewGenericArguments2 = 36590
        ERR_TooManyGenericArguments2 = 36591

        ERR_ExpectedInOrEq = 36592
        ERR_ExpectedQueryableSource = 36593
        ERR_QueryOperatorNotFound = 36594

        ERR_CannotUseOnErrorGotoWithClosure = 36595
        ERR_CannotGotoNonScopeBlocksWithClosure = 36597
        ERR_CannotLiftRestrictedTypeQuery = 36598

        ERR_QueryAnonymousTypeFieldNameInference = 36599
        ERR_QueryDuplicateAnonTypeMemberName1 = 36600
        ERR_QueryAnonymousTypeDisallowsTypeChar = 36601
        ERR_ReadOnlyInClosure = 36602
        ERR_ExprTreeNoMultiDimArrayCreation = 36603
        ERR_ExprTreeNoLateBind = 36604

        ERR_ExpectedBy = 36605
        ERR_QueryInvalidControlVariableName1 = 36606

        ERR_ExpectedIn = 36607
        'ERR_QueryStartsWithLet = 36608
        'ERR_NoQueryExpressionsInDebugger = 36609
        ERR_QueryNameNotDeclared = 36610
        '// Available 36611

        ERR_NestedFunctionArgumentNarrowing3 = 36612

        '// If you change this message, make sure to change message for  QueryAnonTypeFieldXMLNameInference as well!
        ERR_AnonTypeFieldXMLNameInference = 36613
        ERR_QueryAnonTypeFieldXMLNameInference = 36614

        ERR_ExpectedInto = 36615
        'ERR_AggregateStartsWithLet = 36616
        ERR_TypeCharOnAggregation = 36617
        ERR_ExpectedOn = 36618
        ERR_ExpectedEquals = 36619
        ERR_ExpectedAnd = 36620
        ERR_EqualsTypeMismatch = 36621
        ERR_EqualsOperandIsBad = 36622
        '// see 30581 (lambda version of addressof)
        ERR_LambdaNotDelegate1 = 36625
        '// see 30939 (lambda version of addressof)
        ERR_LambdaNotCreatableDelegate1 = 36626
        'ERR_NoLambdaExpressionsInDebugger = 36627
        ERR_CannotInferNullableForVariable1 = 36628
        ERR_NullableTypeInferenceNotSupported = 36629
        ERR_ExpectedJoin = 36631
        ERR_NullableParameterMustSpecifyType = 36632
        ERR_IterationVariableShadowLocal2 = 36633
        ERR_LambdasCannotHaveAttributes = 36634
        ERR_LambdaInSelectCaseExpr = 36635
        ERR_AddressOfInSelectCaseExpr = 36636
        ERR_NullableCharNotSupported = 36637

        '// The follow error messages are paired with other query specific messages above.  Please
        '// make sure to keep the two in sync
        ERR_CannotLiftStructureMeLambda = 36638
        ERR_CannotLiftByRefParamLambda1 = 36639
        ERR_CannotLiftRestrictedTypeLambda = 36640

        ERR_LambdaParamShadowLocal1 = 36641
        ERR_StrictDisallowImplicitObjectLambda = 36642
        ERR_CantSpecifyParamsOnLambdaParamNoType = 36643

        ERR_TypeInferenceFailure1 = 36644
        ERR_TypeInferenceFailure2 = 36645
        ERR_TypeInferenceFailure3 = 36646
        ERR_TypeInferenceFailureNoExplicit1 = 36647
        ERR_TypeInferenceFailureNoExplicit2 = 36648
        ERR_TypeInferenceFailureNoExplicit3 = 36649

        ERR_TypeInferenceFailureAmbiguous1 = 36650
        ERR_TypeInferenceFailureAmbiguous2 = 36651
        ERR_TypeInferenceFailureAmbiguous3 = 36652
        ERR_TypeInferenceFailureNoExplicitAmbiguous1 = 36653
        ERR_TypeInferenceFailureNoExplicitAmbiguous2 = 36654
        ERR_TypeInferenceFailureNoExplicitAmbiguous3 = 36655

        ERR_TypeInferenceFailureNoBest1 = 36656
        ERR_TypeInferenceFailureNoBest2 = 36657
        ERR_TypeInferenceFailureNoBest3 = 36658
        ERR_TypeInferenceFailureNoExplicitNoBest1 = 36659
        ERR_TypeInferenceFailureNoExplicitNoBest2 = 36660
        ERR_TypeInferenceFailureNoExplicitNoBest3 = 36661

        ERR_DelegateBindingMismatchStrictOff2 = 36663
        'ERR_TooDeepNestingOfParensInLambdaParam = 36664 - No Longer Reported. Removed per 926942

        ' ERR_InaccessibleReturnTypeOfSymbol1 = 36665
        ERR_InaccessibleReturnTypeOfMember2 = 36666

        ERR_LocalNamedSameAsParamInLambda1 = 36667
        ERR_MultilineLambdasCannotContainOnError = 36668
        'ERR_BranchOutOfMultilineLambda = 36669 obsolete - was not even reported in Dev10 any more.
        ERR_LambdaBindingMismatch2 = 36670
        'ERR_MultilineLambdaShadowLocal1 = 36671 'unused in Roslyn
        ERR_StaticInLambda = 36672
        ERR_MultilineLambdaMissingSub = 36673
        ERR_MultilineLambdaMissingFunction = 36674
        ERR_StatementLambdaInExpressionTree = 36675
        ' //ERR_StrictDisallowsImplicitLambda                 = 36676
        ' // replaced by LambdaNoType and LambdaNoTypeObjectDisallowed and LambdaTooManyTypesObjectDisallowed
        ERR_AttributeOnLambdaReturnType = 36677

        ERR_ExpectedIdentifierOrGroup = 36707
        ERR_UnexpectedGroup = 36708
        ERR_DelegateBindingMismatchStrictOff3 = 36709
        ERR_DelegateBindingIncompatible3 = 36710
        ERR_ArgumentNarrowing2 = 36711
        ERR_OverloadCandidate1 = 36712
        ERR_AutoPropertyInitializedInStructure = 36713
        ERR_InitializedExpandedProperty = 36714
        'ERR_NewExpandedProperty = 36715 'unused in Roslyn

        ERR_LanguageVersion = 36716
        ERR_ArrayInitNoType = 36717
        ERR_NotACollection1 = 36718
        ERR_NoAddMethod1 = 36719
        ERR_CantCombineInitializers = 36720
        ERR_EmptyAggregateInitializer = 36721

        ERR_VarianceDisallowedHere = 36722
        ERR_VarianceInterfaceNesting = 36723
        ERR_VarianceOutParamDisallowed1 = 36724
        ERR_VarianceInParamDisallowed1 = 36725
        ERR_VarianceOutParamDisallowedForGeneric3 = 36726
        ERR_VarianceInParamDisallowedForGeneric3 = 36727
        ERR_VarianceOutParamDisallowedHere2 = 36728
        ERR_VarianceInParamDisallowedHere2 = 36729
        ERR_VarianceOutParamDisallowedHereForGeneric4 = 36730
        ERR_VarianceInParamDisallowedHereForGeneric4 = 36731
        ERR_VarianceTypeDisallowed2 = 36732
        ERR_VarianceTypeDisallowedForGeneric4 = 36733
        ERR_LambdaTooManyTypesObjectDisallowed = 36734
        ERR_VarianceTypeDisallowedHere3 = 36735
        ERR_VarianceTypeDisallowedHereForGeneric5 = 36736
        ERR_AmbiguousCastConversion2 = 36737
        ERR_VariancePreventsSynthesizedEvents2 = 36738
        ERR_NestingViolatesCLS1 = 36739
        ERR_VarianceOutNullableDisallowed2 = 36740
        ERR_VarianceInNullableDisallowed2 = 36741
        ERR_VarianceOutByValDisallowed1 = 36742
        ERR_VarianceInReturnDisallowed1 = 36743
        ERR_VarianceOutConstraintDisallowed1 = 36744
        ERR_VarianceInReadOnlyPropertyDisallowed1 = 36745
        ERR_VarianceOutWriteOnlyPropertyDisallowed1 = 36746
        ERR_VarianceOutPropertyDisallowed1 = 36747
        ERR_VarianceInPropertyDisallowed1 = 36748
        ERR_VarianceOutByRefDisallowed1 = 36749
        ERR_VarianceInByRefDisallowed1 = 36750
        ERR_LambdaNoType = 36751
        ' //ERR_NoReturnStatementsForMultilineLambda  = 36752 
        ' // replaced by LambdaNoType and LambdaNoTypeObjectDisallowed
        'ERR_CollectionInitializerArity2 = 36753
        ERR_VarianceConversionFailedOut6 = 36754
        ERR_VarianceConversionFailedIn6 = 36755
        ERR_VarianceIEnumerableSuggestion3 = 36756
        ERR_VarianceConversionFailedTryOut4 = 36757
        ERR_VarianceConversionFailedTryIn4 = 36758
        ERR_AutoPropertyCantHaveParams = 36759
        ERR_IdentityDirectCastForFloat = 36760

        ERR_TypeDisallowsElements = 36807
        ERR_TypeDisallowsAttributes = 36808
        ERR_TypeDisallowsDescendants = 36809
        'ERR_XmlSchemaCompileError = 36810

        ERR_TypeOrMemberNotGeneric2 = 36907
        ERR_ExtensionMethodCannotBeLateBound = 36908
        ERR_TypeInferenceArrayRankMismatch1 = 36909

        ERR_QueryStrictDisallowImplicitObject = 36910
        ERR_IfNoType = 36911
        ERR_IfNoTypeObjectDisallowed = 36912
        ERR_IfTooManyTypesObjectDisallowed = 36913
        ERR_ArrayInitNoTypeObjectDisallowed = 36914
        ERR_ArrayInitTooManyTypesObjectDisallowed = 36915
        ERR_LambdaNoTypeObjectDisallowed = 36916
        ERR_OverloadsModifierInModule = 36917
        ERR_SubRequiresSingleStatement = 36918
        ERR_SubDisallowsStatement = 36919
        ERR_SubRequiresParenthesesLParen = 36920
        ERR_SubRequiresParenthesesDot = 36921
        ERR_SubRequiresParenthesesBang = 36922
        ERR_CannotEmbedInterfaceWithGeneric = 36923
        ERR_CannotUseGenericTypeAcrossAssemblyBoundaries = 36924
        ERR_CannotUseGenericBaseTypeAcrossAssemblyBoundaries = 36925
        ERR_BadAsyncByRefParam = 36926
        ERR_BadIteratorByRefParam = 36927
        'ERR_BadAsyncExpressionLambda = 36928 'unused in Roslyn
        ERR_BadAsyncInQuery = 36929
        ERR_BadGetAwaiterMethod1 = 36930
        'ERR_ExpressionTreeContainsAwait = 36931
        ERR_RestrictedResumableType1 = 36932
        ERR_BadAwaitNothing = 36933
        ERR_AsyncSubMain = 36934
        ERR_PartialMethodsMustNotBeAsync1 = 36935
        ERR_InvalidAsyncIteratorModifiers = 36936
        ERR_BadAwaitNotInAsyncMethodOrLambda = 36937
        ERR_BadIteratorReturn = 36938
        ERR_BadYieldInTryHandler = 36939
        ERR_BadYieldInNonIteratorMethod = 36940
        '// unused 36941
        ERR_BadReturnValueInIterator = 36942
        ERR_BadAwaitInTryHandler = 36943
        'ERR_BadAwaitObject = 36944 'unused in Roslyn
        ERR_BadAsyncReturn = 36945
        ERR_BadResumableAccessReturnVariable = 36946
        ERR_BadIteratorExpressionLambda = 36947
        'ERR_AwaitLibraryMissing = 36948
        'ERR_AwaitPattern1 = 36949
        ERR_ConstructorAsync = 36950
        ERR_InvalidLambdaModifier = 36951
        ERR_ReturnFromNonGenericTaskAsync = 36952
        'ERR_BadAutoPropertyFlags1 = 36953 'unused in Roslyn

        ERR_BadOverloadCandidates2 = 36954
        ERR_BadStaticInitializerInResumable = 36955
        ERR_ResumablesCannotContainOnError = 36956
        ERR_FriendRefNotEqualToThis = 36957
        ERR_FriendRefSigningMismatch = 36958
        ERR_FailureSigningAssembly = 36960
        ERR_SignButNoPrivateKey = 36961
        ERR_InvalidVersionFormat = 36962

        ERR_ExpectedSingleScript = 36963
        ERR_ReferenceDirectiveOnlyAllowedInScripts = 36964
        ERR_NamespaceNotAllowedInScript = 36965
        ERR_KeywordNotAllowedInScript = 36966

        ERR_ReservedAssemblyName = 36968

        ERR_ConstructorCannotBeDeclaredPartial = 36969
        ERR_ModuleEmitFailure = 36970

        ERR_ParameterNotValidForType = 36971
        ERR_MarshalUnmanagedTypeNotValidForFields = 36972
        ERR_MarshalUnmanagedTypeOnlyValidForFields = 36973
        ERR_AttributeParameterRequired1 = 36974
        ERR_AttributeParameterRequired2 = 36975
        ERR_InvalidVersionFormat2 = 36976
        ERR_InvalidAssemblyCultureForExe = 36977
        ERR_InvalidMultipleAttributeUsageInNetModule2 = 36978
        ERR_SecurityAttributeInvalidTarget = 36979

        ERR_PublicKeyFileFailure = 36980
        ERR_PublicKeyContainerFailure = 36981

        ERR_InvalidAssemblyCulture = 36982
        ERR_EncUpdateFailedMissingAttribute = 36983

        ERR_CantAwaitAsyncSub1 = 37001
        ERR_ResumableLambdaInExpressionTree = 37050
        ERR_DllImportOnResumableMethod = 37051
        ERR_CannotLiftRestrictedTypeResumable1 = 37052
        ERR_BadIsCompletedOnCompletedGetResult2 = 37053
        ERR_SynchronizedAsyncMethod = 37054
        ERR_BadAsyncReturnOperand1 = 37055
        ERR_DoesntImplementAwaitInterface2 = 37056
        ERR_BadAwaitInNonAsyncMethod = 37057
        ERR_BadAwaitInNonAsyncVoidMethod = 37058
        ERR_BadAwaitInNonAsyncLambda = 37059
        ERR_LoopControlMustNotAwait = 37060

        ERR_MyGroupCollectionAttributeCycle = 37201
        ERR_LiteralExpected = 37202
        ERR_PartialMethodDefaultParameterValueMismatch2 = 37203
        ERR_PartialMethodParamArrayMismatch2 = 37204

        ERR_NetModuleNameMismatch = 37205
        ERR_BadModuleName = 37206
        ERR_CmdOptionConflictsSource = 37207
        ERR_TypeForwardedToMultipleAssemblies = 37208
        ERR_InvalidSignaturePublicKey = 37209
        ERR_CollisionWithPublicTypeInModule = 37210
        ERR_ExportedTypeConflictsWithDeclaration = 37211
        ERR_ExportedTypesConflict = 37212
        ERR_AgnosticToMachineModule = 37213
        ERR_ConflictingMachineModule = 37214
        ERR_CryptoHashFailed = 37215
        ERR_CantHaveWin32ResAndManifest = 37216

        ERR_ForwardedTypeConflictsWithDeclaration = 37217
        ERR_ForwardedTypeConflictsWithExportedType = 37218
        ERR_ForwardedTypesConflict = 37219

        ERR_TooLongMetadataName = 37220
        ERR_MissingNetModuleReference = 37221
        ERR_UnsupportedModule1 = 37222
        ERR_UnsupportedEvent1 = 37223
        ERR_NetModuleNameMustBeUnique = 37224
        ERR_PDBWritingFailed = 37225
        ERR_ParamDefaultValueDiffersFromAttribute = 37226
        ERR_ResourceInModule = 37227
        ERR_FieldHasMultipleDistinctConstantValues = 37228

        ERR_AmbiguousInNamespaces2 = 37229
        ERR_EncNoPIAReference = 37230
        ERR_LinkedNetmoduleMetadataMustProvideFullPEImage = 37231
        ERR_CantReadRulesetFile = 37232
        ERR_MetadataReferencesNotSupported = 37233
        ERR_PlatformDoesntSupport = 37234
        ERR_CantUseRequiredAttribute = 37235
        ERR_EncodinglessSyntaxTree = 37236

        ERR_InvalidFormatSpecifier = 37237

        ERR_CannotBeMadeNullable1 = 37238
        ERR_BadConditionalWithRef = 37239
        ERR_NullPropagatingOpInExpressionTree = 37240
        ERR_TooLongOrComplexExpression = 37241

        ERR_BadPdbData = 37242
        ERR_AutoPropertyCantBeWriteOnly = 37243

        ERR_ExpressionDoesntHaveName = 37244
        ERR_InvalidNameOfSubExpression = 37245
        ERR_MethodTypeArgsUnexpected = 37246
        ERR_InReferencedAssembly = 37247
        ERR_EncReferenceToAddedMember = 37248
        ERR_InterpolationFormatWhitespace = 37249
        ERR_InterpolationAlignmentOutOfRange = 37250
        ERR_InterpolatedStringFactoryError = 37251
        ERR_DebugEntryPointNotSourceMethodDefinition = 37252
        ERR_InvalidPathMap = 37253
        ERR_PublicSignNoKey = 37254
        ERR_TooManyUserStrings = 37255
        ERR_PeWritingFailure = 37256

        ERR_OptionMustBeAbsolutePath = 37257
        ERR_DocFileGen = 37258

        ERR_TupleTooFewElements = 37259
        ERR_TupleReservedElementNameAnyPosition = 37260
        ERR_TupleReservedElementName = 37261
        ERR_TupleDuplicateElementName = 37262

        ERR_RefReturningCallInExpressionTree = 37263

        ERR_SourceLinkRequiresPdb = 37264
        ERR_CannotEmbedWithoutPdb = 37265

        ERR_InvalidInstrumentationKind = 37266

        ERR_ValueTupleTypeRefResolutionError1 = 37267

        ERR_TupleElementNamesAttributeMissing = 37268
        ERR_ExplicitTupleElementNamesAttribute = 37269
        ERR_TupleLiteralDisallowsTypeChar = 37270

        ERR_DuplicateProcDefWithDifferentTupleNames2 = 37271
        ERR_InterfaceImplementedTwiceWithDifferentTupleNames2 = 37272
        ERR_InterfaceImplementedTwiceWithDifferentTupleNames3 = 37273
        ERR_InterfaceImplementedTwiceWithDifferentTupleNamesReverse3 = 37274
        ERR_InterfaceImplementedTwiceWithDifferentTupleNames4 = 37275

        ERR_InterfaceInheritedTwiceWithDifferentTupleNames2 = 37276
        ERR_InterfaceInheritedTwiceWithDifferentTupleNames3 = 37277
        ERR_InterfaceInheritedTwiceWithDifferentTupleNamesReverse3 = 37278
        ERR_InterfaceInheritedTwiceWithDifferentTupleNames4 = 37279

        ERR_NewWithTupleTypeSyntax = 37280
        ERR_PredefinedValueTupleTypeMustBeStruct = 37281
        ERR_PublicSignNetModule = 37282
        ERR_BadAssemblyName = 37283

        ERR_Merge_conflict_marker_encountered = 37284

        ERR_BadSourceCodeKind = 37285
        ERR_BadDocumentationMode = 37286
        ERR_BadLanguageVersion = 37287
        ERR_InvalidPreprocessorConstantType = 37288
        ERR_TupleInferredNamesNotAvailable = 37289
        ERR_InvalidDebugInfo = 37290

        ERR_NoRefOutWhenRefOnly = 37300
        ERR_NoNetModuleOutputWhenRefOutOrRefOnly = 37301

        ERR_BadNonTrailingNamedArgument = 37302
        ERR_ExpectedNamedArgumentInAttributeList = 37303
        ERR_NamedArgumentSpecificationBeforeFixedArgumentInLateboundInvocation = 37304

        ERR_ValueTupleResolutionAmbiguous3 = 37305

        ERR_CommentsAfterLineContinuationNotAvailable1 = 37306

        ERR_DefaultInterfaceImplementationInNoPIAType = 37307
        ERR_ReAbstractionInNoPIAType = 37308
        ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation = 37309
        ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember = 37310

        '// WARNINGS BEGIN HERE
        WRN_UseOfObsoleteSymbol2 = 40000
        WRN_InvalidOverrideDueToTupleNames2 = 40001
        WRN_MustOverloadBase4 = 40003
        WRN_OverrideType5 = 40004
        WRN_MustOverride2 = 40005
        WRN_DefaultnessShadowed4 = 40007
        WRN_UseOfObsoleteSymbolNoMessage1 = 40008
        WRN_AssemblyGeneration0 = 40009
        WRN_AssemblyGeneration1 = 40010
        WRN_ComClassNoMembers1 = 40011
        WRN_SynthMemberShadowsMember5 = 40012
        WRN_MemberShadowsSynthMember6 = 40014
        WRN_SynthMemberShadowsSynthMember7 = 40018
        WRN_UseOfObsoletePropertyAccessor3 = 40019
        WRN_UseOfObsoletePropertyAccessor2 = 40020
        ' WRN_MemberShadowsMemberInModule5 = 40021      ' no repro in legacy test, most probably not reachable. Unused in Roslyn.
        ' WRN_SynthMemberShadowsMemberInModule5 = 40022 ' no repro in legacy test, most probably not reachable. Unused in Roslyn.
        ' WRN_MemberShadowsSynthMemberInModule6 = 40023 ' no repro in legacy test, most probably not reachable. Unused in Roslyn.
        ' WRN_SynthMemberShadowsSynthMemberMod7 = 40024 ' no repro in legacy test, most probably not reachable. Unused in Roslyn.
        WRN_FieldNotCLSCompliant1 = 40025
        WRN_BaseClassNotCLSCompliant2 = 40026
        WRN_ProcTypeNotCLSCompliant1 = 40027
        WRN_ParamNotCLSCompliant1 = 40028
        WRN_InheritedInterfaceNotCLSCompliant2 = 40029
        WRN_CLSMemberInNonCLSType3 = 40030
        WRN_NameNotCLSCompliant1 = 40031
        WRN_EnumUnderlyingTypeNotCLS1 = 40032
        WRN_NonCLSMemberInCLSInterface1 = 40033
        WRN_NonCLSMustOverrideInCLSType1 = 40034
        WRN_ArrayOverloadsNonCLS2 = 40035
        WRN_RootNamespaceNotCLSCompliant1 = 40038
        WRN_RootNamespaceNotCLSCompliant2 = 40039
        WRN_GenericConstraintNotCLSCompliant1 = 40040
        WRN_TypeNotCLSCompliant1 = 40041
        WRN_OptionalValueNotCLSCompliant1 = 40042
        WRN_CLSAttrInvalidOnGetSet = 40043
        WRN_TypeConflictButMerged6 = 40046
        ' WRN_TypeConflictButMerged7 = 40047  ' deprecated
        WRN_ShadowingGenericParamWithParam1 = 40048
        WRN_CannotFindStandardLibrary1 = 40049
        WRN_EventDelegateTypeNotCLSCompliant2 = 40050
        WRN_DebuggerHiddenIgnoredOnProperties = 40051
        WRN_SelectCaseInvalidRange = 40052
        WRN_CLSEventMethodInNonCLSType3 = 40053
        WRN_ExpectedInitComponentCall2 = 40054
        WRN_NamespaceCaseMismatch3 = 40055
        WRN_UndefinedOrEmptyNamespaceOrClass1 = 40056
        WRN_UndefinedOrEmptyProjectNamespaceOrClass1 = 40057
        'WRN_InterfacesWithNoPIAMustHaveGuid1 = 40058 ' Not reported by Dev11.
        WRN_IndirectRefToLinkedAssembly2 = 40059
        WRN_DelaySignButNoKey = 40060

        WRN_UnimplementedCommandLineSwitch = 40998

        ' WRN_DuplicateAssemblyAttribute1 = 41000   'unused in Roslyn
        WRN_NoNonObsoleteConstructorOnBase3 = 41001
        WRN_NoNonObsoleteConstructorOnBase4 = 41002
        WRN_RequiredNonObsoleteNewCall3 = 41003
        WRN_RequiredNonObsoleteNewCall4 = 41004
        WRN_MissingAsClauseinOperator = 41005

        WRN_ConstraintsFailedForInferredArgs2 = 41006
        WRN_ConditionalNotValidOnFunction = 41007
        WRN_UseSwitchInsteadOfAttribute = 41008
        WRN_TupleLiteralNameMismatch = 41009

        '// AVAILABLE                             41010 - 41199
        WRN_ReferencedAssemblyDoesNotHaveStrongName = 41997
        WRN_RecursiveAddHandlerCall = 41998
        WRN_ImplicitConversionCopyBack = 41999
        WRN_MustShadowOnMultipleInheritance2 = 42000
        ' WRN_ObsoleteClassInitialize = 42001     ' deprecated
        ' WRN_ObsoleteClassTerminate = 42002      ' deprecated
        WRN_RecursiveOperatorCall = 42004
        ' WRN_IndirectlyImplementedBaseMember5 = 42014 ' deprecated
        ' WRN_ImplementedBaseMember4 = 42015 ' deprecated

        WRN_ImplicitConversionSubst1 = 42016 '// populated by 42350/42332/42336/42337/42338/42339/42340
        WRN_LateBindingResolution = 42017
        WRN_ObjectMath1 = 42018
        WRN_ObjectMath2 = 42019
        WRN_ObjectAssumedVar1 = 42020  ' // populated by 42111/42346
        WRN_ObjectAssumed1 = 42021  ' // populated by 42347/41005/42341/42342/42344/42345/42334/42343
        WRN_ObjectAssumedProperty1 = 42022  ' // populated by 42348

        '// AVAILABLE                             42023

        WRN_UnusedLocal = 42024
        WRN_SharedMemberThroughInstance = 42025
        WRN_RecursivePropertyCall = 42026

        WRN_OverlappingCatch = 42029
        WRN_DefAsgUseNullRefByRef = 42030
        WRN_DuplicateCatch = 42031
        WRN_ObjectMath1Not = 42032

        WRN_BadChecksumValExtChecksum = 42033
        WRN_MultipleDeclFileExtChecksum = 42034
        WRN_BadGUIDFormatExtChecksum = 42035
        WRN_ObjectMathSelectCase = 42036
        WRN_EqualToLiteralNothing = 42037
        WRN_NotEqualToLiteralNothing = 42038

        '// AVAILABLE                             42039 - 42098
        WRN_UnusedLocalConst = 42099

        '// UNAVAILABLE                           42100
        WRN_ComClassInterfaceShadows5 = 42101
        WRN_ComClassPropertySetObject1 = 42102
        '// only reference types are considered for definite assignment.
        '// DefAsg's are all under VB_advanced
        WRN_DefAsgUseNullRef = 42104
        WRN_DefAsgNoRetValFuncRef1 = 42105
        WRN_DefAsgNoRetValOpRef1 = 42106
        WRN_DefAsgNoRetValPropRef1 = 42107
        WRN_DefAsgUseNullRefByRefStr = 42108
        WRN_DefAsgUseNullRefStr = 42109
        ' WRN_FieldInForNotExplicit = 42110       'unused in Roslyn
        WRN_StaticLocalNoInference = 42111

        '// AVAILABLE                             42112 - 42202
        ' WRN_SxSHigherIndirectRefEmitted4 = 42203    'unused in Roslyn
        ' WRN_ReferencedAssembliesAmbiguous6 = 42204  'unused in Roslyn
        ' WRN_ReferencedAssembliesAmbiguous4 = 42205  'unused in Roslyn
        ' WRN_MaximumNumberOfWarnings = 42206     'unused in Roslyn
        WRN_InvalidAssemblyName = 42207

        '// AVAILABLE                             42209 - 42299
        WRN_XMLDocBadXMLLine = 42300
        WRN_XMLDocMoreThanOneCommentBlock = 42301
        WRN_XMLDocNotFirstOnLine = 42302
        WRN_XMLDocInsideMethod = 42303
        WRN_XMLDocParseError1 = 42304
        WRN_XMLDocDuplicateXMLNode1 = 42305
        WRN_XMLDocIllegalTagOnElement2 = 42306
        WRN_XMLDocBadParamTag2 = 42307
        WRN_XMLDocParamTagWithoutName = 42308
        WRN_XMLDocCrefAttributeNotFound1 = 42309
        WRN_XMLMissingFileOrPathAttribute1 = 42310
        WRN_XMLCannotWriteToXMLDocFile2 = 42311
        WRN_XMLDocWithoutLanguageElement = 42312
        WRN_XMLDocReturnsOnWriteOnlyProperty = 42313
        WRN_XMLDocOnAPartialType = 42314
        WRN_XMLDocReturnsOnADeclareSub = 42315
        WRN_XMLDocStartTagWithNoEndTag = 42316
        WRN_XMLDocBadGenericParamTag2 = 42317
        WRN_XMLDocGenericParamTagWithoutName = 42318
        WRN_XMLDocExceptionTagWithoutCRef = 42319
        WRN_XMLDocInvalidXMLFragment = 42320
        WRN_XMLDocBadFormedXML = 42321
        WRN_InterfaceConversion2 = 42322
        WRN_LiftControlVariableLambda = 42324
        ' 42325 unused, was abandoned, now used in unit test "EnsureLegacyWarningsAreMaintained". Please update test if you are going to use this number.
        WRN_LambdaPassedToRemoveHandler = 42326
        WRN_LiftControlVariableQuery = 42327
        WRN_RelDelegatePassedToRemoveHandler = 42328
        ' WRN_QueryMissingAsClauseinVarDecl = 42329     ' unused in Roslyn.

        ' WRN_LiftUsingVariableInLambda1 = 42330     ' unused in Roslyn.
        ' WRN_LiftUsingVariableInQuery1 = 42331     ' unused in Roslyn.
        WRN_AmbiguousCastConversion2 = 42332 '// substitutes into 42016
        WRN_VarianceDeclarationAmbiguous3 = 42333
        WRN_ArrayInitNoTypeObjectAssumed = 42334
        WRN_TypeInferenceAssumed3 = 42335
        WRN_VarianceConversionFailedOut6 = 42336 '// substitutes into 42016
        WRN_VarianceConversionFailedIn6 = 42337 '// substitutes into 42016
        WRN_VarianceIEnumerableSuggestion3 = 42338 '// substitutes into 42016
        WRN_VarianceConversionFailedTryOut4 = 42339 '// substitutes into 42016
        WRN_VarianceConversionFailedTryIn4 = 42340 '// substitutes into 42016
        WRN_IfNoTypeObjectAssumed = 42341
        WRN_IfTooManyTypesObjectAssumed = 42342
        WRN_ArrayInitTooManyTypesObjectAssumed = 42343
        WRN_LambdaNoTypeObjectAssumed = 42344
        WRN_LambdaTooManyTypesObjectAssumed = 42345
        WRN_MissingAsClauseinVarDecl = 42346
        WRN_MissingAsClauseinFunction = 42347
        WRN_MissingAsClauseinProperty = 42348

        WRN_ObsoleteIdentityDirectCastForValueType = 42349
        WRN_ImplicitConversion2 = 42350 ' // substitutes into 42016

        WRN_MutableStructureInUsing = 42351
        WRN_MutableGenericStructureInUsing = 42352

        WRN_DefAsgNoRetValFuncVal1 = 42353
        WRN_DefAsgNoRetValOpVal1 = 42354
        WRN_DefAsgNoRetValPropVal1 = 42355
        WRN_AsyncLacksAwaits = 42356
        WRN_AsyncSubCouldBeFunction = 42357
        WRN_UnobservedAwaitableExpression = 42358
        WRN_UnobservedAwaitableDelegate = 42359
        WRN_PrefixAndXmlnsLocalName = 42360
        WRN_UseValueForXmlExpression3 = 42361 ' Replaces ERR_UseValueForXmlExpression3

        'WRN_PDBConstantStringValueTooLong = 42363  we gave up on this warning. See comments in commonCompilation.Emit()
        WRN_ReturnTypeAttributeOnWriteOnlyProperty = 42364

        ' // AVAILABLE 42365

        WRN_InvalidVersionFormat = 42366
        WRN_MainIgnored = 42367
        WRN_EmptyPrefixAndXmlnsLocalName = 42368

        WRN_DefAsgNoRetValWinRtEventVal1 = 42369

        WRN_AssemblyAttributeFromModuleIsOverridden = 42370
        WRN_RefCultureMismatch = 42371
        WRN_ConflictingMachineAssembly = 42372

        WRN_PdbLocalNameTooLong = 42373
        WRN_PdbUsingNameTooLong = 42374

        WRN_XMLDocCrefToTypeParameter = 42375

        WRN_AnalyzerCannotBeCreated = 42376
        WRN_NoAnalyzerInAssembly = 42377
        WRN_UnableToLoadAnalyzer = 42378

        WRN_AttributeIgnoredWhenPublicSigning = 42379
        WRN_Experimental = 42380

        WRN_AttributeNotSupportedInVB = 42381
        ERR_MultipleAnalyzerConfigsInSameDir = 42500

        ' // AVAILABLE                             42600 - 49998
        ERRWRN_NextAvailable = 42600

        '// HIDDENS AND INFOS BEGIN HERE
        HDN_UnusedImportClause = 50000
        HDN_UnusedImportStatement = 50001
        INF_UnableToLoadSomeTypesInAnalyzer = 50002

        ' // AVAILABLE                             50003 - 54999   

        ' Adding diagnostic arguments from resx file
        IDS_ProjectSettingsLocationName = 56000
        IDS_FunctionReturnType = 56001
        IDS_TheSystemCannotFindThePathSpecified = 56002
        ' available: 56003
        IDS_MSG_ADDMODULE = 56004
        IDS_MSG_ADDLINKREFERENCE = 56005
        IDS_MSG_ADDREFERENCE = 56006
        IDS_LogoLine1 = 56007
        IDS_LogoLine2 = 56008
        IDS_VBCHelp = 56009
        IDS_LangVersions = 56010
        IDS_ToolName = 56011

        ' Feature codes
        FEATURE_AutoProperties
        FEATURE_LineContinuation
        FEATURE_StatementLambdas
        FEATURE_CoContraVariance
        FEATURE_CollectionInitializers
        FEATURE_SubLambdas
        FEATURE_ArrayLiterals
        FEATURE_AsyncExpressions
        FEATURE_Iterators
        FEATURE_GlobalNamespace
        FEATURE_NullPropagatingOperator
        FEATURE_NameOfExpressions
        FEATURE_ReadonlyAutoProperties
        FEATURE_RegionsEverywhere
        FEATURE_MultilineStringLiterals
        FEATURE_CObjInAttributeArguments
        FEATURE_LineContinuationComments
        FEATURE_TypeOfIsNot
        FEATURE_YearFirstDateLiterals
        FEATURE_WarningDirectives
        FEATURE_PartialModules
        FEATURE_PartialInterfaces
        FEATURE_ImplementingReadonlyOrWriteonlyPropertyWithReadwrite
        FEATURE_DigitSeparators
        FEATURE_BinaryLiterals
        FEATURE_Tuples
        FEATURE_LeadingDigitSeparator
        FEATURE_PrivateProtected
        FEATURE_InterpolatedStrings
        FEATURE_UnconstrainedTypeParameterInConditional
        FEATURE_CommentsAfterLineContinuation
    End Enum
End Namespace
