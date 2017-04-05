
## Test plan
- Read the **feature specification** and related conversations
- Think about how the new language feature **interacts with each IDE feature**
    - Are new keywords visualized appropriately?
    - Do refactorings and fixes produce correct code utilizing the new language feature?
    - How do IDE error recovery scenarios work?
    - Etc.
- **Brainstorm ideas** for new features to enhance the language feature
    - Should existing code be refactored to some new pattern?
    - Should users be able to easily switch between two equivalent forms?
    - Do we need to make the flow of this feature easier to visualize?
    - Etc.
- Open questions
    - IDE test coverage -- how much / which parts / added by whom?

### Test Outputs
- [**Merge Signoff**](#mergesignoff): Can the language feature be merged, with all *Internal Dogfooding Requirements* met and all *IDE Features* free of crashes and asserts?
- [**Feature Signoff**](#featuresignoff): Per-feature signoff with links to non-blocking issues.
- [**Feature Suggestions**](#featuresuggestions): What can we add to the IDE to make the new language feature delightful to use?

### Testing tags

:white_check_mark: = Tested & Approved (possibly with linked bugs)
:x: = Merge blocking
:construction: = Non-blocking bugs
:ok: = Issue has been fixed
:free: = No expected impact
:question: = Open question
:grey_question: = Answered question

<a name="testfootnote1"><sup>1</sup></a> Feature updated by compiler team, with appropriate unit tests.
<a name="testfootnote2"><sup>2</sup></a> Feature requires more complete testing by IDE team. (?)

## <a name="mergesignoff">Internal Dogfooding Requirements (Merge Signoff)</a>
Critical IDE features in the areas of **Typing**, **Navigating**, and **Viewing** must function as expected. Any non-trivial issues in these areas blocking language feature merging.
*All feature descriptions and testing suggestions are merely examples. Each language feature should be carefully considered independently against each IDE feature to find interesting intersections*

| Category | Feature/Description | C# Signoff/Notes | VB Signoff/Notes |
| --- | --- | --- | --- |
| **Enable/Disable** | **Feature Flag** <br />Completely enables/disables the feature in the compiler & IDE | | |
| **Typing** | **General Typing**<br />- Type and paste new constructs<br />- Nothing interferes with verbatim typing | |
| | **Completion**<br />- Typing new keyword/construct names<br />- Dotting off of new constructs | | |
| | **Formatting** <br />- Spacing in and around new constructs<br />- Spacing options<br />- Format Document command | | |
| | **Automatic Brace Completion** (*C# only*) <br />- Auto-insert close brace<br />- Shift+Enter commit of pending brace completion | | |
| | **Indentation** <br />- Typing `Enter` in an unfinished statement indents the next line | | |
| **Navigating** | **Go To Definition** <br />- F12 from callsites to definition | | |
| | **Go To Implementation** <br />- Jump from virtual members to their implementations<br />- Jump from inheritable types to their implementations | | |
| | **Find All References** <br />- Lists references to a symbol in "Find Symbol Results" window<br />- Shows results in hierarchy grouped by definition | | |
| **Viewing** | **Colorization** <br />- Keywords, literals, and identifiers colored appropriately in code | | |
| | **Error Squiggles** <br />- Squiggles appear as expected on reasonable spans | | |

##  <a name="featuresignoff">IDE Features (Feature Signoff)</a>
For the remaining set of IDE features, only crashes and asserts are considered blocking. However, *all* issues discovered must be filed and linked here.
*All feature descriptions and testing suggestions are merely examples. Each language feature should be carefully considered independently against each IDE feature to find interesting intersections*

### Code Transformations
| Category | Feature/Description | C# Signoff/Notes | VB Signoff/Notes |
| --- | --- | --- | --- |
| **Refactoring with UI** | **Inline Rename (with UI)**<br />- Dashboard shows correct information<br />- Highlighted spans are updated appropriately<br />- Rename operation updates the correct set of symbols | | |
| | **Change Signature (with UI)**<br />- Updates all direct & cascaded definitions/callsites<br />- Shows appropriate signature & parameter previews in UI | | |
| | **Extract Interface (with UI)**<br />- Generated Interface has expected shape<br />- UI shows appropriate method previews | | |
| | **Generate Type (with UI)**<br />- Dialog gives all valid options<br /> | | |
| **Refactorings** | **Rename Tracking**<br />- Tracking span tracks & dismisses as expected | | |
| | **Extract Method**<br />- Extracted method has the expected signature<br />- All arguments/return values handled correctly<br />- Extracted code block is reasonable | | |
| | **Introduce Variable**<br />- Introduced variable has the expected signature and initializer expression<br />- "Introduce for All" correctly finds dupes | | |
| | **Inline Temporary Variable**<br />- Inlined values are appropriately expanded/reduced | | |
| | **Organize Usings**<br />- Honors "Place 'System' namespace first" option | | |
| | **Convert "Get*" Methods to Properties**<br />*Add Description* | | |
| | **Encapsulate Field**<br />*Add Description* | | |
| **Fixes** | **Add Using**<br />- Triggers on appropriate constructs | | |
| | **Generate Local**<br />*Add Description* | | |
| | **Generate Field**<br />*Add Description* | | |
| | **Generate Method/Constructor**<br />- Generated method has the expected signature and accessibility | | |
| | **Generate Constructor from members**<br />- Select fields/properties to generate a constructor accepting corresponding arguments<br />- Generated constructor has the expected signature and accessibility | | |
| | **Implement Interface**<br />- Only missing methods added<br />- All added methods have the expected signature and accessibility | | |
| | **Implement IDisposable**<br />*Add Description* | | |
| | **Implement Abstract Class**<br />*Add Description* | | |
| **Code Gen** | **Snippets**<br />*Add Description* | | |
| | **Event Hookup on Tab** (*C# only*)<br />- Type "+=" after an event name and QuickInfo shows<br />- Invoking should pick good name & launch Inline Rename | | N/A |
| | **End Construct Generation** (*VB only*)<br />*Add Description* | N/A | |
| | **Automatic End Construct Update** (*VB only*)<br />*Add Description* | N/A | |

### IDE Features
| Feature/Description | C# Signoff/Notes | VB Signoff/Notes |
| --- | --- | --- |
| **Signature Help**<br />- Overloads shown with appropriate, colorized signature | | |
| **Quick Info**<br />- Hover on identifiers<br />- On completion list items | | |
| **Outlining**<br />*Add Description* | | |
| **Brace Matching** (*C# only*)<br />*Add Description* | | N/A |
| **Highlight References**<br />*Add Description* | | |
| **Peek**<br />*Add Description* | | |
| **Navigation Bars**<br />*Add Description* | | |
| **Metadata As Source**<br />*Add Description* | | |
| **Navigate To**<br />*Add Description* | | |
| **Go to Next/Previous Method**<br />*Add Description* | | |
| **Solution Explorer Pivots**<br />*Add Description* | | |
| **Call Hierarchy**<br />*Add Description* | | |
| **Code Lens**<br />*Add Description* | | |
| **Project System**<br />*Add Description* | | |
| **Debugger IntelliSense**<br />*Add Description* | | |
| **Breakpoint Spans**<br />*Add Description* | | |
| **Code Model / Class Designer**<br />*Add Description* | | |
| **Object Browser / Class View**<br />*Add Description* | | |
| **Lightbulb**<br />*Add Description* | | |
| **Line Separators**<br />*Add Description* | | |
| **Options**<br />*Add Description* | | |

### Project System & External Integration
(Any changes to the runtime will require much more testing in this category)
*All feature descriptions and testing suggestions are merely examples. Each language feature should be carefully considered independently against each IDE feature to find interesting intersections*

| Category | Integration | Signoff/Notes |
| --- | --- | --- |
| **Projection Buffers** | **Razor**<br />- Verify expression and block contexts<br />- Test on projection boundaries<br /> - Emphasis on rename and formatting | |
| | **Venus**<br />- Verify expression and block contexts<br />- Test on projection boundaries<br /> - Emphasis on rename and formatting | |
| **Designers** | **WPF**<br />- Event generation from designer to code<br />- Designer consumption of new types of members<br />- Cross language features (GTD, FAR) | |
| | **WinForms**<br />*Add Description* | |
| **Project System Interactions** | **.NET Core "Console Application (Package)"**<br />*Add Description* | |
| | **Linked Files (all flavors)**<br />*Add Description* | |

## Interaction with other new language features
Verify IDE handling of the new language feature in conjunction with other new/unreleased language features

| Feature | C# Signoff/Notes | VB Signoff/Notes |
| --- | --- | --- |
| **\<New Language Feature 1\>** | | |
| **\<New Language Feature 2\>** | | |

## <a name="featuresuggestions">New Feature Suggestions</a>
What refactorings, fixes, code transformations, or other in-editor experiences would enhance this language feature?

| Feature Name | Description |
| --- | --- |
| ? | ? |