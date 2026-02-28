# Porting Actions Log

## Project: Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests

### CheckSumTest (PDB/CheckSumTest.cs)
- NormalizedPath_Tree: skipped — WindowsOnly, Windows backslash path normalization
- NoResolver: skipped — WindowsOnly, Windows backslash path normalization
- NormalizedPath_LineDirective: skipped — WindowsOnly, Windows backslash path normalization
- NormalizedPath_ChecksumDirective: skipped — WindowsOnly, Windows backslash path normalization
- NormalizedPath_NoBaseDirectory: skipped — WindowsOnly, Windows backslash path normalization

### PDBWinMdExpTests (PDB/PDBWinMdExpTests.cs)
- All 5 methods: skipped — WindowsOnly, WinMD output + native PDB genuinely needed

### PDBSourceLinkTests (PDB/PDBSourceLinkTests.cs)
- SourceLink_Errors_NotSupportedByPdbWriter: skipped — WindowsOnly, explicitly uses DebugInformationFormat.Pdb + mock SymWriter

### PDBUsingTests (PDB/PDBUsingTests.cs)
- ImportedNoPiaTypes: skipped — WindowsDesktopOnly, NoPIA genuinely needs desktop

### EditAndContinueTests (Emit/EditAndContinue/EditAndContinueTests.cs)
- UniqueSynthesizedNames_DynamicSiteContainer: ported — changed WindowsDesktopOnly to Fact; dynamic types available on .NET Core via TargetFramework.StandardAndCSharp
- MethodSignatureWithNoPIAType: skipped — NoPIA genuinely needs desktop
- LocalSignatureWithNoPIAType: skipped — NoPIA genuinely needs desktop
- NoPIAReferences: skipped — NoPIA genuinely needs desktop
- NoPIATypeInNamespace: skipped — NoPIA genuinely needs desktop
- SymWriterErrors: skipped — uses MockSymUnmanagedWriter, native PDB specific
- AnonymousTypes_OtherTypeNames: skipped — uses ILASM which doesn't support portable PDBs
- ChangingCompilationDependencies: ported — changed WindowsOnly to Fact; EnC with EmptyLocalsProvider works on .NET Core
- DependencyVersionWildcards_Compilation: ported — changed WindowsOnly to Fact
- DependencyVersionWildcards_Metadata: ported — changed WindowsOnly to Fact
- DependencyVersionWildcardsCollisions: ported — changed WindowsOnly to Fact
- CurrentCompilationVersionWildcards: ported — changed WindowsOnly to Fact; CreateSymReader works with portable PDB
- CompilationReferences_Less: skipped — explicit stream-based EmitDifference produces BadImageFormatException on .NET Core
- CompilationReferences_More: skipped — same as CompilationReferences_Less
- PortablePdb_DeterministicCompilationWithSJIS: ported — changed DesktopOnly to Theory; added Encoding.RegisterProvider for SJIS on .NET Core
- InvalidCharacterInPdbPath: ported — changed WindowsDesktopOnly to Fact; test uses Embedded PDB format, not native
- SwitchExpression_MethodBody_02: ported — changed WindowsOnly to Fact; test uses default format which works with portable PDB
- SyntaxOffset_OutVarInInitializers_SwitchExpression: ported — changed WindowsOnly to Fact; test uses default format which works with portable PDB
- RelativePathForExternalSource_Sha1_Windows: skipped — test uses Windows-specific paths
- SymWriterErrors: skipped — tests native PDB SymWriter error handling
- SymWriterErrors2: skipped — tests native PDB SymWriter error handling
- SymWriterErrors3: skipped — tests native PDB SymWriter error handling
- SymWriterErrors4: skipped — tests native PDB SymWriter error handling
- NativeWriterLimit_Under: skipped — tests native SymWriter byte limits
- NativeWriterLimit_Over: skipped — tests native SymWriter byte limits
- SymWriterErrors_ErrorReturned: skipped — tests native PDB SymWriter error handling
- SwitchExpression_MethodBody: skipped — VerifyPdb expected XML contains native-PDB-specific elements incompatible with portable PDB

## Project: Microsoft.CodeAnalysis.CSharp.Emit.UnitTests

### PrivateProtected (PrivateProtected.cs) — 22 methods
- RejectIncompatibleModifiers
- AccessibleWhereRequired_01
- AccessibleWhereRequired_02
- NotAccessibleWhereRequired
- NotInStructOrNamespace
- NotInStaticClass
- NestedTypes
- PermittedAccessorProtection
- ForbiddenAccessorProtection_01
- ForbiddenAccessorProtection_02
- AtLeastAsRestrictivePositive_01
- AtLeastAsRestrictiveNegative_01
- DuplicateAccessInBinder
- NotInVersion71
- VerifyPrivateProtectedIL
- VerifyPartialPartsMatch
- VerifyProtectedSemantics
- HidingAbstract
- HidingInaccessible
- UnimplementedInaccessible
- ImplementInaccessible
- VerifyPPExtension

