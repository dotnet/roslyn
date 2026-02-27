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
