# TDD Implementation Checklist: Comma-Separated List Wrapping EditorConfig Integration

## 🎯 **TDD Approach**
Each step follows: **Red → Green → Refactor**
- ❌ **Red**: Write failing test that specifies desired behavior
- ✅ **Green**: Write minimal code to make test pass  
- 🔄 **Refactor**: Clean up implementation while keeping tests green

---

## 📋 **Milestone 1: C# Basic Wrapping**

### **Phase 1.1: EditorConfig Option Foundation**

#### Step 1.1.1: EditorConfig Option Definition
- [ ] ❌ **Test**: EditorConfig parser recognizes `dotnet_separated_list_wrapping` option
- [ ] ❌ **Test**: Parser correctly handles all 5 values: `do_not_wrap`, `align_wrapped`, `unwrap_and_indent_all`, `keep_first_indent_remaining`, `unwrap_to_new_line`
- [ ] ❌ **Test**: Parser defaults to `do_not_wrap` when option is missing
- [ ] ❌ **Test**: Parser handles invalid values gracefully (falls back to default)
- [ ] ✅ **Implement**: `SeparatedListWrappingStyle` enum in `FormattingOptions2.cs`
- [ ] ✅ **Implement**: EditorConfig serializer for the enum
- [ ] 🔄 **Refactor**: Clean up option definition and tests

#### Step 1.1.2: Formatting Options Integration  
- [ ] ❌ **Test**: `CSharpSyntaxFormattingOptions` exposes `SeparatedListWrapping` property
- [ ] ❌ **Test**: Options flow from EditorConfig → `CSharpSyntaxFormattingOptions`
- [ ] ❌ **Test**: Default behavior preserved when `do_not_wrap` specified
- [ ] ✅ **Implement**: Add `SeparatedListWrapping` to `CSharpSyntaxFormattingOptions`
- [ ] ✅ **Implement**: Wire up options parsing from EditorConfig
- [ ] 🔄 **Refactor**: Ensure clean options flow

### **Phase 1.2: Basic Formatting Rule Infrastructure**

#### Step 1.2.1: Formatting Rule Registration
- [ ] ❌ **Test**: `SeparatedListWrappingFormattingRule` is registered in C# formatting pipeline
- [ ] ❌ **Test**: Rule is invoked during formatting operations
- [ ] ❌ **Test**: Rule short-circuits immediately when `do_not_wrap` specified (basic functionality test)
- [ ] ✅ **Implement**: Create `SeparatedListWrappingFormattingRule` class
- [ ] ✅ **Implement**: Register rule in `CSharpSyntaxFormatting._rules`
- [ ] 🔄 **Refactor**: Optimize rule ordering

#### Step 1.2.2: Syntax Detection Logic
- [ ] ❌ **Test**: Rule correctly identifies parameter lists needing wrapping
- [ ] ❌ **Test**: Rule ignores parameter lists with ≤1 parameters  
- [ ] ❌ **Test**: Rule handles syntax errors gracefully (no wrapping when errors present)
- [ ] ❌ **Test**: Rule respects `WrappingColumn` for `_if_long` detection
- [ ] ✅ **Implement**: Parameter list detection in formatting rule
- [ ] ✅ **Implement**: Integration with existing `WrappingColumn` option
- [ ] 🔄 **Refactor**: Extract common detection logic

### **Phase 1.3: Refactoring Integration**

#### Step 1.3.1: Wrapper Delegation  
- [ ] ❌ **Test**: Formatting rule successfully creates `CSharpParameterWrapper` instance
- [ ] ❌ **Test**: Rule can invoke `TryCreateComputerAsync` with formatting options
- [ ] ❌ **Test**: Rule receives `SeparatedSyntaxListCodeActionComputer` for valid scenarios
- [ ] ❌ **Test**: Rule handles null computer result gracefully
- [ ] ✅ **Implement**: Create wrapper instance in formatting rule
- [ ] ✅ **Implement**: Convert formatting options → wrapping options
- [ ] 🔄 **Refactor**: Clean up option conversion logic

