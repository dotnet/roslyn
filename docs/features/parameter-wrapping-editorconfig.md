# Comma-Separated List Wrapping EditorConfig Integration

## Summary

This feature exposes existing comma-separated list wrapping functionality to EditorConfig, allowing consistent wrapping styles to be automatically applied during formatting operations (`dotnet format`, format-on-save, etc.) rather than only being available as manual refactorings.

**Scope:** Applies to all comma-separated syntax constructs including parameter lists, argument lists, collection expressions, and collection initializers.

**Language Support:** Both C# and VB.NET have equivalent manual wrapping functionality through `AbstractCSharpSeparatedSyntaxListWrapper` and `AbstractVisualBasicSeparatedSyntaxListWrapper`. This design addresses both languages with a single unified option.

## Motivation

Currently, Roslyn provides comprehensive comma-separated list wrapping through multiple refactoring providers (`CSharpParameterWrapper`, `CSharpArgumentWrapper`, etc.), offering consistent wrapping styles across all comma-separated constructs. However, these are only available as manual refactorings accessed through the lightbulb menu.

Teams often want consistent comma-separated list formatting applied automatically during formatting operations. This allows:

1. **Consistent Code Style**: Enforce uniform wrapping across parameters, arguments, collections, etc.
2. **Automatic Formatting**: Integration with `dotnet format`, format-on-save, and CI/CD pipelines  
3. **Reduced Manual Work**: No need to manually apply wrapping refactorings
4. **Team Consistency**: EditorConfig ensures all team members use the same wrapping style

**Current Gap**: Automatic formatting handles indentation, spacing, and new lines, but doesn't handle semantic wrapping decisions for comma-separated constructs.

### Current Comma-Separated List Wrapping Actions
Both `AbstractCSharpSeparatedSyntaxListWrapper` and `AbstractVisualBasicSeparatedSyntaxListWrapper` provide equivalent refactoring actions for all comma-separated constructs:

1. **Align wrapped items** - `void Goo(int i,\n         int j)`
2. **Unwrap and indent all** - `void Goo(\n    int i,\n    int j)`  
3. **Keep first, indent remaining** - `void Goo(int i,\n    int j)`
4. **Unwrap to new line** - `void Goo(\n    int i, int j)`

This feature exposes these existing, well-tested wrapping behaviors as automatic formatting rules.

## Current State

### Existing Infrastructure
- **`CSharpParameterWrapper`** - Provides manual refactoring for parameter wrapping
- **`AbstractWrappingCodeRefactoringProvider`** - Base infrastructure for wrapping refactorings  
- **`SyntaxWrappingOptions`** - Configuration system for wrapping behaviors
- **`CSharpFormattingOptions2`** - EditorConfig integration for C# formatting

### Current Parameter Wrapping Actions
Both `CSharpParameterWrapper` and `VisualBasicParameterWrapper` offer equivalent refactoring actions:
1. **Align wrapped parameters** - `void Goo(int i,\n         int j)`
2. **Unwrap and indent all** - `void Goo(\n    int i,\n    int j)`  
3. **Wrap first, indent rest** - `void Goo(int i,\n    int j)`
4. **Unwrap all parameters** - `void Goo(\n    int i, int j)`
5. **Unwrap to single line** - `void Goo(int i, int j)`

### Gap
These wrapping styles are not available during automatic formatting operations for either language.

## Proposed Solution

### Phase 1: EditorConfig Option
Add a new language-agnostic EditorConfig option that applies to **all comma-separated constructs** (parameters, arguments, collection expressions, initializers):

```editorconfig
# Comma-separated list wrapping style (applies to both C# and VB.NET)
dotnet_separated_list_wrapping = do_not_wrap | align_wrapped | unwrap_and_indent_all | keep_first_indent_remaining | unwrap_to_new_line
```

**Applies To:**
- Parameter lists: `void Method(int a, int b)`
- Argument lists: `Method(value1, value2)`  
- Collection expressions: `[item1, item2, item3]`
- Collection initializers: `new List<int> { 1, 2, 3 }`
- Any comma-separated syntax construct

**Option Values** (based on existing `AbstractCSharpSeparatedSyntaxListWrapper` functionality):
- `do_not_wrap` (default) - **Preserve current behavior**: No automatic wrapping applied during formatting
- `align_wrapped` - Keep first item with parent, align remaining items with first
  ```csharp
  void Method(int first,
              int second,
              int third)
  ```
- `unwrap_and_indent_all` - Wrap all items including first, indent all consistently  
  ```csharp
  void Method(
      int first,
      int second,
      int third)
  ```
