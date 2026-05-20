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

### CodeGenThrowTests (CodeGen/CodeGenThrowTests.cs)
- TestThrowNewExpression, TestThrowLocalExpression, TestThrowNull: ported — changed DesktopOnly to Fact

### CodeGenUsingStatementTests (CodeGen/CodeGenUsingStatementTests.cs)
- ObjectCreateInsideUsing: ported — changed DesktopOnly to Fact

### CodeGenNullCoalescingAssignmentTests (CodeGen/CodeGenNullCoalescingAssignmentTests.cs)
- IndexerLvalue: ported — changed DesktopOnly to Fact

### CodeGenIterators (CodeGen/CodeGenIterators.cs)
- TestIteratorWithNullableAsCollectionVariable_Null: ported — changed DesktopOnly to Fact

### CodeGenConstructorInitTests (CodeGen/CodeGenConstructorInitTests.cs)
- StaticNullInitializerHasNoEffectOnTypeIL: skipped — fails on .NET Core (different metadata)

### SwitchTests (CodeGen/SwitchTests.cs)
- ShareLikeKindedTemps_01: skipped — WindowsOnly, NativePdbRequiresDesktop

### CodeGenUsingDeclarationTests (CodeGen/CodeGenUsingDeclarationTests.cs)
- UsingVariableVarEmitTest: skipped — WindowsOnly, NativePdbRequiresDesktop

### CodeGenStructsAndEnum (CodeGen/CodeGenStructsAndEnum.cs)
- InplaceCtor005: skipped — WindowsDesktopOnly, RestrictedTypesNeedDesktop

### DestructorTests (CodeGen/DestructorTests.cs)
- ClassDestructor: ported — DesktopOnly→Fact, passes on net10.0
- ExpressionBodiedClassDestructor: ported — DesktopOnly→Fact, passes on net10.0
- ExpressionBodiedSubClassDestructor: ported — DesktopOnly→Fact, passes on net10.0
- SubclassDestructor: ported — DesktopOnly→Fact, passes on net10.0
- DestructorOverridesNonDestructor: skipped — WindowsDesktopOnly
- DestructorBody: skipped — WindowsDesktopOnly
- PartialClassDestructor: skipped — WindowsDesktopOnly

### CodeGenShortCircuitOperatorTests (CodeGen/CodeGenShortCircuitOperatorTests.cs)
- TestOr: ported — DesktopOnly→Fact, passes on net10.0
- TestAnd: ported — DesktopOnly→Fact, passes on net10.0

### CodeGenScriptTests (CodeGen/CodeGenScriptTests.cs)
- ScriptEntryPoint: ported — DesktopOnly→Fact, passes on net10.0
- SubmissionEntryPoint: ported — DesktopOnly→Fact, passes on net10.0

### CodeGenDynamicTests (CodeGen/CodeGenDynamicTests.cs)
- ByRefDynamic: ported — DesktopOnly→Fact, passes on net10.0

### CodeGenCapturing (CodeGen/CodeGenCapturing.cs)
- GenerateAllTest: ported — WindowsOnly→Fact, passes on net10.0
- ExpressionGeneratorTest01: ported — WindowsOnly→Fact, passes on net10.0
- AllCaptureTests: ported — removed WindowsOnly from multi-condition, passes on net10.0

### CodeGenOverridingAndHiding (CodeGen/CodeGenOverridingAndHiding.cs)
- All methods are ClrOnly (Mono skip) — already run on .NET Core, no changes needed

### CodeGenInParametersTests (CodeGen/CodeGenInParametersTests.cs)
- All methods are ClrOnly (Mono skip) — already run on .NET Core, no changes needed

### CodeGenTryFinally (CodeGen/CodeGenTryFinally.cs)
- All 7 methods: skipped — WindowsDesktopOnly, ThreadAbort not supported on .NET Core

### BreakingChanges (BreakingChanges.cs)
- All 3 methods: skipped — WindowsDesktopOnly, fails on .NET Core