#### Step 1.3.2: Edit Conversion Logic
- [ ] ❌ **Test**: Formatting rule converts `Edit` objects to formatting operations
- [ ] ❌ **Test**: Token operations correctly handle whitespace changes
- [ ] ❌ **Test**: Conversion preserves exact transformation behavior from manual refactoring
- [ ] ❌ **Test**: Multiple edits in same parameter list are applied correctly
- [ ] ✅ **Implement**: `Edit` → `FormattingOperation` conversion logic
- [ ] ✅ **Implement**: Token operation generation from edits
- [ ] 🔄 **Refactor**: Optimize conversion performance

### **Phase 1.4: Core Wrapping Styles**

#### Step 1.4.1: `align_wrapped` Style
- [ ] ❌ **Test**: `align_wrapped` produces identical output to manual "Align wrapped parameters" refactoring
- [ ] ❌ **Test**: First parameter stays with opening parenthesis  
- [ ] ❌ **Test**: Subsequent parameters align with first parameter position
- [ ] ❌ **Test**: Works with `dotnet format` command
- [ ] ✅ **Implement**: `align_wrapped` → `WrappingStyle.UnwrapFirst_AlignRest` mapping
- [ ] ✅ **Implement**: Integration test with `dotnet format`
- [ ] 🔄 **Refactor**: Verify identical behavior with manual refactoring

#### Step 1.4.2: `unwrap_and_indent_all` Style  
- [ ] ❌ **Test**: `unwrap_and_indent_all` produces identical output to manual "Unwrap and indent all parameters" refactoring
- [ ] ❌ **Test**: Opening parenthesis followed by newline
- [ ] ❌ **Test**: All parameters indented consistently  
- [ ] ❌ **Test**: Closing parenthesis on separate line
- [ ] ✅ **Implement**: `unwrap_and_indent_all` → `WrappingStyle.WrapFirst_IndentRest` mapping
- [ ] ✅ **Implement**: Integration test with format-on-save
- [ ] 🔄 **Refactor**: Ensure consistent indentation handling

#### Step 1.4.3: `keep_first_indent_remaining` Style
- [ ] ❌ **Test**: `keep_first_indent_remaining` produces identical output to manual "Indent wrapped parameters" refactoring  
- [ ] ❌ **Test**: First parameter stays with opening parenthesis
- [ ] ❌ **Test**: Subsequent parameters are simply indented (not aligned)
- [ ] ❌ **Test**: Handles complex parameter types correctly
- [ ] ✅ **Implement**: `keep_first_indent_remaining` → `WrappingStyle.UnwrapFirst_IndentRest` mapping
- [ ] ✅ **Implement**: Test with attributes and default values
- [ ] 🔄 **Refactor**: Handle edge cases gracefully

#### Step 1.4.4: `unwrap_to_new_line` Style
- [ ] ❌ **Test**: `unwrap_to_new_line` produces identical output to manual "Unwrap parameter list" refactoring
- [ ] ❌ **Test**: All parameters on new line together, indented
- [ ] ❌ **Test**: Single line of parameters after opening parenthesis
- [ ] ❌ **Test**: Closing parenthesis follows immediately  
- [ ] ✅ **Implement**: `unwrap_to_new_line` style mapping
- [ ] ✅ **Implement**: Edge case tests (long parameter names, etc.)
- [ ] 🔄 **Refactor**: Optimize for readability

### **Phase 1.5: Integration & Validation**

#### Step 1.5.1: End-to-End Integration
- [ ] ❌ **Test**: Full pipeline works: EditorConfig → `dotnet format` → Expected output
- [ ] ❌ **Test**: Format-on-save applies wrapping automatically  
- [ ] ❌ **Test**: Manual refactoring still available and unchanged
- [ ] ❌ **Test**: Basic functionality works with `do_not_wrap` (no changes applied)
- [ ] ✅ **Implement**: Full integration tests for all 4 styles
- [ ] ✅ **Implement**: Integration tests with `dotnet format`
- [ ] 🔄 **Refactor**: Clean up integration test setup

#### Step 1.5.2: Edge Cases & Error Handling
- [ ] ❌ **Test**: Handles nested parameter lists correctly
- [ ] ❌ **Test**: Works with parameter attributes and default values
- [ ] ❌ **Test**: Graceful handling when syntax errors present
- [ ] ❌ **Test**: Handles very long parameter names without breaking
- [ ] ✅ **Implement**: Edge case handling in formatting rule
- [ ] ✅ **Implement**: Error recovery logic
- [ ] 🔄 **Refactor**: Robust error handling

---

## 📋 **Milestone 2: VB.NET Support** 

