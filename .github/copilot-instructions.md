# .NET Roslyn Compiler Platform

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

The .NET Roslyn repository contains the open-source implementation of both the C# and Visual Basic compilers with an API surface for building code analysis tools. This is a complex, enterprise-scale codebase that requires specific build tools and internal Microsoft dependencies.

## Working Effectively

### Prerequisites and Initial Setup
- **CRITICAL**: Requires .NET 10.0 SDK (preview version `10.0.100-preview.7.25380.108` as specified in `global.json`)
- **CRITICAL**: Requires access to internal Microsoft Azure DevOps NuGet feeds. The build **WILL NOT WORK** in external environments without access to:
  - `https://pkgs.dev.azure.com/dnceng/public/_packaging/*/nuget/v3/index.json`
  - Multiple other internal Azure DevOps package sources
- Git and curl must be available
- Works on Windows, Linux, and macOS

### Build Commands and Timing (NEVER CANCEL THESE)
1. **First-time setup**: 
   ```bash
   ./build.sh --restore
   ```
   - **NEVER CANCEL**: Takes 10-30 minutes depending on network. Set timeout to 60+ minutes.
   - Downloads .NET 10.0 SDK to `.dotnet` directory if not found
   - Restores NuGet packages from internal Microsoft feeds
   - **WARNING**: Will fail in external environments due to authentication requirements

2. **Build the compiler**:
   ```bash
   ./build.sh --build
   ```
   - **NEVER CANCEL**: Build takes 40+ minutes based on CI configuration. Set timeout to 60+ minutes.
   - Unix builds: 40-minute CI timeout, Windows builds: 40-minute CI timeout
   - Subsequent incremental builds are faster (5-15 minutes)
   - Builds `Compilers.slnf` solution by default

3. **Full verification build**:
   ```bash
   ./verify.sh
   ```
   - **NEVER CANCEL**: Runs build + test + analyzers + pack with warning-as-error
   - Total time: 60-120 minutes. Set timeout to 180+ minutes.
   - Equivalent to: `./build.sh --build --restore --rebuild --pack --test --runAnalyzers --warnAsError`

### Testing (NEVER CANCEL TEST RUNS)
- **Unit tests**:
  ```bash
  ./test.sh
  # or
  ./build.sh --test
  ```
  - **NEVER CANCEL**: CI timeout is 90-120 minutes depending on platform. Set timeout to 150+ minutes.
  - Test timeout per assembly: 25 minutes for local runs, 15 minutes for Helix (cloud) runs
  - Runs CoreClr unit tests by default

- **Desktop tests** (Windows only):
  ```bash
  ./build.sh --testDesktop
  ```
  - **NEVER CANCEL**: Set timeout to 150+ minutes.

- **Compiler-only tests**:
  ```bash
  ./build.sh --testCompilerOnly
  ```
  - Faster subset focusing on compiler components (30-60 minutes)
  - **NEVER CANCEL**: Set timeout to 90+ minutes.

- **Integration tests** require Visual Studio and specific tooling
  - **NEVER CANCEL**: Set timeout to 150+ minutes.

- **Cross-platform test timing**:
  - Unix tests: 50-90 minute CI timeout depending on job type
  - Windows tests: 120 minute CI timeout
  - Single machine tests: 50-120 minutes depending on platform

## Validation Scenarios

**ALWAYS manually test these scenarios after making changes:**

### Compiler Changes
1. **Build validation**: Ensure your changes build successfully with no warnings-as-errors
2. **Basic compilation test**: 
   ```bash
   # After successful build, test the compiler can compile simple code
   echo 'Console.WriteLine("Hello");' > test.cs
   ./artifacts/bin/csc/Debug/net9.0/csc.exe test.cs
   ./test.exe  # Should output "Hello"
   ```
3. **Language feature validation**: Test new language features work as expected
4. **Bootstrap test**: Build with your changes using `./build.sh --bootstrap`

### IDE Changes
1. **Deploy to experimental hive**: `./build.sh --deployExtensions --launch`
2. **Launch VS experimental**: `devenv /rootSuffix RoslynDev`
3. **Test basic scenarios**: 
   - Create new C# console application
   - IntelliSense functionality works
   - Error squiggles appear correctly
   - Go to definition works

### General Changes
1. **Build validation**: Always run `./build.sh --runAnalyzers` before committing
2. **Used assemblies validation**: Run `./build.sh --testUsedAssemblies` for dependency verification
3. **Code format validation**: Run `dotnet format` to ensure consistent styling
4. **Documentation updates**: Update relevant docs in `docs/` directory if applicable

## Key Repository Structure

**Repository Scale**: 17,430+ source files (.cs/.vb), enterprise-scale codebase

### Primary Solutions
- `Roslyn.sln`: Full solution with all projects (IDE, compiler, tools) - ~228KB file
- `Compilers.slnf`: Compiler-focused solution for cross-platform development - ~8KB file  
- `Ide.slnf`: IDE-focused solution - ~23KB file

### Important Directories
- `src/Compilers/`: C# and VB compiler implementations
- `src/Workspaces/`: Roslyn workspace APIs and MSBuild integration
- `src/Features/`: IDE features and refactoring tools
- `src/EditorFeatures/`: Editor integration components  
- `src/VisualStudio/`: Visual Studio integration and VSIX packages
- `src/Tools/`: Build tools and utilities (BuildBoss, RunTests, etc.)
- `docs/`: Documentation and contributing guides
- `eng/`: Build infrastructure and Arcade SDK files

