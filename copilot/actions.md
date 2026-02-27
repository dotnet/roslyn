# Porting Actions Log

## Project: Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests

### SymbolMatcherTests (Emit/EditAndContinue/SymbolMatcherTests.cs)
- VaryingCompilationReferences: ported — changed DesktopOnly to Fact; replaced MscorlibRef with compilation0.References to use correct base references on .NET Core
