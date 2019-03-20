### RS0016: Add public types and members to the declared API ###

All public types and members should be declared in PublicAPI.txt. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

### RS0017: Remove deleted types and members from the declared API ###

When removing a public type or member the corresponding entry in PublicAPI.txt should also be removed. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

### RS0022: Constructor make noninheritable base class inheritable ###

Constructor makes its noninheritable base class inheritable, thereby exposing its protected members.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

### RS0024: The contents of the public API files are invalid ###

The contents of the public API files are invalid: {0}

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

### RS0025: Do not duplicate symbols in public API files ###

The symbol '{0}' appears more than once in the public API files.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

### RS0026: Do not add multiple public overloads with optional parameters ###

Symbol '{0}' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See '{1}' for details.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

Help: [https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md](https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md)

### RS0027: Public API with optional parameter(s) should have the most parameters amongst its public overloads. ###

Symbol '{0}' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See '{1}' for details.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

Help: [https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md](https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md)

### RS0030: Do not used banned APIs ###

The symbol has been marked as banned in this project, and an alternate should be used instead.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

Help: [https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md)

### RS0031: The list of banned symbols contains a duplicate ###

The list of banned symbols contains a duplicate.

Category: ApiDesign

Severity: Warning

IsEnabledByDefault: True

Help: [https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md)

