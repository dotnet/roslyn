# Recently added productivity features and links to release notes

## TOC
 * [Dev16](#prod-16)
   * [Dev 16.3](#prod-16-3)
   * [Dev 16.2](#prod-16-2)
   * [Dev 16.1](#prod-16-1)
   * [Dev16.0](#prod-16-0)
 * [Dev15](#prod-15)
   * [Dev 15.9](#prod-15-9)
   * [Dev 15.8](#prod-15-8)
   * [Dev 15.7](#prod-15-7)
   * [Dev 15.6](#prod-15-6)
   * [Dev 15.5](#prod-15-5)
   * [Dev 15.4](#prod-15-4)

## <a id="prod-16"></a> dev16

### <a id="prod-16-3"></a> 16.3 Preview ([release notes](https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-preview) )
* Wrap chains of fluent calls with a refactoring
* Introduce a local variable immediately after writing its initializer
* Code Analysis page now in project properties. Right click on the project name within the solution explorer and select properties. Select Code Analysis to install analyzer packages and configure when to run code analysis.
* Now, for users who turn off the completion for unimported types, it's much easier to get it back in the completion list with the new imported type filter added to the IntelliSense toggles.
* There is now Quick Info style support for XML comments. Place the cursor over the method name. Quick Info will then display the supported styles from the XML comments above the code.
* Rename a file when renaming an interface, enum, or class. Place the cursor in the class name and type (Ctrl + R,R) to open the Rename dialogue and check the ‘Rename file’ box.
* There is now Edit and Continue support for multi-targeted projects which includes modules loaded multiple times in the same process on different domains or load contexts. In addition, developers can edit source files even when the containing project is not loaded or the application is running.

### <a id="prod-16-2"></a> 16.2 ([release notes](https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-v16.2) )
* Bring back Sort Usings as a separate command from Remove Usings. Available under Edit > IntelliSense.
* Convert a switch statement to a [switch expression](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-8#switch-expressions) (verify you are using C# 8 to get the switch expression feature)
* Generate a parameter 
* [Test Explorer improvements](https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes#test-explorer)
  * Significant reduction in memory consumed by the Visual Studio process and faster test discovery for solutions with large numbers of tests
  * Filter buttoms for passed, failed, not run tests
  * Additional buttons for the 'Run Failed Tests' and 'Run Previous Test Run'
  * Customize what columns are displayed in the Test Explorer
  * Specify what is displayed in each tier of the test hierarchy. The default tiers are Project, Namespace, and then Class, but additional options include Outcome or Duration groupings.
  * The test status window (the pane below the test list that displays the messages, output, etc.) is much more usable. Users can copy substrings of text, and the font-width is fixed for more readable output.
  * Playlists can be displayed in multiple tabs and are much easier to create and discard as needed.
  * Live Unit Testing now has its own view in the Test Explorer. It displays all tests currently included in Live Unit Testing (aka. the live test set), so testers can easily keep track of Live Unit Testing results separate from the manually run test results.
  * There is a target framework column that can display multi-targeted test results.

### <a id="prod-16-1"></a> 16.1 ([release notes](https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-v16.1))
* There is now experimental intellisense completion for unimported types! You now receive intellisense suggestions for types in dependencies in your project even if you have not yet added the import statement to your file. You must turn this option on in Tools > Options > Text Editor > C# > Intellisense.
* Toggle Single Line Comment/Uncomment is now available through the keyboard shortcut (Ctrl+K,/). This command will add or remove a single line comment depending on whether your selection is already commented.
* You can now export naming styles with the “Generate editorconfig” button located in Tools > Options > Text Editor > C# > Code Style.
* You can now use a new editorconfig code style rule to require or prevent usings inside a namespace. This setting will also be exported when you use the “Generate editorconfig” button located in Tools > Options > Text Editor > C# > Code Style.
* The Find All References “Kind” column now has more filter options and is aware of namespaces and types.
* .NET codefixes and Refactorings
  * Split/Merge if
  * Wrap binary expressions
  * Unseal a class
* A regex completion list can now be accessed through the intellisense menu (Ctrl + space) when inside a regex string. These completions also include an in-line description of what the suggestion does.
* You can now use one-click code cleanup for projects and solutions. You can right-click on projects or the solution in the Solution Explorer and select ‘Run Code Cleanup’.
* You can now use a refactoring dialog to move type to namespace or folder. Place your cursor in the class name and type (Ctrl + .) to open the quick actions and refactorings menu and select ‘Move to namespace.’ This launches a dialog where you can select the target namespace you would like to move the type to.
* Toggle Block Comment/Uncomment (Ctrl+Shift+/) or through Edit > Advanced > Toggle Block Comment.
* There is now a codefix for making readonly struct fields writable. Place your cursor in the struct name, type (Ctrl+.) to open the quick actions and refactorings menu, and select ‘Make readonly fields writable.’
* The codefix to add a private field from a constructor and vice versa is easier to discover and will show when any portion of the field name is selected. This refactoring now also offers all possible constructors.

### <a id="prod-16-0"></a> 16.0 ([release notes](https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-v16.0))

* .NET refactorings and codefixes:
  * Sync Namespace and Folder Name
  * Pull members up refactoring with dialog options
  * Wrap/indent/align lists of parameters/arguments
  * Convert anonymous type to tuple
  * Use expression/block body for lambda
  * Invert conditional expressions and logical operations
  * Convert to compound assignment
  * Automatically close block comment on “/”
  * Convert to compound assignment
  * Fix Implicitly-typed variables cannot be constant
  * Auto-fixer to replace @$" with $@" when typing interpolated verbatim string
  * Completion for #nullable enable|disable
  * Fix for unused expression values and parameters
  * Fix for allowing Extract Interface to remain in the same file
   * For cases where "await" is implied but omitted, there is now a compiler warning.
   * Convert a local function to a method.
   * Convert a tuple to a named-struct.
   * Convert an anonymous type to a class.
   * Convert an anonymous type to a tuple.
   * For a foreach loop to LINQ query or to LINQ method.
   * Generate deconstruct method
* Categorize references by Read/Write in Find All References window.
* Add Editorconfig when_multiline option for csharp_prefer_braces.
* New classification colors are available from the .NET Compiler Platform SDK (aka Roslyn). New default colors, similar to the Visual Studio Code colors, are gradually being rolled out. You can adjust these colors in Tools > Options > Environment > Fonts and Colors or turn them off in Environment > Preview Features by unchecking the Use enhanced colors check box. We’d appreciate hearing feedback on how this change affects your workflow.
* Configure [code cleanup](https://docs.microsoft.com/en-us/visualstudio/ide/code-styles-and-code-cleanup) (settings formally a part of Format Document)
* [Format document global tool](https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes#-apply-code-style-preferences) can be run from the command-line
 * Use [code metrics](https://docs.microsoft.com/visualstudio/code-quality/code-metrics-values) with .NET Core projects with our added compatibility.
 * Export editor settings to an Editorconfig file through **Tools > Options > Text Editor > C# > Code Style** with the button "Generate .editorconfig file from settings".
 * Use C# and Visual Basic's new Regex parser support. Regular expressions are now recognized, and language features are enabled on them. Regex strings are either recognized when a string is passed to the Regex constructor or when a string is immediately preceded with a comment containing the string language=regex. The language features included in this release are classification, brace matching, highlight references, and diagnostics.
 * You can now use dead code analysis for unused private members with an optional code fix to remove unused member declaration.
 * The Find References feature on an accessor now only returns results for that accessor.
 * "Using" statements can be added when code is pasted into a file. A code fix appears after pasting recognized code that prompts you to add relevant missing imports.
 * You can now use Find All References (Shift-F12) and CodeLens to show results from Razor (.cshtml) files in .NET Core projects. You can then navigate to the identified code in the relevant Razor files.
 * You will now receive a warning when running code analysis using FxCop. .NET Compiler analyzers are the recommended way to perform code analysis going forward. Read more on migrating to .NET compiler platform analyzers.

## <a id="prod-15"></a> dev15

### dev15 Summary
* Visual Studio-based code cleanup via Format Document 
* Refactorings: InvertIf, add parameter from method callsite, remove unnecessary parentheses, convert if-else assignment, completion for attributes and return to ternary conditional.  
* .editorconfig “refresh” such that changes will be automatically applied without file reload 
* IOperation APIs RTW 
* Portable PDB format to enable cross-platform debugging scenarios 

### <a id="prod-15-9"></a>15.9
None

### <a id="prod-15-8"></a>15.8 ([release notes](https://docs.microsoft.com/en-us/visualstudio/releasenotes/vs2017-relnotes-v15.8#productivity))
 * Format Document (Ctrl + K, D or Ctrl + E, D) for C# development. 
 * .NET refactorings and codefixes:
   * Invert If 
   * Add parameter from method callsite
   * Remove unnecessary parentheses
   * Use ternary conditionals in assignments and return statements
 * Go to Enclosing Block (Ctrl + Alt + UpArrow)
 * Go to Next/Previous Issue (Alt + PgUp/PgDn) skip to error, squiggle, lightbulb.
 * Go to Member (Ctrl + T, M) is now scoped to the file by default
 * Multi-caret: Insert carets with Ctrl + Alt + LeftMouseClick.
 * Multi-caret: Add a selection and caret at next location that matches current selection with Shift + Alt + Ins.
 * Access a contextual navigation menu with Alt + `.
 * Visual Studio Code and ReSharper (Visual Studio) keybindings

### <a id="prod-15-7"></a>15.7 ([release notes](https://docs.microsoft.com/en-us/visualstudio/releasenotes/vs2017-relnotes-v15.7#dotnet_productivity))
* .NET refactorings and codefixes:
   * For-to-foreach, and vice versa.
   * Make private fields readonly.
   * Toggle between var and the explicit type, regardless of your code style preferences.
 * Go To Definition (F12) is now supported for LINQ query clauses and deconstructions.
 * Quick Info shows captures on lambdas and local functions, so you can see what variables are in scope.
 * Change Signature refactoring (Ctrl+. on signature) works on local functions.
 * You can edit .NET Core project files in-place, so opening containing folder, restoring tabs, and other Editor features are fully supported. IDE changes, such as adding a linked file, will be merged with unsaved changes in the editor.

### <a id="prod-15-6"></a>15.6 ([release notes](https://docs.microsoft.com/en-us/visualstudio/releasenotes/vs2017-relnotes-v15.6#productivity))
 * Navigate to decompiled sources (must enable in Tools > Options > Text Editor > C# > Advanced > Enable navigation to decompiled sources)
 * .NET EditorConfig options: dotnet_prefer_inferred_tuple_names
 * .NET EditorConfig options: dotnet_prefer_inferred_anonymous_type_member_names
 * Ctrl + D to Duplicate text
 * Expand Selection command that allows you to successively expand your selection to the next logical block. You can use the shortcuts Shift+Alt+= to expand and Shift+Alt+- to contract the current selection.

### <a id="prod-15-5"></a>15.5
None

### <a id="prod-15-4"></a>15.4 ([release notes](https://docs.microsoft.com/en-us/visualstudio/releasenotes/vs2017-relnotes-v15.4#editor))
 * Editor: Control Click Go To Definition