### CodeGenLocalFunctionTests (CodeGen/CodeGenLocalFunctionTests.cs)
- All 2 methods: skipped — attempted port, tests fail on .NET Core

### CodeGenDeconstructTests (CodeGen/CodeGenDeconstructTests.cs)
- Constraints_01: skipped — attempted port, fails on .NET Core
- 2 other methods: skipped — WindowsDesktopOnly

### CodeGenExprLambdaTests (CodeGen/CodeGenExprLambdaTests.cs)
- AnonymousCreation: ported — WindowsDesktopOnly(#30160)→Fact, passes on net10.0
- AnonTypes2: ported — WindowsDesktopOnly(#30160)→Fact, passes on net10.0
- BinaryAddOperandTypesDelegate: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- CoalesceWithImplicitUDC: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- ArrayIndexTypeLong: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- UnsafeParamTypeInDelegate: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- ConditionalWithOperandTypesObjectArrAndStringArr: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- CheckedEnumConversion: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- EnumConversions001: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- EnumConversions002: skipped — WindowsDesktopOnly(#30160), fails on .NET Core
- IndexerIsIndexedProperty: skipped — WindowsOnly/TestExecutionNeedsDesktopTypes

### CodeGenTests (CodeGen/CodeGenTests.cs)
- TestBug15818: ported — DesktopOnly→Fact, passes on net10.0
- InitFromBlob: ported — DesktopOnly→Fact, passes on net10.0
- Bug17352_VarArgCtor: skipped — DesktopOnly, __arglist fails on .NET Core
- VarargBridgeSource: skipped — DesktopOnly, __arglist fails on .NET Core
- VarargBridgeMeta: skipped — DesktopOnly, __arglist fails on .NET Core
- DecimalBinaryOp_03: skipped — DesktopOnly, runtime issue #1611
- WindowsDesktopOnly methods (3): skipped — TestExecutionNeedsWindowsTypes

### CodeGenOperators (CodeGen/CodeGenOperators.cs)
- TestFloatNegativeZero: ported — DesktopOnly→Fact, passes on net10.0
- TestDoubleNegativeZero: ported — DesktopOnly→Fact, passes on net10.0
- TestDecimalNegativeZero: ported — DesktopOnly→Fact, passes on net10.0

### UnsafeTests (CodeGen/UnsafeTests.cs)
- PointerArrayConversion: ported — DesktopOnly→Fact, passes on net10.0
- PointerArrayEnumerableConversion: ported — DesktopOnly→Fact, passes on net10.0
- PointerArrayConversionRuntimeError: skipped — DesktopOnly, runtime error message differs on .NET Core
- PointerArrayEnumerableConversionRuntimeError: skipped — DesktopOnly, runtime error message differs
- PointerArrayForeachEnumerable: skipped — DesktopOnly, fails on .NET Core

### CodeGenExplicitImplementationTests (CodeGen/CodeGenExplicitImplementationTests.cs)
- TestExplicitImplSignatureMismatches_ParamsAndOptionals: ported — DesktopOnly→Fact, passes on net10.0
- Other methods: ClrOnly (already run on .NET Core)

### PatternTests (CodeGen/PatternTests.cs)
- SwitchExpressionSequencePoints: ported — WindowsOnly→Fact, passes on net10.0
- RestrictedTypesNeedDesktop method: skipped — WindowsDesktopOnly/RestrictedTypesNeedDesktop

### OptionalArgumentsTests (Emit/OptionalArgumentsTests.cs)
- TestDuplicateConstantAttributesMetadata: ported — DesktopOnly→Fact, passes on net10.0

### ResourceTests (Emit/ResourceTests.cs)
- All methods inside #if NET472 block or WindowsDesktopOnly/WindowsOnly — cannot port

### CodeGenImplicitImplementationTests (CodeGen/CodeGenImplicitImplementationTests.cs)
- All methods are ClrOnly (Mono skip) — already run on .NET Core, no changes needed

### CodeGenCheckedTests (CodeGen/CodeGenCheckedTests.cs)
- All 4 methods: skipped — WindowsDesktopOnly(#30160), fails on .NET Core

### DynamicAnalysisResourceTests (Emit/DynamicAnalysis/DynamicAnalysisResourceTests.cs)
- All 3 methods: skipped — WindowsOnly/TestExecutionHasNewLineDependency, CRLF-dependent hashes

### NoPiaEmbedTypes (Emit/NoPiaEmbedTypes.cs)
- All methods are ClrOnly — already run on .NET Core, no changes needed

---

## Project: `src/Compilers/CSharp/Test/CommandLine/Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests.csproj`


### CommandLineTests (CommandLineTests.cs)
- CompilationWithWarnAsError_01: ported — WindowsOnly→Fact, passes on net10.0
- CompilationWithWarnAsError_02: ported — WindowsOnly→Fact, passes on net10.0
- PdbPathNotEmittedWithoutPdb: ported — WindowsOnly→Fact, passes on net10.0
- 28 WindowsOnly methods: skipped — Windows path handling (hardcoded C:\, backslash normalization, cmd.exe usage)
- 9 WindowsDesktopOnly(#30289) methods: skipped — Windows paths in test data
- 13 WindowsDesktopOnly(#30321) methods: skipped — uses ProcessUtilities.RunAndGetOutput with cmd.exe or s_CSharpCompilerExecutable
- 2 WindowsDesktopOnly(#30328) methods: skipped — codepage/process execution
- 2 WindowsDesktopOnly(TestExecutionNeedsWindowsTypes): skipped — genuine restriction
- 3 WindowsDesktopOnly(NativePdbRequiresDesktop): skipped — genuine restriction
- 1 DesktopClrOnly (SxS loading): skipped — genuine restriction
- 4 WindowsDesktopOnly(#30321) with IsEnglishLocal: skipped — locale + process execution
- WindowsOnly+IsEnglishLocal(Bug15538): skipped
- WindowsOnly+WorkItem(FileShareDeleteCompatibility_ReadOnlyFiles): skipped
- WindowsTheory(TestDuplicateAdditionalFiles_Windows): skipped — Windows path dedup logic

### SarifV2ErrorLoggerTests (SarifV2ErrorLoggerTests.cs)
- AnalyzerDisabledWithCommandLineOptions: ported — WindowsOnly→Fact, passes on net10.0
- 4 WindowsOnly(#30289) methods: skipped — SARIF output contains Windows-style URIs
- 7 WindowsOnly methods: skipped — SARIF URI path formatting differs on Linux
- 1 WindowsTheory(#30289): skipped

### SarifV1ErrorLoggerTests (SarifV1ErrorLoggerTests.cs)
- All 9 methods: skipped — SARIF URI path formatting differs on Linux

### CommandLineDiagnosticFormatterTests (CommandLineDiagnosticFormatterTests.cs)
- All 5 methods: skipped — tests Windows path formatting (C:\, X:\rootdir)

### TouchedFileLoggingTests (TouchedFileLoggingTests.cs)
- All 4 methods: skipped — path normalization fails on Linux

### CommandLineTests - Additional ports (path refactoring)
- NullBaseDirectoryNotAddedToKeyFileSearchPaths: ported — refactored c:/test.cs to platform-conditional path
- NullBaseDirectoryWithAdditionalFiles: ported — same platform-conditional path refactoring
- NullBaseDirectoryWithAdditionalFiles_Wildcard: ported — same approach
- AppConfigParse: ported — refactored baseDirectory to platform-conditional, used Path.Combine for assertions
- AppConfigBasicFail: ported — used Path.Combine for non-existent config path
- ParseDocAndOut: ported — used platform-conditional baseDirectory and Path.DirectorySeparatorChar for subpaths
- ParseErrorLogAndOut: ported — same approach as ParseDocAndOut
- SdkPathAndLibEnvVariable: ported — no path changes needed, only used relative paths
- CompilationWithNonExistingOutPath: ported — used Path.DirectorySeparatorChar and Path.Combine for assertions
- ResponseFilesWithNoconfig_02: ported — removed \r\n from Assert.Contains (line ending independence)
- ResponseFilesWithNoconfig_04: ported — same \r\n removal
- UnableWriteOutput_OutputFileLocked: skipped — file locking (FileShare.None) behaves differently on Linux
- WRN_InvalidSearchPathDir: skipped — '::' is a valid (non-existent) path on Linux, invalid on Windows
- PathMapPdbParser: skipped — /pdb:a\data.pdb uses backslash path that doesn't resolve on Linux

## Project: Syntax (4 methods)

### MemberDeclarationParsingTests.cs
- ✅ ParseOverflow — `WindowsOnly` → `Fact` (stack depth test, works on Core)
- ✅ ParseOverflow2 — `WindowsOnly` → `Fact` (stack depth test, works on Core)

### SyntaxNormalizerTests.cs
- ✅ TestNormalizeSwitchExpressionRawStringsMultiline — `WindowsOnly` → `Fact`
- ✅ TestNormalizeSwitchExpressionRawStringsMultilineUtf8_01 — `WindowsOnly` → `Fact`

## Project: Emit3 (5 ported, 38 skipped)

### OutVarTests.cs
- ✅ Scope_Query_01 — `DesktopOnly` → `Fact`
- ⏭️ Query_01 — failed (issue 28026, different diagnostics on Core)
- ⏭️ 4 RestrictedTypes tests — skipped (RestrictedTypesNeedDesktop)

### AttributeTests_Assembly.cs
- ✅ Bug16465 — `DesktopOnly` → `Fact`

### AttributeTests_WellKnownAttributes.cs
- ✅ TestPseudoAttributes1 — `DesktopOnly` → `Fact`

### AttributeTests_CallerInfoAttributes.cs
- ✅ TestCallerMemberName_ConstructorDestructor — `DesktopOnly` → `Fact`
- ⏭️ 1 test — skipped (TestExecutionHasNewLineDependency)

### AttributeTests.cs
- ✅ TestWellKnownAttributeOnProperty_DynamicAttribute — `DesktopOnly` → `Fact`
- ⏭️ TestUnicodeAttributeArgumentsStrings — failed (issue 41280)
- ⏭️ 1 test — skipped (TestExecutionNeedsDesktopTypes)

### CollectionExpressionTests.cs
- ⏭️ CollectionBuilder_02B — failed (different diagnostics on Core)
- ⏭️ RestrictedTypes — failed (restricted types not available on Core)

### PatternMatchingTests.cs
- ⏭️ PatternInFieldInitializer — failed (issue 28026)
- ⏭️ Query_01 — failed (issue 28026)

### InternalsVisibleToAndStrongNameTests.cs
- ⏭️ All 23 — genuinely Windows-only (signing, key containers, paths)

### Other skipped
- ⏭️ RefReadonlyParameterTests — RestrictedTypes
- ⏭️ AttributeTests_Security — TestExecutionNeedsDesktopTypes

## Project: Semantic (8 ported, 29 skipped)

### LocalFunctionTests.cs
- ✅ LocalFunctionResetsLockScopeFlag — `DesktopOnly` → `Fact`
- ✅ LocalFunctionResetsTryCatchFinallyScopeFlags — `DesktopOnly` → `Fact`
- ✅ LocalFunctionDoesNotOverwriteInnerLockScopeFlag — `DesktopOnly` → `Fact`
- ✅ LocalFunctionDoesNotOverwriteInnerTryCatchFinallyScopeFlags — `DesktopOnly` → `Fact`
- ✅ RethrowingExceptionsInCatchInsideLocalFuncIsAllowed — `DesktopOnly` → `Fact`
- ✅ RethrowingExceptionsInLocalFuncInsideCatchIsNotAllowed — `DesktopOnly` → `Fact`
- ⏭️ CanAccessScriptGlobalsFromInsideMethod — failed (issue 28001 scripting)
- ⏭️ CanAccessScriptGlobalsFromInsideLambda — failed (issue 28001 scripting)

### OperatorTests.cs
- ✅ DynamicAmbiguousOrConversion — `DesktopOnly` → `Fact`

### QueryTests.cs
- ✅ StaticTypeInFromClause — `DesktopOnly` → `Fact`

### LambdaTests.cs
- ⏭️ All 3 failed (missing COM types like IDispatchConstant on Core)

### ScriptSemanticsTests.cs
- ⏭️ All 3 failed (issue 28001 scripting)

### Other skipped
- ⏭️ ArglistTests (9) — RestrictedTypesNeedDesktop
- ⏭️ NameLengthTests (2) — NativePdbRequiresDesktop
- ⏭️ NamedAndOptionalTests (2) — failed (COM types)
- ⏭️ RefFieldTests (2) — NoPiaNeedsDesktop
- ⏭️ SemanticErrorTests (2) — NativePdb + issue 18610
- ⏭️ NonTrailingNamedArgumentsTests (1) — RestrictedTypes
- ⏭️ OverloadResolutionTests (1) — RestrictedTypes
- ⏭️ ImplicitObjectCreationTests (1) — RestrictedTypes
- ⏭️ DelegateTypeTests (1) — failed (missing types)

## Project: Symbol (37 ported, 64 skipped)

### CustomModifiersTests.cs
- ✅ 26 pure `DesktopOnly` → `Fact`
- ✅ 4 `ClrOnly+DesktopOnly` → `ConditionalFact(typeof(ClrOnly))`

### IndexedPropertyTests.cs
- ✅ IndexedPropertyDynamicInvocation — `ClrOnly+DesktopOnly` → `ClrOnly`
- ✅ IndexedPropertyDynamicColorColorInvocation — `DesktopOnly+ClrOnly` → `ClrOnly`

### OverriddenOrHiddenMembersTests.cs
- ✅ MethodConstructedFromOverrideWithCustomModifiers — `DesktopOnly+ClrOnly` → `ClrOnly`

### GenericConstraintTests.cs
- ✅ SpellingOfGenericClassNameIsPreserved5 — `ClrOnly+DesktopOnly` → `ClrOnly`

### DynamicTransformsTests.cs
- ✅ TestDynamicTransformsBadMetadata — `DesktopOnly+ClrOnly` → `ClrOnly`

### PropertyTests.cs
- ✅ InteropDynamification — `DesktopOnly` → `Fact`

### CustomModifierCopyTests.cs
- ✅ Repro819774 — `ClrOnly+DesktopOnly` → `ClrOnly`

### Skipped files
- ⏭️ Retargeting/NoPia.cs (18) — all NoPia
- ⏭️ CompilationAPITests.cs (17) — all scripting/submission failures
- ⏭️ DocumentationCommentCompilerTests.cs (9) — all failed on Core
- ⏭️ SymbolErrorTests.cs (7) — netmodules/NoPIA
- ⏭️ ModuleInitializersTests.cs (2) — NetModulesNeedDesktop
- ⏭️ TypeForwarders.cs (2) — netmodule failures
- ⏭️ NoPiaInstantiationOfGenericClassAndStruct.cs (2) — NoPia
- ⏭️ ReferenceManagerTests.cs (2) — both failed
- ⏭️ ConversionTests.cs (1) — expression tree failure
- ⏭️ NoPia.cs (1) — NoPia
- ⏭️ TypeResolutionTests.cs (1) — reflection type failure
- ⏭️ SemanticModelGetSemanticInfoTests.cs (1) — scripting failure

## Projects: WinRT, IOperation (0 ported, 3 skipped)

- ⏭️ WinRT/WinMdMetadataTests (1) — NeedsDesktopTypes
- ⏭️ WinRT/PdbTests (1) — NativePdbRequiresDesktop
- ⏭️ IOperation/IParameterReferenceExpression (1) — RestrictedTypesNeedDesktop
