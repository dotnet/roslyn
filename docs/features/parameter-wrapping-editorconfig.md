# Parameter Wrapping EditorConfig Integration

## Summary

This feature exposes existing parameter wrapping functionality to EditorConfig, allowing parameter wrapping styles to be automatically applied during formatting operations (`dotnet format`, format-on-save, etc.) rather than only being available as manual refactorings.

## Motivation

Currently, Roslyn provides comprehensive parameter wrapping through the `CSharpParameterWrapper` refactoring provider, offering multiple wrapping styles (align wrapped, unwrap and indent all, etc.). However, these are only available as manual refactorings accessed through the lightbulb menu.

Teams often want consistent parameter wrapping applied automatically during formatting operations. This feature bridges the gap between manual refactoring capabilities and automatic formatting rules.

## Current State

### Existing Infrastructure
- **`CSharpParameterWrapper`** - Provides manual refactoring for parameter wrapping
- **`AbstractWrappingCodeRefactoringProvider`** - Base infrastructure for wrapping refactorings  
- **`SyntaxWrappingOptions`** - Configuration system for wrapping behaviors
- **`CSharpFormattingOptions2`** - EditorConfig integration for C# formatting

### Current Parameter Wrapping Actions
The existing `CSharpParameterWrapper` offers these refactoring actions:
1. **Align wrapped parameters** - `void Goo(int i,\n         int j)`
2. **Unwrap and indent all** - `void Goo(\n    int i,\n    int j)`  
3. **Wrap first, indent rest** - `void Goo(int i,\n    int j)`
4. **Unwrap all parameters** - `void Goo(\n    int i, int j)`
5. **Unwrap to single line** - `void Goo(int i, int j)`

### Gap
These wrapping styles are not available during automatic formatting operations.

## Proposed Solution

### Phase 1: EditorConfig Option
Add a new EditorConfig option for parameter wrapping:

```editorconfig
# Parameter wrapping style
csharp_parameter_wrapping = do_not_wrap | wrap_long_parameters | wrap_every_parameter
```

**Option Values:**
- `do_not_wrap` (default) - No automatic parameter wrapping
- `wrap_long_parameters` - Wrap when exceeding wrapping column limit
- `wrap_every_parameter` - Wrap each parameter to its own line

### Phase 2: Formatting Rule Integration
Create a formatting rule that applies parameter wrapping based on the EditorConfig setting.

### Phase 3: Additional Options (Future)
After proving the concept, add additional wrapping styles:
- `csharp_parameter_first_placement = same_line | new_line`  
- `csharp_parameter_alignment = align_with_first | indent_uniform`

## Technical Design

### 1. EditorConfig Option Definition

**Location:** `src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Formatting/CSharpFormattingOptions2.cs`

```csharp
public static Option2<ParameterWrappingOptionsInternal> ParameterWrapping { get; } = CreateOption(
    CSharpFormattingOptionGroups.Wrapping, "csharp_parameter_wrapping",
    defaultValue: ParameterWrappingOptionsInternal.DoNotWrap,
    new EditorConfigValueSerializer<ParameterWrappingOptionsInternal>(
        s => ParseEditorConfigParameterWrapping(s),
        GetParameterWrappingOptionEditorConfigString));

internal enum ParameterWrappingOptionsInternal
{
    DoNotWrap = 0,
    WrapLongParameters = 1,
    WrapEveryParameter = 2
}
```

### 2. Formatting Rule Implementation

**Location:** `src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Formatting/Rules/ParameterWrappingFormattingRule.cs`

```csharp
internal sealed class ParameterWrappingFormattingRule : BaseFormattingRule
{
    private readonly CSharpSyntaxFormattingOptions _options;

    public override void AddSuppressOperations(...)
    {
        // Add wrapping logic based on _options.ParameterWrapping
    }
    
    public override void AddIndentBlockOperations(...)
    {
        // Handle parameter indentation
    }
}
```

### 3. Integration Points

**Formatting Pipeline Integration:**
- Add `ParameterWrappingFormattingRule` to `CSharpSyntaxFormatting._rules`
- Ensure proper ordering with existing formatting rules

**Options Integration:**
- Extend `CSharpSyntaxFormattingOptions` to include parameter wrapping options
- Update `CSharpSyntaxWrappingOptions` to consume the new settings

### 4. Reuse Existing Logic

