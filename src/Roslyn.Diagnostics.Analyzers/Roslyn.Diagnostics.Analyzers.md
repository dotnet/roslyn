### RS0001: Use SpecializedCollections.EmptyEnumerable() ###

Category: Performance

Severity: Warning

### RS0002: Use SpecializedCollections.SingletonEnumerable() ###

Category: Performance

Severity: Warning

### RS0004: Invoke the correct property to ensure correct use site diagnostics. ###

Category: Usage

Severity: Error

### RS0005: Do not use generic CodeAction.Create to create CodeAction ###

Category: Performance

Severity: Warning

### RS0013: Do not invoke Diagnostic.Descriptor ###

Accessing the Descriptor property of Diagnostic in compiler layer leads to unnecessary string allocations for fields of the descriptor that are not utilized in command line compilation. Hence, you should avoid accessing the Descriptor of the compiler diagnostics here. Instead you should directly access these properties off the Diagnostic type.

Category: Performance

Severity: Warning

### RS0019: SymbolDeclaredEvent must be generated for source symbols ###

Compilation event queue is required to generate symbol declared events for all declared source symbols. Hence, every source symbol type or one of its base types must generate a symbol declared event.

Category: Reliability

Severity: Error

### RS0022: Constructor make noninheritable base class inheritable ###

Category: ApiDesign

Severity: Warning