### **Phase 2.1: VB.NET Options Integration**
- [ ] ❌ **Test**: `VisualBasicSyntaxFormattingOptions` exposes same `SeparatedListWrapping` option
- [ ] ❌ **Test**: VB.NET EditorConfig parsing works identically to C#
- [ ] ✅ **Implement**: VB.NET formatting options integration
- [ ] 🔄 **Refactor**: Share common options logic between languages

### **Phase 2.2: VB.NET Formatting Rule**  
- [ ] ❌ **Test**: `VisualBasicSeparatedListWrappingFormattingRule` produces identical behavior to C# version
- [ ] ❌ **Test**: Integrates with `VisualBasicParameterWrapper` correctly
- [ ] ✅ **Implement**: VB.NET formatting rule parallel to C# version
- [ ] 🔄 **Refactor**: Extract shared formatting rule logic

### **Phase 2.3: Language Parity Validation**
- [ ] ❌ **Test**: All 4 wrapping styles work identically in VB.NET
- [ ] ❌ **Test**: EditorConfig settings affect both languages consistently
- [ ] ✅ **Implement**: Cross-language integration tests
- [ ] 🔄 **Refactor**: Ensure consistent behavior across languages

---

## 📋 **Milestone 3: "If Long" Variants**

### **Phase 3.1: Extended EditorConfig Options**
- [ ] ❌ **Test**: Parser recognizes `align_wrapped_if_long`, `unwrap_and_indent_all_if_long`, etc.
- [ ] ❌ **Test**: Line length detection uses existing `WrappingColumn` option
- [ ] ✅ **Implement**: Extended enum values for `_if_long` variants
- [ ] 🔄 **Refactor**: Clean up option parsing logic

### **Phase 3.2: Conditional Wrapping Logic**
- [ ] ❌ **Test**: `_if_long` variants only wrap when exceeding `WrappingColumn`
- [ ] ❌ **Test**: Line length calculation matches existing manual refactoring behavior
- [ ] ✅ **Implement**: Conditional wrapping in formatting rule
- [ ] 🔄 **Refactor**: Optimize line length calculation

### **Phase 3.3: Cross-Construct Consistency**
- [ ] ❌ **Test**: Same line length logic works for parameters, arguments, collections, etc.
- [ ] ❌ **Test**: All `_if_long` variants behave consistently across constructs
- [ ] ✅ **Implement**: Unified line length detection
- [ ] 🔄 **Refactor**: Share line length logic across all construct types

---

## 📋 **Milestone 4: Polish & Integration**

### **Phase 4.1: Comprehensive Testing**
- [ ] ❌ **Test**: Large codebase testing (run on Roslyn itself or other significant codebase)
- [ ] ❌ **Test**: Integration with all VS formatting scenarios
- [ ] ✅ **Implement**: Comprehensive test suite covering edge cases
- [ ] 🔄 **Refactor**: Clean up test infrastructure

### **Phase 4.2: Documentation & Finalization**
- [ ] ❌ **Test**: EditorConfig documentation examples work correctly
- [ ] ❌ **Test**: All success criteria from design document are met
- [ ] ✅ **Implement**: User documentation and examples
- [ ] 🔄 **Refactor**: Final code cleanup and documentation

---

## 🎯 **TDD Success Criteria**

### **Each Phase Complete When:**
- [ ] All tests for that phase are ✅ **Green**
- [ ] Code coverage ≥ 90% for new functionality (or reasonable coverage)
- [ ] Integration tests pass with `dotnet format` and format-on-save
- [ ] Basic functionality tests confirm expected behavior
- [ ] Manual refactoring behavior unchanged and available

### **Milestone Complete When:**
- [ ] All phases within milestone are complete
- [ ] Cross-phase integration tests pass
- [ ] Milestone success criteria from design document are met
- [ ] Ready for next milestone or production deployment

---

## 📝 **Notes**

- **Red-Green-Refactor cycle** should be short (ideally <30 minutes per step)
- **Integration tests** should use real EditorConfig files and `dotnet format` command
- **Focus on functionality** - Performance optimization can be addressed in future iterations
- **Manual verification** should compare output with existing manual refactoring results
- **Each test** should be **independent** and **repeatable**
- **Basic performance** - Ensure `do_not_wrap` doesn't break anything, but don't optimize beyond that 