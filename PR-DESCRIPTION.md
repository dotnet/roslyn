# Expose Comma-Separated List Wrapping to EditorConfig

**Addresses:** [#33872](https://github.com/dotnet/roslyn/issues/33872) - Add editorconfig options for wrapping/unwrapping various language constructs

## 🎯 **Summary**

This PR exposes existing comma-separated list wrapping functionality to EditorConfig, allowing consistent wrapping styles to be automatically applied during formatting operations (`dotnet format`, format-on-save, etc.) rather than only being available as manual refactorings.

**Applies to:** Parameter lists, argument lists, collection expressions, collection initializers, and all other comma-separated syntax constructs.

## 📝 **Motivation**

This PR addresses part of [#33872](https://github.com/dotnet/roslyn/issues/33872), which requests EditorConfig options for wrapping/unwrapping various language constructs. Specifically, this implements automatic wrapping for **comma-separated lists**.

Teams often want consistent comma-separated list formatting applied automatically during formatting operations. Currently, Roslyn provides comprehensive wrapping through manual refactorings (`CSharpParameterWrapper`, `CSharpArgumentWrapper`, etc.), but these are only available through the lightbulb menu.

This PR bridges that gap by:
- ✅ **Automatic formatting integration** - Works with `dotnet format`, format-on-save, CI/CD pipelines
- ✅ **Team consistency** - EditorConfig ensures all team members use the same wrapping style  
- ✅ **Zero code duplication** - Reuses existing proven wrapping infrastructure
- ✅ **Backward compatibility** - No breaking changes, opt-in only

## ⚙️ **EditorConfig Integration**

### **New Option**
```editorconfig
# Comma-separated list wrapping style (applies to both C# and VB.NET)
dotnet_separated_list_wrapping = do_not_wrap | align_wrapped | unwrap_and_indent_all | keep_first_indent_remaining | unwrap_to_new_line
```

### **Wrapping Styles**
| Value | Behavior | Example |
|-------|----------|---------|
| `do_not_wrap` | **Default** - No automatic wrapping | `void Method(int a, int b, int c)` |
| `align_wrapped` | Keep first item with parent, align remaining | `void Method(int a,`<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`int b,`<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`int c)` |
| `unwrap_and_indent_all` | Wrap all items, indent consistently | `void Method(`<br>&nbsp;&nbsp;&nbsp;&nbsp;`int a,`<br>&nbsp;&nbsp;&nbsp;&nbsp;`int b,`<br>&nbsp;&nbsp;&nbsp;&nbsp;`int c)` |
| `keep_first_indent_remaining` | Keep first item, indent remaining | `void Method(int a,`<br>&nbsp;&nbsp;&nbsp;&nbsp;`int b,`<br>&nbsp;&nbsp;&nbsp;&nbsp;`int c)` |
| `unwrap_to_new_line` | All items on new line together | `void Method(`<br>&nbsp;&nbsp;&nbsp;&nbsp;`int a, int b, int c)` |

## 🏗️ **Architecture**

**Hybrid Pipeline Approach:**
```
EditorConfig → FormattingOptions → SeparatedListWrappingFormattingRule → CSharpParameterWrapper → Formatting Operations
```

**Benefits:**
- **Zero code duplication** - All wrapping logic remains in proven refactoring infrastructure
- **Consistent behavior** - Manual refactoring and auto-formatting use identical transformations  
- **Clean separation** - Formatting handles "when", refactoring handles "what"

## 📋 **Implementation Status**

### ✅ **Completed**
- [x] **Design Document** - Comprehensive feature design with architecture decisions
- [x] **TDD Checklist** - Step-by-step implementation plan with testable behaviors
- [x] **Branch Setup** - Clean feature branch with focused scope

### 🚧 **In Progress**
- [ ] **Milestone 1: C# Basic Wrapping**
  - [ ] EditorConfig option parsing
  - [ ] Formatting rule infrastructure  
  - [ ] Refactoring integration
  - [ ] Core 4 wrapping styles
  - [ ] Integration testing

### 📅 **Planned**
- [ ] **Milestone 2: VB.NET Support** - Parallel implementation for VB.NET
- [ ] **Milestone 3: "If Long" Variants** - Line length conditional wrapping (`align_wrapped_if_long`, etc.)
- [ ] **Milestone 4: Polish & Documentation** - Edge cases, comprehensive testing, user docs

## 🧪 **Testing Strategy**

**TDD Approach:**
- **Red**: Write failing test specifying exact behavior
- **Green**: Minimal implementation to pass test
- **Refactor**: Clean up while maintaining green tests

**Key Test Categories:**
- **EditorConfig parsing** - All option values handled correctly
- **Integration testing** - Real `dotnet format` and format-on-save scenarios
- **Compatibility testing** - Manual refactoring behavior unchanged
- **Cross-construct testing** - Same behavior across parameters, arguments, collections
- **Edge case testing** - Syntax errors, complex scenarios, attributes

## 🔄 **Compatibility**

### **Backward Compatibility**
- ✅ **Zero breaking changes** - Default behavior (`do_not_wrap`) preserves current formatting
- ✅ **Opt-in only** - No impact until teams explicitly configure the option
- ✅ **Manual refactoring unchanged** - Existing lightbulb actions remain available

### **Performance**
- ✅ **Zero overhead for default** - `do_not_wrap` short-circuits immediately
- ✅ **Minimal impact when enabled** - Reuses existing proven algorithms
- ✅ **Focus on functionality** - Basic performance verification, detailed optimization can come later

## 📚 **Related Work**

**Existing Infrastructure Used:**
- `AbstractCSharpSeparatedSyntaxListWrapper` - Base wrapping logic
- `CSharpParameterWrapper`, `CSharpArgumentWrapper`, etc. - Specific construct wrappers
- `SeparatedSyntaxListCodeActionComputer` - Wrapping transformation engine
- `FormattingOptions2` - EditorConfig integration pattern
- `WrappingColumn` option - Line length detection (for future `_if_long` variants)

## 🎯 **Success Criteria**

### **Milestone 1 Complete When:**
- [ ] All 4 wrapping styles work identically to manual refactoring
- [ ] `dotnet format` applies wrapping automatically based on EditorConfig
- [ ] Format-on-save integrates correctly
- [ ] Manual refactoring still available and unchanged
- [ ] Basic functionality tests confirm expected behavior

### **Overall Feature Complete When:**
- [ ] Both C# and VB.NET supported
- [ ] "If long" variants integrated with line length detection
- [ ] Comprehensive edge case handling
- [ ] User documentation with examples
- [ ] All design document success criteria met

---

**Status**: 🟡 **Design Complete, Implementation Starting**  
**Next Step**: Begin TDD implementation of Milestone 1, Phase 1.1  
**Target**: Working basic wrapping functionality for C# parameter lists 