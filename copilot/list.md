# Test Porting List

## Project: `src/Compilers/CSharp/Test/Emit2/Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests.csproj`

### Classes

- [x] PDB/CheckSumTest.cs (5 methods)
- [x] PDB/PDBTests.cs (12 methods)
- [x] PDB/PDBWinMdExpTests.cs (5 methods)
- [x] PDB/PDBSourceLinkTests.cs (1 method)
- [x] PDB/PDBUsingTests.cs (1 method)
- [x] PDB/CSharpDeterministicBuildCompilationTests.cs (1 method)
- [x] Emit/EditAndContinue/AssemblyReferencesTests.cs (7 methods)
- [x] Emit/EditAndContinue/EditAndContinueTests.cs (7 methods)
- [x] Emit/EditAndContinue/SymbolMatcherTests.cs (1 method)

## Project: `src/Compilers/CSharp/Test/Emit/Microsoft.CodeAnalysis.CSharp.Emit.UnitTests.csproj`

### Classes

- [x] PrivateProtected.cs (22 methods)
- [x] Emit/CompilationEmitTests.cs (22 methods)
- [x] CodeGen/CodeGenTupleTest.cs (16 methods)
- [x] CodeGen/CodeGenExprLambdaTests.cs (11 methods)
- [x] CodeGen/CodeGenTests.cs (9 methods)
- [x] CodeGen/CodeGenOperators.cs (8 methods)
- [x] CodeGen/DestructorTests.cs (7 methods)
- [x] CodeGen/CodeGenTryFinally.cs (7 methods)
- [x] Emit/ResourceTests.cs (5 methods)
- [x] CodeGen/UnsafeTests.cs (5 methods)
- [x] CodeGen/CodeGenImplicitImplementationTests.cs (5 methods)
- [x] CodeGen/CodeGenCheckedTests.cs (4 methods)
- [x] Emit/DynamicAnalysis/DynamicAnalysisResourceTests.cs (3 methods)
- [x] CodeGen/CodeGenThrowTests.cs (3 methods)
- [x] CodeGen/CodeGenExplicitImplementationTests.cs (3 methods)
- [x] CodeGen/CodeGenDeconstructTests.cs (3 methods)
- [x] CodeGen/CodeGenCapturing.cs (3 methods)
- [x] BreakingChanges.cs (3 methods)
- [x] CodeGen/PatternTests.cs (2 methods)
- [x] CodeGen/CodeGenShortCircuitOperatorTests.cs (2 methods)
- [x] CodeGen/CodeGenScriptTests.cs (2 methods)
- [x] CodeGen/CodeGenLocalFunctionTests.cs (2 methods)
- [x] CodeGen/CodeGenInParametersTests.cs (2 methods)
- [x] Emit/OptionalArgumentsTests.cs (1 method)
- [x] Emit/NoPiaEmbedTypes.cs (1 method)
- [x] CodeGen/SwitchTests.cs (1 method)
- [x] CodeGen/CodeGenUsingStatementTests.cs (1 method)
- [x] CodeGen/CodeGenUsingDeclarationTests.cs (1 method)
- [x] CodeGen/CodeGenStructsAndEnum.cs (1 method)
- [x] CodeGen/CodeGenOverridingAndHiding.cs (1 method)
- [x] CodeGen/CodeGenNullCoalescingAssignmentTests.cs (1 method)
- [x] CodeGen/CodeGenIterators.cs (1 method)
- [x] CodeGen/CodeGenDynamicTests.cs (1 method)
- [x] CodeGen/CodeGenConstructorInitTests.cs (1 method)
## Project: `src/Compilers/CSharp/Test/CommandLine/Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests.csproj`

### Classes

- [x] CommandLineTests.cs (76 methods)
- [x] SarifV2ErrorLoggerTests.cs (13 methods)
- [x] SarifV1ErrorLoggerTests.cs (9 methods)
- [x] CommandLineDiagnosticFormatterTests.cs (5 methods)
- [x] TouchedFileLoggingTests.cs (4 methods)

## Project: `src/Compilers/CSharp/Test/Emit3/Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests.csproj`

### Classes

- [x] Attributes/InternalsVisibleToAndStrongNameTests.cs (23 methods) — all genuinely Windows-only (signing, paths)
- [x] Semantics/OutVarTests.cs (6 methods) — 1 ported (Scope_Query_01), 5 skipped (RestrictedTypes, issue 28026)
- [x] Attributes/AttributeTests.cs (4 methods) — 1 ported (TestWellKnownAttributeOnProperty_DynamicAttribute), 3 skipped
- [x] Semantics/CollectionExpressionTests.cs (2 methods) — both failed (restricted types, issue 28026)
- [x] Semantics/PatternMatchingTests.cs (2 methods) — both failed (issue 28026)
- [x] Attributes/AttributeTests_CallerInfoAttributes.cs (2 methods) — 1 ported (TestCallerMemberName_ConstructorDestructor), 1 skipped (newline)
- [x] RefReadonlyParameterTests.cs (1 method) — skipped (RestrictedTypes)
- [x] Attributes/AttributeTests_Security.cs (1 method) — skipped (NeedsDesktopTypes)
- [x] Attributes/AttributeTests_Assembly.cs (1 method) — ported (Bug16465)
- [x] Attributes/AttributeTests_WellKnownAttributes.cs (1 method) — ported (TestPseudoAttributes1)