### CompilationEmitTests (Emit/CompilationEmitTests.cs) — 22 methods
- RefAssembly_NoPia
- RefAssembly_NoPia_ReferenceFromMethodBody
- CheckUnsafeAttributes3
- CheckUnsafeAttributes4
- CheckUnsafeAttributes5
- CheckUnsafeAttributes6
- CheckUnsafeAttributes7
- CheckUnsafeAttributes8
- CheckUnsafeAttributes9
- PlatformMismatch_01
- PlatformMismatch_02
- PlatformMismatch_03
- PlatformMismatch_04
- PlatformMismatch_05
- PlatformMismatch_06
- PlatformMismatch_07
- PlatformMismatch_08
- BrokenPDBStream
- MultipleNetmodulesWithPrivateImplementationDetails
- MultipleNetmodulesWithAnonymousTypes

### CodeGenTupleTest (CodeGen/CodeGenTupleTest.cs) — 16 methods
- LongTupleDeclaration
- DefaultAndFriendlyElementNames_01
- DefaultAndFriendlyElementNames_LongTuple
- DefaultAndFriendlyElementNames_02
- DefaultAndFriendlyElementNames_03
- DefaultAndFriendlyElementNames_05
- DefaultAndFriendlyElementNames_08
- DefaultAndFriendlyElementNames_09
- CreationOfTupleSymbols_01
- UnifyUnderlyingWithTuple_05
- RefTupleDynamicDecode001
- RefTupleDynamicDecode004
- Serialization

### CodeGenExprLambdaTests (CodeGen/CodeGenExprLambdaTests.cs) — 11 methods
- AnonymousCreation
- BinaryAddOperandTypesDelegate
- CoalesceWithImplicitUDC
- ArrayIndexTypeLong
- UnsafeParamTypeInDelegate
- AnonTypes2
- ConditionalWithOperandTypesObjectArrAndStringArr
- IndexerIsIndexedProperty
- CheckedEnumConversion
- EnumConversions001
- EnumConversions002

### CodeGenTests (CodeGen/CodeGenTests.cs) — 9 methods
- TestBug15818
- TestAssignIdentity
- TestRefCast
- InitFromBlob
- VarargBridgeSource
- VarargByRef
- VarargBridgeMeta

### CodeGenOperators (CodeGen/CodeGenOperators.cs) — 8 methods
- TestFloatNegativeZero
- TestDoubleNegativeZero
- TestDecimalNegativeZero

### DestructorTests (CodeGen/DestructorTests.cs) — 7 methods
- ClassDestructor
- SubclassDestructor
- DestructorOverridesNonDestructor
- BaseTypeHasNonVirtualFinalize
- GenericBaseTypeHasNonVirtualFinalize

### CodeGenTryFinally (CodeGen/CodeGenTryFinally.cs) — 7 methods
- NestedExceptionHandlersThreadAbort01
- NestedExceptionHandlersThreadAbort02
- NestedExceptionHandlersThreadAbort03
- NestedExceptionHandlersThreadAbort04
- NestedExceptionHandlersThreadAbort05
- NestedExceptionHandlersThreadAbort06
- NestedExceptionHandlersThreadAbort07

### ResourceTests (Emit/ResourceTests.cs) — 5 methods
- DefaultVersionResource
- ResourcesInCoff
- AddManagedResource
- AddResourceToModule
- ResourceWithAttrSettings

### UnsafeTests (CodeGen/UnsafeTests.cs) — 5 methods
- PointerArrayConversion
- PointerArrayConversionRuntimeError
- PointerArrayEnumerableConversion
- PointerArrayEnumerableConversionRuntimeError
- PointerArrayForeachEnumerable

### CodeGenImplicitImplementationTests (CodeGen/CodeGenImplicitImplementationTests.cs) — 5 methods
- TestImplicitImplementationInBaseGenericType
- TestImplicitImplementationInBaseGenericType2
- TestImplicitImplementationInBaseGenericType3
- TestImplicitImplementationInBaseGenericType4
- TestImplicitImplementationInBaseGenericType5

### CodeGenCheckedTests (CodeGen/CodeGenCheckedTests.cs) — 4 methods
- CheckedConversionsInExpressionTrees_Implicit
- CheckedConversionsInExpressionTrees_Explicit
- CheckedConversionsInExpressionTrees_ExplicitTuple

### DynamicAnalysisResourceTests (Emit/DynamicAnalysis/DynamicAnalysisResourceTests.cs) — 3 methods
- TestSpansPresentInResource
- ResourceStatementKinds
- TestMethodSpansWithAttributes

### CodeGenThrowTests (CodeGen/CodeGenThrowTests.cs) — 3 methods
- TestThrowNewExpression
- TestThrowLocalExpression
- TestThrowNull

