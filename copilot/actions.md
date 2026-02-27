# Porting Actions Log

## Project: Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests

### PDBTests (PDB/PDBTests.cs)
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