- `keep_first_indent_remaining` - Keep first item with parent, indent remaining items
  ```csharp
  void Method(int first,
      int second,
      int third)
  ```
- `unwrap_to_new_line` - Place all items on new line together, indented
  ```csharp
  void Method(
      int first, int second, int third)
  ```

**Line Length Integration:** For each wrapping style, an additional `_if_long` variant could be supported to only apply wrapping when exceeding the configured line length (`dotnet_max_line_length`).

**Backward Compatibility:** When no `dotnet_parameter_wrapping` option is specified, formatting behavior remains completely unchanged. Existing codebases will see no difference until they explicitly opt-in by adding the EditorConfig option.

**Future Expansion:** Language-specific overrides could be added later if needed:
```editorconfig
# Future: Language-specific overrides (if requested)
# csharp_parameter_wrapping = wrap_every_parameter    # Overrides dotnet_parameter_wrapping for C#
# visual_basic_parameter_wrapping = wrap_long_parameters  # Overrides dotnet_parameter_wrapping for VB.NET
```

### Phase 2: Formatting Rule Integration
Create formatting rules for both languages that apply parameter wrapping based on the shared EditorConfig setting.

### Phase 3: Additional Options (Future)
After proving the concept, add additional wrapping styles:
- `dotnet_parameter_first_placement = same_line | new_line`  
- `dotnet_parameter_alignment = align_with_first | indent_uniform`

## Technical Design

### Current Architecture
The manual parameter wrapping refactoring already exists with this architecture:
- **`CSharpParameterWrapper`** - Handles parameter list wrapping logic
- **`AbstractCSharpSeparatedSyntaxListWrapper`** - Base class for comma-separated list wrapping
- **`CSharpSyntaxWrappingOptions`** - Configuration for wrapping behavior
- **`SeparatedSyntaxListCodeActionComputer`** - Generates the 4 different wrapping code actions

### Integration Points
Instead of building new wrapping logic, this feature **exposes existing functionality** through:

1. **EditorConfig → FormattingOptions** 
   - Add `ParameterWrappingStyle` enum to shared `FormattingOptions2`
   - Map EditorConfig values to wrapping style enum

2. **FormattingOptions → Wrapping Logic**
   - Extend `CSharpSyntaxWrappingOptions` to consume the EditorConfig setting
   - **Reuse existing `CSharpParameterWrapper` logic** during formatting

3. **Formatting Pipeline Integration**
   - Create `ParameterWrappingFormattingRule` that applies chosen wrapping style
   - Leverage existing `SeparatedSyntaxListCodeActionComputer` to determine transformations
   - Integrate into `CSharpSyntaxFormatting._rules`

### Key Implementation Strategy
- **Reuse, don't rebuild**: Leverage existing `CSharpParameterWrapper` infrastructure
- **Consistent behavior**: Ensure EditorConfig wrapping matches manual refactoring results exactly  
- **Performance**: Minimize overhead when `do_not_wrap` is specified (default)

## Implementation Plan

### Phase 1: Foundation (Milestone 1) - C# First
1. **Add shared EditorConfig option definition** 
   - Define `ParameterWrappingOptionsInternal` enum in shared location
   - Add `dotnet_parameter_wrapping` option to `FormattingOptions2`
   - Add EditorConfig serialization support

2. **Extend C# formatting options**
   - Update `CSharpSyntaxFormattingOptions` to consume the shared option
   - Update `CSharpSyntaxWrappingOptions` to use parameter wrapping settings

3. **Create basic C# formatting rule**
   - Implement `ParameterWrappingFormattingRule` with stub logic
   - Integrate into C# formatting pipeline
   - Add to `CSharpSyntaxFormatting._rules`

### Phase 2: Core Logic + VB.NET (Milestone 2)
1. **Extract shared wrapping utilities**
   - Create language-agnostic `ParameterWrappingUtilities` class
   - Extract algorithms from both `CSharpParameterWrapper` and `VisualBasicParameterWrapper`
   - Ensure both refactoring and formatting can use shared code

2. **Implement C# wrapping logic**
   - Add `WrapEveryParameter` support in C# formatting rule
   - Add `WrapLongParameters` support with column detection
   - Handle parameter alignment and indentation

3. **Add VB.NET support**
   - Update `VisualBasicSyntaxFormattingOptions` to consume the shared option
   - Create VB.NET formatting rule parallel to C# version
   - Integrate into VB.NET formatting pipeline

4. **Testing**
   - Unit tests for both language formatting rules
   - Integration tests with `dotnet format` for both languages
   - EditorConfig option parsing tests