Leverage existing parameter wrapping logic from `CSharpParameterWrapper`:
- Extract shared wrapping algorithms into utility classes
- Avoid duplicating wrapping logic between refactoring and formatting

## Implementation Plan

### Phase 1: Foundation (Milestone 1)
1. **Add EditorConfig option definition** 
   - Define `ParameterWrappingOptionsInternal` enum
   - Add option to `CSharpFormattingOptions2`
   - Add EditorConfig serialization support

2. **Extend formatting options**
   - Update `CSharpSyntaxFormattingOptions` constructor
   - Update `CSharpSyntaxWrappingOptions` to consume parameter wrapping settings

3. **Create basic formatting rule**
   - Implement `ParameterWrappingFormattingRule` with stub logic
   - Integrate into formatting pipeline
   - Add to `CSharpSyntaxFormatting._rules`

### Phase 2: Core Logic (Milestone 2)
1. **Extract shared wrapping utilities**
   - Create `ParameterWrappingUtilities` class
   - Extract algorithms from `CSharpParameterWrapper`
   - Ensure both refactoring and formatting can use shared code

2. **Implement wrapping logic**
   - Add `WrapEveryParameter` support in formatting rule
   - Add `WrapLongParameters` support with column detection
   - Handle parameter alignment and indentation

3. **Testing**
   - Unit tests for formatting rule
   - Integration tests with `dotnet format`
   - EditorConfig option parsing tests

### Phase 3: Polish & Documentation (Milestone 3)
1. **Edge case handling**
   - Handle complex parameter scenarios (attributes, default values, etc.)
   - Ensure compatibility with existing formatting rules
   - Performance optimization

2. **Documentation**
   - EditorConfig documentation for new option
   - Migration guide for teams wanting automatic parameter wrapping

## Testing Strategy

### Unit Tests
- **EditorConfig parsing**: Test option serialization/deserialization
- **Formatting rule behavior**: Test wrapping logic in isolation  
- **Integration**: Test interaction with other formatting rules

### Integration Tests
- **dotnet format**: Verify command-line formatting works
- **VS integration**: Test format-on-save scenarios
- **Cross-platform**: Ensure consistent behavior across environments

### Test Cases
```csharp
// Input
void Method(int parameter1, int parameter2, int reallyLongParameterName, int anotherParameter) { }

// csharp_parameter_wrapping = wrap_every_parameter
void Method(
    int parameter1,
    int parameter2, 
    int reallyLongParameterName,
    int anotherParameter) { }

// csharp_parameter_wrapping = wrap_long_parameters (with csharp_max_line_length = 80)
void Method(int parameter1, int parameter2,
           int reallyLongParameterName, int anotherParameter) { }
```

## Compatibility

### Backward Compatibility
- Default option value maintains current behavior (no automatic wrapping)
- Existing refactoring functionality remains unchanged
- No breaking changes to formatting APIs

### Forward Compatibility  
- Design allows for additional wrapping options in future
- Architecture supports extending to other construct types (arguments, expressions, etc.)

## Open Questions

1. **Rule Ordering**: Where should `ParameterWrappingFormattingRule` be positioned in the formatting pipeline?

2. **Performance**: What's the performance impact of adding wrapping detection to the formatting pipeline?

3. **Interaction**: How should this interact with existing `csharp_preserve_single_line_*` options?

4. **Scope**: Should the first implementation support method parameters only, or also include constructors, delegates, etc.?

## Success Criteria

### Milestone 1 Complete
- [ ] EditorConfig option defined and integrated
- [ ] Basic formatting rule infrastructure in place
- [ ] Options flow from EditorConfig → Formatting pipeline

### Milestone 2 Complete  
- [ ] `wrap_every_parameter` fully functional
- [ ] `wrap_long_parameters` with line length detection
- [ ] Integration tests passing with `dotnet format`

### Milestone 3 Complete
- [ ] Edge cases handled (attributes, default values, complex scenarios)
- [ ] Performance impact measured and acceptable
- [ ] Documentation complete
- [ ] Ready for broader team review

## Future Extensions

After parameter wrapping proves successful, this approach can be extended to:
- **Argument wrapping** (`csharp_argument_wrapping`)
- **Binary expression wrapping** (`csharp_binary_expression_wrapping`)  
- **Chained expression wrapping** (`csharp_chained_expression_wrapping`)

This creates a path toward comprehensive automatic wrapping support throughout the C# formatting system. 