### CodeGenExplicitImplementationTests (CodeGen/CodeGenExplicitImplementationTests.cs) — 3 methods
- TestExplicitImplSignatureMismatches_ParamsAndOptionals
- TestExplicitImplementationInBaseGenericType
- TestExplicitImplementationInBaseGenericType2

### CodeGenDeconstructTests (CodeGen/CodeGenDeconstructTests.cs) — 3 methods
- Constraints_01

### CodeGenCapturing (CodeGen/CodeGenCapturing.cs) — 3 methods
- GenerateAllTest
- ExpressionGeneratorTest01
- AllCaptureTests

### BreakingChanges (BreakingChanges.cs) — 3 methods
- ExpressionTreeNoCovertForIdentityConversion
- ExpressionTreeWithNullableUDCandOperator

### PatternTests (CodeGen/PatternTests.cs) — 2 methods
- SwitchExpressionSequencePoints
- TargetTypedSwitch_Arglist

### CodeGenShortCircuitOperatorTests (CodeGen/CodeGenShortCircuitOperatorTests.cs) — 2 methods

### CodeGenScriptTests (CodeGen/CodeGenScriptTests.cs) — 2 methods
- ScriptEntryPoint
- SubmissionEntryPoint

### CodeGenLocalFunctionTests (CodeGen/CodeGenLocalFunctionTests.cs) — 2 methods
- LocalFunctionAttribute_TypeIL

### CodeGenInParametersTests (CodeGen/CodeGenInParametersTests.cs) — 2 methods
- InParamGenericReadonly
- InParamGenericReadonlyROstruct

### OptionalArgumentsTests (Emit/OptionalArgumentsTests.cs) — 1 method
- TestDuplicateConstantAttributesMetadata

### NoPiaEmbedTypes (Emit/NoPiaEmbedTypes.cs) — 1 method

### SwitchTests (CodeGen/SwitchTests.cs) — 1 method
- ShareLikeKindedTemps_01

### CodeGenUsingStatementTests (CodeGen/CodeGenUsingStatementTests.cs) — 1 method
- ObjectCreateInsideUsing

### CodeGenUsingDeclarationTests (CodeGen/CodeGenUsingDeclarationTests.cs) — 1 method
- UsingVariableVarEmitTest

### CodeGenStructsAndEnum (CodeGen/CodeGenStructsAndEnum.cs) — 1 method
- InplaceCtor005

### CodeGenOverridingAndHiding (CodeGen/CodeGenOverridingAndHiding.cs) — 1 method
- TestCallMethodsWithLeastCustomModifiers

### CodeGenNullCoalescingAssignmentTests (CodeGen/CodeGenNullCoalescingAssignmentTests.cs) — 1 method
- IndexerLvalue

### CodeGenIterators (CodeGen/CodeGenIterators.cs) — 1 method

### CodeGenDynamicTests (CodeGen/CodeGenDynamicTests.cs) — 1 method
- ByRefDynamic

### CodeGenConstructorInitTests (CodeGen/CodeGenConstructorInitTests.cs) — 1 method
- StaticNullInitializerHasNoEffectOnTypeIL

---

### PrivateProtected (PrivateProtected.cs)
- All 22 methods: ported — changed DesktopOnly to Fact; pure compilation/diagnostic tests with no desktop dependencies

### CompilationEmitTests (Emit/CompilationEmitTests.cs)
- RefAssembly_NoPia: skipped — NoPIA genuinely needs desktop
- RefAssembly_NoPia_ReferenceFromMethodBody: skipped — NoPIA genuinely needs desktop
- RefAssembly_InvariantToResourceChanges: skipped — wrapped in #if NET472, not compilable on .NET Core
- CheckUnsafeAttributes3-9: skipped — hardcodes mscorlib assembly identity in DeclSecurity validation
- PlatformMismatch_01-08, TestCompilationEmitUsesDifferentStreamsForBinaryAndPdb: skipped — uses CreateEmptyCompilation with no .NET Core refs, fails with OperatingSystem.IsWindows() error
- BrokenPDBStream: skipped — native PDB stream error handling differs on .NET Core
- MultipleNetmodulesWithPrivateImplementationDetails: skipped — net modules need desktop
- MultipleNetmodulesWithAnonymousTypes: skipped — net modules need desktop

### CodeGenTupleTests (CodeGen/CodeGenTupleTest.cs)
- LongTupleDeclaration: ported — changed DesktopOnly to Fact
- Serialization: ported — changed DesktopOnly to Fact
- DefaultAndFriendlyElementNames_01, _02, _03, _05, _08, _LongTuple: skipped — tests use MscorlibRef or reference mscorlib identity in symbol validation
- CreationOfTupleSymbols_01: skipped — references mscorlib-specific symbols
- UnifyUnderlyingWithTuple_05: skipped — references mscorlib-specific symbols
- RefTupleDynamicDecode001, RefTupleDynamicDecode004: skipped — reference mscorlib-specific symbols
- DefaultAndFriendlyElementNames_09: skipped — WindowsOnly, newline dependency