## Project: `src/Compilers/CSharp/Test/Semantic/Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.csproj`

### Classes

- [x] Semantics/ArglistTests.cs (9 methods) — all skipped (RestrictedTypesNeedDesktop)
- [x] Semantics/LocalFunctionTests.cs (8 methods) — 6 ported, 2 skipped (issue 28001 scripting)
- [x] Semantics/LambdaTests.cs (3 methods) — all failed (missing types on Core)
- [x] Semantics/ScriptSemanticsTests.cs (3 methods) — all failed (issue 28001 scripting)
- [x] Semantics/NameLengthTests.cs (2 methods) — skipped (NativePdbRequiresDesktop)
- [x] Semantics/NamedAndOptionalTests.cs (2 methods) — both failed (missing COM types)
- [x] Semantics/RefFieldTests.cs (2 methods) — skipped (NoPiaNeedsDesktop)
- [x] Semantics/SemanticErrorTests.cs (2 methods) — 1 NativePdb skipped, 1 failed (issue 18610)
- [x] Semantics/NonTrailingNamedArgumentsTests.cs (1 method) — skipped (RestrictedTypes)
- [x] Semantics/OverloadResolutionTests.cs (1 method) — skipped (RestrictedTypes)
- [x] Semantics/ImplicitObjectCreationTests.cs (1 method) — skipped (RestrictedTypes)
- [x] Semantics/QueryTests.cs (1 method) — ported (StaticTypeInFromClause)
- [x] Semantics/OperatorTests.cs (1 method) — ported (DynamicAmbiguousOrConversion)
- [x] Semantics/DelegateTypeTests.cs (1 method) — failed (missing types on Core)

## Project: `src/Compilers/CSharp/Test/Symbol/Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.csproj`

### Classes

- [x] Symbols/CustomModifiersTests.cs (30 methods) — all 30 ported
- [x] Symbols/Retargeting/NoPia.cs (18 methods) — all skipped (NoPia)
- [x] Compilation/CompilationAPITests.cs (17 methods) — all failed (scripting/submission)
- [x] DocumentationComments/DocumentationCommentCompilerTests.cs (9 methods) — all failed on Core
- [x] Symbols/SymbolErrorTests.cs (7 methods) — all failed (netmodules, NoPIA)
- [x] Symbols/IndexedPropertyTests.cs (2 methods) — 2 ported (ClrOnly)
- [x] Symbols/ModuleInitializers/ModuleInitializersTests.cs (2 methods) — skipped (NetModulesNeedDesktop)
- [x] Symbols/Metadata/PE/TypeForwarders.cs (2 methods) — both failed (netmodules)
- [x] Symbols/Metadata/PE/NoPiaInstantiationOfGenericClassAndStruct.cs (2 methods) — skipped (NoPia)
- [x] Compilation/ReferenceManagerTests.cs (2 methods) — both failed
- [x] Symbols/OverriddenOrHiddenMembersTests.cs (1 method) — ported (ClrOnly)
- [x] Symbols/GenericConstraintTests.cs (1 method) — ported (ClrOnly)
- [x] Symbols/ConversionTests.cs (1 method) — failed (expression trees)
- [x] Symbols/Metadata/PE/DynamicTransformsTests.cs (1 method) — ported (ClrOnly)
- [x] Symbols/Metadata/PE/NoPia.cs (1 method) — skipped (NoPia)
- [x] Symbols/TypeResolutionTests.cs (1 method) — failed (reflection types)
- [x] Symbols/DefaultInterfaceImplementationTests.cs (1 method) — N/A
- [x] Symbols/Source/PropertyTests.cs (1 method) — ported
- [x] Symbols/Source/CustomModifierCopyTests.cs (1 method) — ported (ClrOnly)
- [x] Compilation/SemanticModelGetSemanticInfoTests.cs (1 method) — failed

## Project: `src/Compilers/CSharp/Test/Syntax/Microsoft.CodeAnalysis.CSharp.Syntax.UnitTests.csproj`

### Classes

- [x] Parsing/MemberDeclarationParsingTests.cs (2 methods) — both ported (ParseOverflow, ParseOverflow2)
- [x] Syntax/SyntaxNormalizerTests.cs (2 methods) — both ported (raw string multiline tests)

## Project: `src/Compilers/CSharp/Test/WinRT/Microsoft.CodeAnalysis.CSharp.WinRT.UnitTests.csproj`

### Classes

- [x] Metadata/WinMdMetadataTests.cs (1 method) — skipped (NeedsDesktopTypes)
- [x] PdbTests.cs (1 method) — skipped (NativePdbRequiresDesktop)

## Project: `src/Compilers/CSharp/Test/IOperation/Microsoft.CodeAnalysis.CSharp.IOperation.UnitTests.csproj`

### Classes

- [x] IOperation/IOperationTests_IParameterReferenceExpression.cs (1 method) — skipped (RestrictedTypesNeedDesktop)