### Key Build Files
- `global.json`: Specifies required .NET SDK version (10.0.100-preview.7.25380.108)
- `NuGet.config`: Internal Microsoft NuGet feed configuration (16 different feeds)
- `Directory.Build.props`: MSBuild properties for entire repo
- `build.sh` / `Build.cmd`: Primary build scripts  
- `test.sh` / `Test.cmd`: Test execution scripts
- `verify.sh` / `Verify.cmd`: Full verification scripts

## Common Development Workflows

### Making Compiler Changes
1. Build: `./build.sh --restore --build`
2. Test: `./build.sh --testCompilerOnly` 
3. Create bootstrap compiler if needed: `./build.sh --bootstrap`
4. Validate with used assemblies: `./build.sh --testUsedAssemblies`

### IDE Feature Development  
1. Build: `./build.sh --restore --build --solution Ide.slnf`
2. Deploy to VS experimental hive: `./build.sh --deployExtensions --launch`
3. Test in Visual Studio: `devenv /rootSuffix RoslynDev`

### Working with Language Features
- New language features require approval through the language design process
- See `docs/contributing/Developing a Language Feature.md`
- Always implement in both C# and VB unless language-specific

## Cross-Platform Considerations

### Linux/macOS Development
- **Limited support**: "Very much a work in progress" per documentation
- Use `build.sh` scripts (not Windows .cmd files)
- Built compilers available at: `artifacts/bin/csc/Debug/netcoreapp3.1`
- Mono support: `mono artifacts/bin/csc/Debug/net472/csc.exe -noconfig`

### Visual Studio Code Development
1. Install C# extension
2. Use "Run Build Task" command 
3. Use "Run Tests in Context" for debugging
4. Launch language server: run "launch vscode with language server" task

## Known Issues and Limitations

### Build Failures
- **Authentication errors**: Repository requires internal Microsoft Azure DevOps access
  ```
  Unable to load the service index for source https://pkgs.dev.azure.com/dnceng/public/_packaging/*/nuget/v3/index.json
  ```
- **NuGet restore failures**: Cannot restore packages without Microsoft corporate network access
- **Missing dependencies**: Arcade SDK and internal tooling dependencies not publicly available
  ```
  The result "" of evaluating the value "$(ArcadeSdkBuildTasksAssembly)" of the "AssemblyFile" attribute in element <UsingTask> is not valid
  ```
- **SDK version mismatch**: Requires exact .NET 10.0 preview version from global.json

### Common Error Patterns  
- **Timeout errors**: Build processes that appear hung - always wait minimum 60 minutes
- **Feed authentication**: HTTP 405 Method Not Allowed from Azure DevOps feeds
- **Missing MSBuild tasks**: Indicates Arcade SDK packages not restored properly
- **Assembly reference errors**: Usually indicates incomplete package restore

### Workarounds
- **Source-only builds**: Use `./build.sh --sourceBuild` for source-build scenarios
- **Bootstrap builds**: Use `./build.sh --bootstrap` to build with custom compiler
- **Public API fixes**: Run dotnet format analyzers for RS0016 diagnostics

### Testing Limitations  
- **VSI tests**: Require Visual Studio installation and specific tooling
- **Integration tests**: Need full Visual Studio environment
- **Helix tests**: Require Microsoft internal infrastructure

## Performance Tips
- **Incremental builds**: Use `./build.sh --build` after initial restore
- **Parallel builds**: Default configuration uses multiple cores
- **Local caching**: Enable NuGet local cache for faster package restore
- **Build server**: Use `dotnet build-server` commands to manage compiler processes

## Contributing Guidelines
- Follow `.editorconfig` settings
- Run analyzers before submitting: `./build.sh --runAnalyzers`
- Ensure tests pass: `./build.sh --test`
- Check used assemblies: `./build.sh --testUsedAssemblies`
- Review breaking changes policy in `docs/Breaking API Changes.md`
- Use BuildBoss tool for solution/project validation

## Emergency Procedures and Troubleshooting
- **Build hanging**: Wait minimum 60 minutes before considering cancellation (CI uses 40-120 minute timeouts)
- **Test timeouts**: Default timeout is 25 minutes for local runs, 15 minutes for Helix cloud runs
- **Process cleanup**: Use `./build.sh --prepareMachine` for CI-style cleanup
- **Cache issues**: Clear NuGet cache and retry restore operation
- **Compiler bootstrap**: Use `./build.sh --bootstrap` to build with custom compiler version
- **MSBuild logging**: Enable with `/bl:build.binlog` for detailed diagnostics
- **Parallel build issues**: MSBuild uses multiple cores by default, disable with `-m:1` if needed

## Working in Restricted Environments
If you don't have access to Microsoft internal feeds:
1. **Document the limitation**: Note that builds will fail due to authentication
2. **Focus on code review**: Review code changes without building
3. **Use public documentation**: Refer to https://docs.microsoft.com/dotnet/csharp/roslyn-sdk/
4. **Identify workarounds**: Check if specific functionality can be validated through other means
5. **Coordinate with maintainers**: Work with Microsoft team members who have proper access