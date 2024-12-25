# Purpose of this document

This document is meant to be a comprehensive list of Roslyn features for use in several kinds of testing.
- [**Routine Product Testing**](#routineproducttesting)
- [**Release Testing**](#releasetesting)
- [**New Language Feature Testing**](#newlanguagefeaturetesting)

## <a name="routineproducttesting">Routine Product Testing</a>
- Primarily focused on features marked with a :mag:

## <a name="releasetesting">Release Testing</a>
- Test all product features in all languages

## <a name="newlanguagefeaturetesting">New Language Feature Testing</a>

- Test phases
    - **Merge Signoff**: Can the language feature be merged?
        - Critical IDE features in the areas of **Typing**, **Navigating**, and **Viewing** must function as expected. Any non-trivial issues in these areas block language feature merging.
            - These are marked with a :fast_forward:.
        - All product features have been considered/tested are free of crashes, asserts, and hangs.
            -All issues discovered (even non-crash/non-blocking) must be filed and linked here.
                - _All feature descriptions and testing suggestions are merely examples. Each language feature should be carefully considered independently against each IDE feature to find interesting intersections_
    - **Feature Signoff**: Per-feature signoff with links to non-blocking issues.
    - **Feature Suggestions**: What can we add to the product to make the new language feature delightful to use?

- Testing approaches
    - Read the **feature specification** and related conversations
    - Think about how the new language feature **interacts with each product feature**
        - Are new keywords visualized appropriately?
        - Do refactorings and fixes produce correct code utilizing the new language feature?
        - How do IDE error recovery scenarios work?
        - Do things work well across languages?
        - How is perf on large/medium/small solutions? Are features properly cancellable?
        - Etc.
    - **Brainstorm ideas** for new features to enhance the language feature
        - Should existing code be refactored to some new pattern?
        - Should users be able to easily switch between two equivalent forms?
        - Do we need to make the flow of this feature easier to visualize?
        - Etc.

# Testing tags

- :mag: = Scenarios that are regularly tested against internal builds
- :fast_forward: = Scenarios that must work as expected before new language features are merged

When doing a test pass, copy this page and consider using these status indicators:

- :white_check_mark: = Tested & Approved (possibly with linked bugs)
- :x: = Merge blocking
- :construction: = Non-blocking bugs
- :ok: = Issue has been fixed
- :free: = No expected impact
- :question: = Open question

<a name="testfootnote1"><sup>1</sup></a> Feature updated by compiler team, with appropriate unit tests.

<a name="testfootnote2"><sup>2</sup></a> Feature requires more complete testing by IDE team.

# Product Features

### Core Scenarios in Editing, Navigating, and Viewing

| Category | Feature/Description | C# Signoff/Notes | VB Signoff/Notes | F# Signoff/Notes |
| --- | --- | --- | --- | --- | 
| **Enable/Disable** | :fast_forward: **Feature Flags** <br />To completely enables/disable new compiler features in the compiler & IDE | | | N/A |
| **Typing** | :fast_forward: **General Typing**<br />- Type and paste new constructs<br />- Nothing interferes with verbatim typing | | | |
| | :mag: :fast_forward: **Completion**<br />- Typing new keyword/construct names<br />- Dotting off of new constructs<br />- Matching part of the identifier is highlighted (including word prefix matches) [Visual Studio 2015 Update 1]<br />- Target type preselection [Visual Studio 2017]<br />IntelliSense filtering [Visual Studio 2017] | | | |
| | :fast_forward: **Formatting** <br />- Spacing in and around new constructs<br />- Spacing options<br />- Format Document command<br /> `Tools > Options` settings should be respected | | | |
| | :fast_forward: **Automatic Brace Completion** (*C# only*) <br />- Auto-insert close brace<br />- Shift+Enter commit of IntelliSense and any pending brace completion sessions | | | N/A |
| | :fast_forward: **Indentation** <br />- Typing `Enter` in an unfinished statement indents the next line | | | |
| **Navigating** | :mag: :fast_forward: **Go To Definition** <br />- F12 from callsites to definition<br />- Ctrl+click [Visual Studio 2017 version 15.4] | | | |
| | :fast_forward: **Go To Implementation** <br />- Ctrl+F12 to jump from virtual members to their implementations<br />- Jump from inheritable types to their implementations | | | N/A |
| | :mag: :fast_forward: **Find All References** <br />- Lists references to a symbol in "Find Symbol Results" window<br />- Shows results in hierarchy grouped by definition [Visual Studio 2015]<br />- Results should be groupable/filterable/classified appropriately [Visual Studio 2017] <br />- Find All References on literals [Visual Studio 2017 version 15.3] | | | |
| **Viewing** | :mag: :fast_forward: **Colorization** <br />- Keywords, literals, and identifiers colored appropriately in code<br />- Colors should theme appropriately<br />- The "Blue Theme (Additional Contrast)" should have sufficiently dark colors | | | |
| | :fast_forward: **Error Squiggles** <br />- Squiggles appear as expected on reasonable spans | | | |

### Code Transformations
| Category | Feature/Description | C# Signoff/Notes | VB Signoff/Notes | F# Signoff/Notes |
| --- | --- | --- | --- | --- |
| **Refactoring with UI** | :mag: **Inline Rename (with UI)**<br />- Dashboard shows correct information<br />- Highlighted spans are updated appropriately<br />- Rename operation updates the correct set of symbols | | | |
| | :mag: **Change Signature (with UI)**<br />- Updates all direct & cascaded definitions/callsites<br />- Shows appropriate signature & parameter previews in UI<br />- Reorder and Remove in the same session [Visual Studio 2015] | | | N/A |
| | :mag: **Extract Interface (with UI)**<br />- Generated Interface has expected shape<br />- UI shows appropriate method previews | | | N/A |
| | **Generate Type (with UI)**<br />- Dialog gives all valid options<br /> | | | N/A |
| | **Generate Overrides** [Visual Studio 2017 version 15.3] | | | N/A |
| **Refactorings** | **Rename Tracking**<br />- Tracking span tracks & dismisses as expected<br />- Invokable from references [Visual Studio 2015] | | | N/A |
| | :mag: **Extract Method**<br />- Extracted method has the expected signature<br />- All arguments/return values handled correctly<br />- Extracted code block is reasonable<br />- Automatically starts Inline Rename | | | N/A |
| | **Introduce Variable**<br />- Introduced variable has the expected signature and initializer expression<br />- "Introduce for All" correctly finds dupes | | | N/A |
| | **Inline Temporary Variable**<br />- Inlined values are appropriately expanded/reduced | | | N/A |
| | **Organize Usings**<br />- Honors "Place 'System' namespace first" option | | | N/A |
| | **Convert `Get*` Methods to Properties**<br />*Add Description* | | | N/A |
| | :mag: **Encapsulate Field**<br />- Select a field and convert it to a property backed by the field<br />- Try selecting multiple fields at once | | | N/A |
| | **Modifier Ordering** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Convert `ReferenceEquals` to `is null`** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Add Missing Modifiers** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Convert Lambda to Local Function** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Move Declaration Near Reference** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Introduce Pattern Matching** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Simplify Inferred Tuple Names** [Visual Studio 2017 version 15.5] | _Requires C# 7.1 or greater_ | | N/A |
| | **Convert keyword and symbol references to doc comment tags** [Visual Studio 2017 version 15.5] | | | N/A |
| **Fixes** | **Add Using**<br />- Triggers on appropriate constructs<br />- Including NuGet references<br />- Including Referenced Assemblies<br />- Includes spelling fixes | | | |
| | **Generate Local**<br />- Select an expression and introduce a local variable to represent it<br />- This should start an Inline Rename session | | | N/A |
| | **Generate Field**<br />- Select an expression and introduce a field to represent it<br />- This should start an Inline Rename session | | | N/A |
| | **Generate Method/Constructor**<br />- Call a nonexistent method or constructor to generate it from its usage<br />- Generated method has the expected signature and accessibility<br />- Add parameter to existing constructor from callsite [Visual Studio 2017 version 15.3] | | | N/A |
| | **Generate Constructor from members**<br />- Select fields/properties to generate a constructor accepting corresponding arguments<br />- Generated constructor has the expected signature and accessibility | | | N/A |
| | **Implement Interface**<br />- Only missing methods added<br />- All added methods have the expected signature and accessibility | | | N/A |
| | **Implement IDisposable**<br />- Implement IDisposable and you should see a large block of code properly implementing that particular interface | | | N/A |
| | **Implement Abstract Class**<br />- Inherit from an abstract class, and you should be able to auto-generate all of the missing members | | | N/A |
| | **Remove Unused Variables** [Visual Studio 2017 version 15.3]| | | N/A |
| | **Remove Unused Usings** | | | |
| | **Sort Usings** | | | N/A |
| | **Convert Get Methods to Properties**<br />- Name a method `GetStuff` and convert it to a property called `Stuff` | | | N/A |
| | **Make Method Async/Sync**<br />- Add an `await` to a synchronous method, you should be offered to add the async keyword<br />- Remove all `await` keywords from an async method, you should be offered to remove the async keyword | | | N/A |
| | **Use Object Initializer Over Property Assignment**<br />- Create a new instance of a type and then assign each of its properties on subsequent lines<br />- You should be offered to convert that to an object initializer | | | N/A |
| | **Insert Digit Separators** [Visual Studio 2017 version 15.3] | | | N/A |
| **Code Gen** | :mag: **Snippets**<br />- Tab completion, presence in completion list<br />- Insertion via Snippet Picker UI (Ctrl + K, Ctrl + X) or (Ctrl + K, Ctrl + S)<br />- (VB) Snippet Picker UI via `?<Tab>`<br />- (VB) Special snippet completion list (`p?<esc><tab>`) | | | N/A |
| | **Event Hookup on Tab** (*C# only*)<br />- Type "+=" after an event name and QuickInfo shows<br />- Invoking should pick good name & launch Inline Rename | | N/A | N/A |
| | **End Construct Generation** (*VB only*)<br />- Type `Sub Test()` and hit enter, the `End Sub` should be generated automatically | N/A | | N/A |
| | **Automatic End Construct Update** (*VB only*)<br />- Type `Sub Test()` and `End Sub`, changing `Sub` to `Function` in either one should update the other | N/A | | N/A |
| | **Spell checking**<br />- Type a name that's close to a variable name but off by a character or two<br />- Lightbulb should have option to fix up the spelling<br />- Includes type names that will require a using | | | N/A |
| | **Move type to file**<br />- Lightbulb to move type to another file when the type name doesn't match the filename<br />- Option to change the file name if the type doesn't match the file name | | | N/A |
| | **Convert between properties and Get methods** <br />- Offers to change a method named `GetStuff` to a property named `Stuff` | | | N/A |
| | **Convert auto property to full property** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Add missing cases**<br />Use a `switch` on a strict subset of an Enum's members<br />- It should offer to generate the rest of the cases | | | N/A |
| | **Add null checks for parameters** [Visual Studio 2017 version 15.3] | | | N/A |
| | **Change base for numeric literals** [Visual Studio 2017 version 15.3] | | | N/A |
| | **Convert if to switch** [Visual Studio 2017 version 15.3] | | | N/A |
| | **Resolve git merge conflicts** [Visual Studio 2017 version 15.3] | | | N/A |
| | **Add argument name** [Visual Studio 2017 version 15.3 | | | N/A |
| | **Fade and remove unreachable code** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Add missing file banner** [Visual Studio 2017 version 15.5] | | | N/A |
| | **Deconstruct tuple declaration** [Visual Studio 2017 version 15.4] | | | N/A |

### IDE Features
| Category | Feature/Description | C# Signoff/Notes | VB Signoff/Notes | F# Signoff/Notes |
| --- | --- | --- | --- | --- |
| **General** | **Signature Help**<br />- Overloads shown with appropriate, colorized signature | | | |
| | **Quick Info**<br />- Hover on identifiers<br />- On completion list items | | | |
| | :mag: **Outlining**<br />- Make sure outlining is enabled under options<br />- Define a method and expect to see a collapsible region around the method definition<br />- Make sure collapse and expand work | | | |
| | **Brace Matching** (*C# only*)<br />- Highlights matching brace token, if caret is on an open or close brace.<br />- Hovering on a close brace shows the code around the open brace | | N/A | N/A |
| | **Highlight References**<br />- Ensure "Highlight references to symbol under cursor" is enabled in options<br />- If caret is on an identifier, all references to that identifier in the active document should be highlighted<br />- We also show related keywords, so placing the caret on a return should show the other returns, if should show elses, try shows catches and finallys, etc.<br />- Should be able to navigate between highlighted references with Ctrl+Shift+Up/Down | | | |
| | :mag: **Peek**<br />Press Alt + F12 after placing the cursor on a predefined Type or predefined member and expect to see to a temporary window showing the appropriate definition | | | |
| | :mag: **Navigation Bars**<br />- Open some existing source files, make sure you can navigate around the file choosing classes or methods.<br />- Switch between project contexts<br />- In VB, the NavBar can do code generation for events and for New/Finalize methods | | | |
| | **Metadata As Source**<br />- Press F12 on a predefined type and expect the cursor to move the predefined type definition inside a Metadata-As-Source Generated document.<br />- Expect to see the xml doc comments collapsed above the method. | | | N/A |
| | **Navigate to Decompiled Source** [Visual Studio 2017 version 15.6 Preview 2]<br />- Set the option at `Tools -> Options -> Text Editor -> C# -> Advanced -> Enable navigation to decompiled sources (experimental)` <br />- F12 on a type defined in metadata<br />- You should see decompiled method bodies, not just declarations | | | N/A |
| | :mag: **Navigate To**<br />- Place caret on a user defined Type reference and press "ctrl + ,"<br />- This should list the User Defined Type in the drop down on the Upper Right corner of the editor and selecting this item will move the cursor to the User Reference Definition<br />- Filters per kind of symbol [Visual Studio 2017]<br />- Results should be sorted as follows: [Visual Studio 2017 version 15.6 Preview 4]<br /><details><summary>Expand for details</summary>TODO</details> | | | |
| | **Go to Next/Previous Method**<br />- `Edit.NextMethod` and `Edit.PreviousMethod` should work<br />- You may need to set up keyboard bindings for these commands under `Tools > Options > Environment > Keyboard` | | | N/A |
| | **Solution Explorer Pivots**<br />- Define a Type and some members in a sample Document.<br />- Expand the Document listed in the Solution Explorer window and expect to see the Type and Members defined<br />- Right-click types and try Base Types / Derived Types / Is Used By / Implements<br /> - Right-click methods and try Calls / Is Called By / Is Used By / Implements | | | N/A |
| | **Call Hierarchy**<br />- Place the caret over a method, right click & select View Hierarchy<br />- This should open the "Call Hierarchy window" listing the methods that call the original method and also the callsites within each calling method. | | | N/A |
| | :mag: **Code Lens**<br />- Make sure Code Lens is enabled from the options. Look for an adornment on top of each method declaration with lists the number of references for that method, last time someone modified the method, who modified the method and other information | | | N/A |
| | **Project System**<br />- Open/close a project<br />- Add/remove references<br />- Unload/reload projects<br />- Source Control integration (adding references checks out projects, etc.) | | | |
| | **Debugger IntelliSense**<br />- Hit a breakpoint (or step) and verify that there's IntelliSense in the Immediate Window (C#, VB)<br />- Type an expression, and hit enter. Verify it's evaluated. Type another expression. IntelliSense should still work.<br />-    (VB) there should be IntelliSense if you type "?" followed by an expression (eg, the text of the line in the window is "?foo")<br />- Hit a breakpoint (or step) and verify that there's IntelliSense in the Watch Window<br />- Hit a breakpoint (or step) and verify that there's IntelliSense in the Quick Watch Window<br /> - Verify intellisense in the Conditional Breakpoint view<br />- Verify each of the above scenarios after hitting f5 and hitting another breakpoint, and after stepping | | | |
| | **Breakpoint Spans**<br />- The span highlighted when a breakpoint is set should be logical | | | |
| | **Code Model / Class Designer**<br />- Right click a file in Solution Explorer & choose View Class Diagram.<br />- This shows the "Class Details" window where you can add/remove members, change their type/accessibility/name, parameter info, etc. | | | N/A |
| | **Object Browser / Class View**<br />- Open object browser and classview and verify that project contents are represented live.<br />- Should be able to invoke find all references, navigate to the definition, and do searches. | | | N/A |
| | **Lightbulb**<br />- Should work with `Ctrl+.` and the right-click menu<br />- Should include a diff view<br />- Should include options to fix all in Document/Project/Solution, and to Preview Changes<br />Varying icons for lightbulb items [Visual Studio 2017 version 15.4] | | | |
| | **Line Separators**<br />- Turn the option on under `Tools > Options`<br />- Ensure there's a line between methods. | | | N/A |
| | **Indent Guides**<br />- Vertical dotted lines appear between braces for declaration-level things<br />- Hovering on the line shows context | | | |
| **Code Style** | **Naming Rules** | | | N/A |
| | **Use this./me.** | | | N/A |
| | **Use predefined type** | | | N/A |
| | **Prefer object/collection initializer** | | | N/A |
| | **Prefer explicit tuple name** | | | N/A |
| | **Prefer coalesce expression over null check** | | | N/A |
| | **Prefer null propagation over null check** | | | N/A |
| | **var preferences** (*C# only*) | | N/A | N/A |
| | **Prefer braces** (*C# only*) | | N/A | N/A |
| | **Prefer pattern matching over is/as checks** (*C# only*) | | N/A | N/A |
| | **Use expression body** (*C# only*) | | N/A | N/A |
| | **Prefer inlined variable declaration** (*C# only*) | | N/A | N/A |
| | **Prefer throw expression** (*C# only*) | | N/A | N/A |
| | **Prefer conditional delegate call** (*C# only*) | | N/A | N/A |

### Project System & External Integration
(Any changes to the runtime will require much more testing in this category)
*All feature descriptions and testing suggestions are merely examples. Each language feature should be carefully considered independently against each IDE feature to find interesting intersections*

| Category | Integration | Signoff/Notes |
| --- | --- | --- |
| **Projection Buffers** | **Razor** (web)<br />- Verify expression and block contexts<br />- Test on projection boundaries<br /> - Emphasis on rename and formatting | |
| | **Venus**<br />- Verify expression and block contexts<br />- Test on projection boundaries<br /> - Emphasis on rename and formatting | |
| **Designers** | **WPF**<br />- Event generation from designer to code<br />- Designer consumption of new types of members<br />- Cross language features (Go to definition, find references, rename) | |
| | **WinForms**<br />- Create a project, add some controls to the form<br />- Verify that simple changes to InitializeComponent round-trip to the designer.<br />- Verify that double clicking a control generates or navigates to an event handler for its default event | |
| **Project System Interactions** | **Linked Files (all flavors)**<br />- Regular linked files<br />- Shared Projects<br />- Multitargeted .NET Core Apps | |

## Interaction with other new language features in the IDE
Verify IDE handling of the new language feature in conjunction with other new/unreleased language features

| Feature | C# Signoff/Notes | VB Signoff/Notes | F# Signoff/Notes |
| --- | --- | --- | --- |
| **\<New Language Feature 1\>** | | | |
| **\<New Language Feature 2\>** | | | |

## <a name="featuresuggestions">New Feature Suggestions</a>
What refactorings, fixes, code transformations, or other in-editor experiences would enhance this language feature?

| Feature Name | Description |
| --- | --- |
| ? | ? |
| ? | ? |