### Phase 3: Polish & Documentation (Milestone 3)
1. **Edge case handling**
   - Handle complex parameter scenarios (attributes, default values, etc.)
   - Ensure compatibility with existing formatting rules
   - Performance optimization

2. **Documentation**
   - EditorConfig documentation for new option
   - Migration guide for teams wanting automatic parameter wrapping

### 4. Reuse Existing Logic

Leverage existing parameter wrapping logic from both wrappers:
- Extract shared wrapping algorithms into utility classes
- Avoid duplicating wrapping logic between refactoring and formatting
- Ensure consistent behavior across manual refactoring and automatic formatting

## Testing Strategy

### Unit Tests
- **EditorConfig parsing**: Test option serialization/deserialization
- **Formatting rule behavior**: Test wrapping logic in isolation for both languages
- **Integration**: Test interaction with other formatting rules

### Integration Tests
- **dotnet format**: Verify command-line formatting works for both languages
- **VS integration**: Test format-on-save scenarios
- **Cross-platform**: Ensure consistent behavior across environments

### Test Cases
```csharp
// Input (C#)
void Method(int parameter1, int parameter2, int reallyLongParameterName, int anotherParameter) { }

// dotnet_parameter_wrapping = wrap_every_parameter
void Method(
    int parameter1,
    int parameter2, 
    int reallyLongParameterName,
    int anotherParameter) { }

// dotnet_parameter_wrapping = wrap_long_parameters (with dotnet_max_line_length = 80)
void Method(int parameter1, int parameter2,
           int reallyLongParameterName, int anotherParameter) { }
```

```vb
' Input (VB.NET)
Sub Method(parameter1 As Integer, parameter2 As Integer, reallyLongParameterName As Integer, anotherParameter As Integer)

' dotnet_parameter_wrapping = wrap_every_parameter
Sub Method(
    parameter1 As Integer,
    parameter2 As Integer,
    reallyLongParameterName As Integer,
    anotherParameter As Integer)
```

## Compatibility

### Backward Compatibility
- Default option value maintains current behavior (no automatic wrapping)
- Existing refactoring functionality remains unchanged
- No breaking changes to formatting APIs

### Forward Compatibility  
- Design allows for language-specific overrides if needed in future
- Architecture supports extending to other construct types (arguments, expressions, etc.)

## Open Questions

1. **"If Long" Variants**: Should we implement `_if_long` variants (e.g., `align_wrapped_if_long`) in Phase 1, or add them later based on demand?

2. **Line Length Integration**: Should this use the existing `dotnet_max_line_length` option or introduce a separate wrapping threshold for comma-separated lists?

3. **Performance Impact**: How do we ensure minimal overhead when `do_not_wrap` is specified, especially in large codebases with many comma-separated constructs?

4. **Manual Refactoring Interaction**: Should the presence of an EditorConfig setting affect what manual refactoring options are shown in the lightbulb menu for comma-separated lists?

5. **Formatting vs. Refactoring Pipeline**: Which pipeline should drive this - extend the existing formatting system or create a new hybrid approach that bridges manual refactoring logic into automatic formatting?

6. **Construct Priority**: If different comma-separated constructs have conflicting wrapping needs in the same file, how do we handle that? (This may be theoretical given the unified approach.)

## Success Criteria

### Milestone 1 Complete
- [ ] Shared `dotnet_parameter_wrapping` EditorConfig option defined and integrated
- [ ] Basic C# formatting rule infrastructure in place
- [ ] Options flow from EditorConfig → C# Formatting pipeline

### Milestone 2 Complete  
- [ ] `wrap_every_parameter` fully functional for C#
- [ ] `wrap_long_parameters` with line length detection for C#
- [ ] VB.NET formatting rule implemented using shared option  
- [ ] Integration tests passing with `dotnet format` for both languages

### Milestone 3 Complete
- [ ] Edge cases handled (attributes, default values, complex scenarios)
- [ ] Performance impact measured and acceptable
- [ ] Documentation complete
- [ ] Ready for broader team review

## Future Extensions

After parameter wrapping proves successful, this approach can be extended to:
- **Argument wrapping** (`dotnet_argument_wrapping`)
- **Binary expression wrapping** (`dotnet_binary_expression_wrapping`)  
- **Chained expression wrapping** (`dotnet_chained_expression_wrapping`)

**Language-specific overrides** (if requested):
- `csharp_parameter_wrapping` / `visual_basic_parameter_wrapping` to override `dotnet_parameter_wrapping`

This creates a path toward comprehensive automatic wrapping support throughout the .NET formatting system. 
