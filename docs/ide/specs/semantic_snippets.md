# Semantic Snippets

@jmarolf | Status: Draft

Snippets as they exist today for C# developers only do text expansion (`cw -> Console.WriteLine`) and have lacked any significant improvements in the last 10+ years. We would like to iterate on this experience for .NET developers using Visual Studio. The hope is that typing (the thing developers do a lot) can also be the best way to discover how to and write code.

## Table of Contents

- [Requirements](#requirements)
  - [Goals](#goals)
  - [Non-Goals](#non-goals)
- [Scenarios and User Experience](#scenarios-and-user-experience)
  - [User Experience: Snippet commit behavior](#user-experience-snippet-commit-behavior)
  - [User Experience: Snippet behavior with existing options](#user-experience-snippet-behavior-with-existing-options)
  - [Scenario 1: Add New Snippets](#scenario-1-add-new-snippets)
    - [record snippet](#record-snippet)
    - [record struct snippet](#record-struct-snippet)
  - [Scenario 2: Snippets only show up where they are valid](#scenario-2-snippets-only-show-up-where-they-are-valid)
    - [Existing Snippet Valid Insertion Locations](#existing-snippet-valid-insertion-locations)
    - [New Snippet Valid Insertion Locations](#new-snippet-valid-insertion-locations)
  - [Scenario 3: Snippet text is relevant to my code](#scenario-3-snippet-text-is-relevant-to-my-code)
    - [`Console.WriteLine`](#consolewriteline)
    - [MessageBox.Show](#messageboxshow)
    - [Invoke an Event](#invoke-an-event)
  - [Scenario 3: Ability to populate snippets replacement parameters from context](#scenario-3-ability-to-populate-snippets-replacement-parameters-from-context)
    - [Boolean snippets](#boolean-snippets)
    - [Loop snippets](#loop-snippets)
  - [Scenario 4: Snippets are discoverable](#scenario-4-snippets-are-discoverable)
    - [Snippets Synonyms](#snippets-synonyms)
- [Design](#design)
  - [Snippet Completion Provider](#snippet-completion-provider)
  - [Custom Completion Sorting Logic](#custom-completion-sorting-logic)
  - [Complex Edits](#complex-edits)
  - [LSP Considerations](#lsp-considerations)
- [Test Plan](#test-plan)
  - [Ensuring we don't break muscle memory](#ensuring-we-dont-break-muscle-memory)
  - [Ensuring out new feautures work](#ensuring-out-new-feautures-work)
- [Q & A](#q--a)

## Requirements

### Goals

- Snippet functionality of more discoverable for all users
- Snippets are more targeted to what a user is trying to do
- We do not want existing users to have their flow interrupted

### Non-Goals

- We are not making a general-purpose extension API for snippets (yet)
  - We may do this someday but for now we want to ensure that we can meet customers’ needs for the narrow case
- We are not changing the VB experience for snippets (yet)
  - VB has a very different design than C#.
- We are not changing "Surround With" behavior
- we are not changing snippet menu behavior (Ctrl+K,X)

## Scenarios and User Experience

### User Experience: Snippet commit behavior

- We should change the commit behavior of these new snippets to only require a single tab to complete
- If the user types `Tab ↹` twice (due to muscle memory)  we will eat the second key press (similar to how automatic brace matching works)
- Two undo stacks will be created for each snippet that is commited (`Console.WriteLine()` -> `cw` -> ``)

### User Experience: Snippet behavior with existing options

There is also the snippet options under `Tools->Options->Text Editor->C# Intellisense`:

![image](https://user-images.githubusercontent.com/9797472/137983382-f243e241-fc4a-4529-be08-18e3ee7d5174.png)

"Always include snippets" is the default and this is what over 90% of user have set.

1. If "Always include snippets" is on
    - We need to preserve the muscle memory of users with this option while being allowed to change the visual presentation.
    - Even though “cw” won’t appear in the completion list with our feature turned on it still “needs to work” from the users perspective so the following experience needs to be preserved:
      - User types `cw`
      - Nothing is pre-selected in the completion list such that cw is over-written
      - Tab-Tab executed the console.writeline(); snippet
1. If "Never include snippets" is on
    - If the user has decided to never include snippets in the completion list then we also want to preserve this behavior
      - User types `cw`
      - The completion list pre-selects an item from the list
      - Tab commits this item from the list to the editor
1. If "Include snippets when ?-Tab is typed after an identifier" is on
    - Might want to consider removing this option as its rather odd and cannot find any data to suggests that people use this. If we decided to include this then the behavior is
      - No ?-Tab: behaves like 2 above
      - ?-Tab: behaves like 1 above

### Scenario 1: Add New Snippets

*We should offer new snippets for new language constructs.*

|Name |Description |
|-----|------------|
|`record` | Creates a record declaration. |
|`srecord` | Creates a record struct declaration. |

#### record snippet

```csharp
record MyRecord() // users cursor is placed inside the parenthesis
{
}
```

#### record struct snippet

```csharp
record struct MyRecord() // users cursor is placed inside the parenthesis
{
}
```

### Scenario 2: Snippets only show up where they are valid

*If a user types an existing snippet in a location where it is not valid (example `cw`) nothing will be shown and no snippet suggestions will be given to the user.*

Below is a list of snippets that the contexts in which they should appear

#### Existing Snippet Valid Insertion Locations

|Name | Valid locations to insert snippet|
|-----|----------------------------------|
|`#if` |  Anywhere.
|`#region` | Anywhere.
|`~` | Inside a class or record.
|`attribute` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`checked` | Inside a method, an indexer, a property accessor, or an event accessor.
|`class` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`ctor` | Inside a class or record.
|`cw` | Inside a method, an indexer, a property accessor, or an event accessor.
|`do` | Inside a method, an indexer, a property accessor, or an event accessor.
|`else` | Inside a method, an indexer, a property accessor, or an event accessor.
|`enum` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`equals` | Inside a class, or a struct.
|`exception` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`for` | Inside a method, an indexer, a property accessor, or an event accessor.
|`foreach` | Inside a method, an indexer, a property accessor, or an event accessor.
|`forr` | Inside a method, an indexer, a property accessor, or an event accessor.
|`if` | Inside a method, an indexer, a property accessor, or an event accessor.
|`indexer` | Inside a class, recordstruct record, or a struct.
|`interface` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`invoke` | Inside a method, an indexer, a property accessor, or an event accessor.
|`iterator` | Inside a class, recordstruct record, or a struct.
|`iterindex` | Inside a class, recordstruct record, or a struct.
|`lock` | Inside a method, an indexer, a property accessor, or an event accessor.
|`mbox` | (If inside a winforms project) Inside a method, an indexer, a property accessor, or an event accessor.
|`namespace` | Inside a namespace (including the global namespace).
|`prop` | Inside a class, record ,or a struct.
|`propfull` | Inside a class, record, struct record, or a struct.
|`propg` | Inside a class, record, struct record, or a struct.
|`sim` | Inside a class, record, struct record, or a struct.
|`struct` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`svm` | Inside a  class, record, struct record, or a struct.
|`switch` | Inside a method, an indexer, a property accessor, or an event accessor.
|`try` | Inside a method, an indexer, a property accessor, or an event accessor.
|`tryf` | Inside a method, an indexer, a property accessor, or an event accessor.
|`unchecked` | Inside a method, an indexer, a property accessor, or an event accessor.
|`unsafe` | Inside a method, an indexer, a property accessor, or an event accessor.
|`using` | Inside a namespace (including the global namespace).
|`while` | Inside a method, an indexer, a property accessor, or an event accessor.

#### New Snippet Valid Insertion Locations

|Name | Valid locations to insert snippet|
|-----|----------------------------------|
|`record` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`srecord` | Inside a namespace (including the global namespace), a class, record, struct record, or a struct.
|`elseif` | Inside a method, an indexer, a property accessor, or an event accessor.

### Scenario 3: Snippet text is relevant to my code

When possible, we want snippets to offer more fine-grained help to users writing code. There are two snippets where we want to change the code we complete dependeing on context.

#### `Console.WriteLine`

Today `cw` always expands to the text below:

```csharp
Console.WriteLine(); // Users cursor is placed inside the parenthesis
```

This should remain the same for normal methods.

```csharp
void M()
{
    Console.WriteLine(); // cursor is placed inside the parenthesis
}
```

however inside an `async` method we should complete to this instead

```csharp
async Task M()
{
    await Console.Out.WriteLineAsync(); // cursor is placed inside the parenthesis
}
```

#### MessageBox.Show

`mbox` always expands to the following text

```csharp
System.Windows.Forms.MessageBox.Show("Test");
```

This is a perfectly valid snippet but we only want it to show under the following circumstances:

1. The user is in a .NET Framework project
1. The user is int a .NET Core project with `<UseWindowsForms>true</UseWindowsForms>` set

#### Invoke an Event

This snippet (`invoke`) was added to help with invoking events before the introduction of the `?.` operator. It always generates the following text:

```csharp
[|EventHandler|] temp = [|MyEvent|]; // two tab stop groups denoted with [||]
                                     //  1. first the user fills in the event type
                                     //  2. then the user gives the identifier name of the event
if (temp != null)
{
    temp();
}
```

If the user it targeting a version that is older than C# 6 (where `?.` was introduced) we should still generate something similar to what the original snippet did but with better help.

```csharp
event System.Action MyEvent;

void CallMyEvent()
{
    Action? temp = [|MyEvent|]; // Just one tab stop group where the user fills in the identifier of the event
                                // NOTE: we should also show a target-typed completion list here to help fill in the event
                                // Once the user types `Tab` to complete filling in the event identifier we will update the 
                                // type of `temp` (if the user has non-var code style preferences).
    if (temp != null)
    {
        temp([||]); // The second tab group is now here since most events require arguments, whole-method completion should be turned on for this line
    }
}
```

If the user is doing this in a version of C# that is newer than version 5 things are significantly simpler. We will still offer a target-typed completion for events that are in scopt but we just generate this code:

```csharp
event System.Action MyEvent;

void CallMyEvent()
{
    MyEvent?.Invoke([||]); // The user was offered a list of event types in scope and upon selection the whole line was re-writted to this.
                           // As above, whole-line completion helps fill in the argument list here.
}
```

If the user selects an event that returns an awaitable type things get more complex.

```csharp
event System.Func<Task> MyEvent;

void CallMyEvent()
{
    Func<Task>? myEvent = [|MyEvent|]; // user chose an event that returns an awaitable type
    if (myEvent != null)
    {
        Task eventTasks = Task.WhenAll(Array.ConvertAll( // we generate the code to ensure all the delegate results are awaited
            myEvent.GetInvocationList(),
            e => ((Func<Task>)e).Invoke([||]))); // As above, whole-line completion helps fill in the argument list here.
        [|eventTasks.GetAwaiter().GetResult();|] // since the method is not async we offer a third group with the options
                                                 // 1. make method async
                                                 // 2. wait for all delegates to finish
    }
}
```

If the user is inside of an `async` method _and_ they select an event that returns an awaitable we elide the third tab group.

```csharp
event System.Func<Task> MyEvent;

async Task CallMyEventAsync()
{
    Func<Task>? myEvent = [|MyEvent|]; // user chose an event that returns an awaitable type
    if (myEvent != null)
    {
        await Task.WhenAll(Array.ConvertAll(
            myEvent.GetInvocationList(),
            e => ((Func<Task>)e).Invoke([||]))); // As above, whole-line completion helps fill in the argument list here.
    }
}
```

### Scenario 3: Ability to populate snippets replacement parameters from context

Note that is a snippet isn't called out here the expectation is that its behavior remanes the same. Meaning snippets like `tryf` expand to the same text.

#### Boolean snippets

- `do`
- `if`
- `while`
- `else`
- `elseif`

These snippets create a block and have a place to but a boolean expression. The only additional work we want to do here is ensure the users are shown a target-typed list of members that are in scope

Note that `else if` isn't a snippet today and in the past was handled by the user typing `else` Tab `if` Tab-Tab. Since we are changine this behavior a bit we should add an `elseif` snippet that completes the following

```csharp
else if ([|true|]) // user is shown a target-typed list for bool members
{

}
```

#### Loop snippets

- `for`
- `forr`
- `foreach`

this is what `for` produces today:

```csharp
for (int [|i|] = 0; i < [|length|]; i++) // user need to iterate through naming `i` then selecting what `length` should be
{

}
```

We can improve the process for filling in for loops here

```csharp
for (int [|i|] = 0; i < [|length|]; i++) // we automatically choose a name for `i` so it does not conflict, 
                                     // show a list of in-scope variabled that have a Count or Length property and are indexable
{
    T [|temp|] = arrag[i]; // Once the uder commits the indexable member we generate a temp and let them name it.
                           // if the user is doing something that does not require reading from the indexable member esc undoes this
}
```

Today users are asked to fill in information on a foreach loop in the exact opposite order of what it should be:

```csharp
foreach ([|var|] [|item|] in [|collection|]) // user needs to select `var` then `item` then `collection`
{

}
```

For foreach loops we will reverse the tab group ordering asking them to select the type the member they are iterating over first

```csharp
foreach (var [|item|] in [|collection|]) // user selects`collection` first from list of IEnumerable variables then everything else is filled in based on preferenced.
{                                        // the user is allowed to override the chosen name by tabbing
                                         // If the user preferes var then var is used for the variable type otherwise it is replaced with concrete type from the enumerable
}
```

By changing this order it also allows us to automatically deconstruct elements we are iterating over.

```csharp
List<(int id, string name)> _students = new();

foreach ((int [|id|], string [|name|]) in [|_students|}) // user chose a type that can be decontructed so we do that automatically
{                                                        // user can tab though the deconstructed names and change them if they want

}
```

### Scenario 4: Snippets are discoverable

Just typing what the user is trying to do snippets should show up and be obvious what they do. What also want to handle synonmyms. Below is a list of synonyms that should also filter to the snippet in the completion list. Note that the old trigger text for these snippets is included to ensure that typing the same keystrokes produces the same results.

#### Snippets Synonyms

|Name |Descriptive Text in Completion List | Valid synonyms |
|-----|------------|----------------------------------|
|`#if`  | Create a #if directive and a #endif directive. | directive, pound, hashtag, #if
|`#region`  | Create a #region directive and a #endregion directive. | region, pound, hashtag, #region
|`~`  | Create a finalizer (destructor) for the containing class. | destruct, final, dispose, ~
|`attribute`  |  Create a declaration for a class that derives from Attribute. |
|`checked`  | Create a checked block. |
|`class`  | Create a class declaration. | type, new
|`ctor`  | Create a constructor for the containing class. | new
|`cw`  | Write to the console. | write line, output, log, print, cw
|`do`  | Create a do while loop. |
|`else`  | Create an else block. |
|`enum`  | Create an enum declaration. | new
|`equals`  | Create a method declaration that overrides the Equals method defined in the Object class. | compare, equal, equals
|`exception`  | Create a declaration for a class that derives from an exception (Exception by default). |
|`for`  | Create a for loop. |
|`foreach`  | Create a foreach loop. |
|`forr`  |  Create a for loop that decrements the loop variable after each iteration.| reverse, forr
|`if`  | Create an if block. |
|`indexer`  | Create an indexer declaration. |
|`interface`  | Create an interface declaration. |
|`invoke`  | Create a block that safely invokes an event. | event, invoke
|`iterator`  | Create an iterator. |
|`iterindex`  | Create a "named" iterator and indexer pair by using a nested class.  | iterindex
|`lock`  | Create a lock block. |
|`mbox`  | Call System.Windows.Forms.MessageBox.Show. | popup, show, mbox
|`namespace`  | Create a namespace declaration. |
|`prop`  | Create an auto-implemented property declaration. | auto, prop
|`propfull`  | Create a property declaration with get and set accessors. | full, propfull
|`propg`  | Create a read-only auto-implemented property with a private set accessor. | propg
|`sim`  | Create a static int Main method declaration | sim, entry, start
|`struct`  | Create a struct declaration. |
|`svm`  |  Create a static void Main method declaration. | svm, entry, start
|`switch`  | Create a switch block. |
|`try`  | Create a try-catch block. | catch, handle
|`tryf`  | Create a try-finally block. | finally, tryf
|`unchecked`  | Create an unchecked block |
|`unsafe`  | Create an unsafe block. |
|`using`  | Create a using directive. | dispose
|`while`  | Create a while loop. |
|`record`  | Create a record declaration.  | data, new
|`srecord`  | Create a struct record declaration. | data, new

## Design

Essentially all of these new snippets can just be completion providers as the apis we have today should be enought to accmplish everything that is detailed here. There are a few adjusts that will need to be made to exising services to ensure these all fit in cleanly.

### Snippet Completion Provider

Work will need to be done [here](https://github.com/dotnet/roslyn/blob/1667bc61ac41e5e63ea672c2a39c1a0bebe5f8e9/src/Features/CSharp/Portable/Completion/CompletionProviders/SnippetCompletionProvider.cs#L146) and potentially [here](https://github.com/dotnet/roslyn/blob/main/src/VisualStudio/Core/Def/Implementation/Snippets/AbstractSnippetInfoService.cs) to filter out _only_ the snippets that out team owns so we do not break third-party snippets.

### Custom Completion Sorting Logic

We may need to adjust the item sorting logic [here](https://github.com/dotnet/roslyn/blob/1667bc61ac41e5e63ea672c2a39c1a0bebe5f8e9/src/Features/Core/Portable/Completion/CompletionService.cs#L202) considering the synonym matching that we want to do.

### Complex Edits

Serveral of these complation behaviors will be complex edits to the docuement. Based on the existing API shape this should not be a problem but we may want to ensure that snippets (that the user generally thinks of a lightweight operation) do not slow things down by doing _overly_ complex things.

### LSP Considerations

The expectation is that LSP clients should be able to use these completion providers without issue but we should keep in contact with those teams to ensure this remains the case.

## Test Plan

### Ensuring we don't break muscle memory

- Type in the following locations and ensure the same text is committed given the same keystroked
  - inside namespace
    - `cla⇥⇥Hi` -> `class Hi { }`
    - `us⇥<space>Sys⇥;` -> `using System;`
  - inside class
    - `svm⇥⇥` -> `static void Main(string[] args) { }`
    - `prop⇥⇥` -> `public int MyProperty { get; set; }`
    - `over⇥<space>↓⇥` -> `public override bool Equals(object? obj) { return base.Equals(obj); }`
    - `ev⇥<space>Act⇥?<space>MyEvent;` -> `event Action MyEvent;`
  - inside method body,  lambda, property, or local function
    - `if⇥⇥` -> `if (true) { }`
    - `forr⇥⇥` -> `for (int i = length - 1; i >= 0; i--) { }`
    - `str⇥<space>name = "Some Name<end>;` -> `string name = "Some Name";`
  - inside parameter list
    - `str⇥<space>name = "Some Name` -> `string name = "Some Name"`

## Q & A

- [C# snippet Docs](https://docs.microsoft.com/visualstudio/ide/visual-csharp-code-snippets)
- [Visual Studio Snippet XML Schema](https://docs.microsoft.com//visualstudio/ide/code-snippets-schema-reference)
