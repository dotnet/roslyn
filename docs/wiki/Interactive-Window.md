The C# Interactive Window provides a fast and iterative way to learn APIs, experiment with code snippets, and test methods by giving immediate feedback on what an expression will return or what an API call does. 

The C# Interactive Window is a [read-eval-print-loop (REPL)](https://en.wikipedia.org/wiki/Read%E2%80%93eval%E2%80%93print_loop) with advanced editor support. It supports features like IntelliSense as well as the ability to redefine functions & classes. After entering a code snippet--which can contain class and function definitions at top-level along with statements--the code executes directly. This means you no longer need to open a project, define a namespace, define a ```Main``` method, add a ```Console.WriteLine()``` call to output your result, and add a ```Console.ReadLine()``` call in order to play with code. In other words, say goodbye to ConsoleApp137 or whatever ridiculously high number your Console Apps default to today!

> **Note**: The Interactive Window does not currently support any form of debugging.

- [Getting Started](#start)
- [Basic Features](#basic)
- [Command-line REPL](#repl)
- [Code Samples](#samples)

<hr/>

## <a name="start"></a>Getting Started
You must have [Visual Studio 2015 Update 1 CTP](http://go.microsoft.com/fwlink/?LinkId=517106) or [Visual Studio 2015 Update 1 RC](https://www.visualstudio.com/news/vs2015-update1-vs) installed to access the Interactive Window. Once you have one of these versions installed, navigate to ```View > Other Windows > C# Interactive```. This will bring up the Interactive Window. 

Type any valid C# expression or statement and press ```Enter``` to evaluate. To learn more about what you can do in the Interactive Window, check out the [Basic Features](#basic) or [Code Samples](#samples).

You can also play with C# in the [command-line REPL](#repl).

<hr/>

## <a name="basic"></a>Basic Features
- [Language Features](#language)
- [Interactive Features](#interactive)
- [Editor Features](#editor)
- [Directives](#directives)

<hr/>

### <a name="language"></a>Language Features
The Interactive Window supports C# 6.0. Because the Interactive Window is for experimentation, there are some differences from regular C#.

#### ```async``` Context
The Interactive Window has default async context, meaning you can await expressions at the top level. Despite the async context, the Interactive Window will execute synchronous code normally.

```csharp
> using System.Threading.Tasks;
> await Task.Delay(100)
void
```
	
#### ```public``` Context
All top level variables and methods are public by default.
	
#### Better Display for Enumerables
In the Immediate Window, printing out enumerables takes up a lot of space. We have improved this experience with the following design:

```csharp
> List<int> myList = new List<int> { 3, 2, 7, 4, 9, 0 };
> mylist.Where(x => x % 2 == 0)
Enumerable.WhereListIterator<int> { 2, 4, 0 }
```

For reference, the old design was:
```csharp
myList
{System.Linq.Enumerable.WhereListIterator<int>}
    [0]: 2
    [1]: 4
    [2]: 0

```

<hr/>

### <a name="interactive"></a>Interactive Features
To be more productive in the Interactive Window, check out the following keyboard shortcuts and features.

*In this section*:
- [Evaluate Code Snippets](#evaluate)
- [Navigating History](#history)
- [Navigating History by Prefix](#prefix)
- [Navigating the Editor Window](#navigate)
- [Copy/Paste](#copy)
- [Multi-line Support](#multiline)
- [Selecting](#select)
- [Clear Submission](#clearsub)

#### <a name="evaluate"></a>Evaluate Code Snippets
After typing a valid expression or statement, press ```Enter``` to evaluate the code snippet. The output will be displayed below your submission and a new prompt will appear for the next submission. 

Statements must end in a semi-colon or brace; expressions do not need an end-of-line token. If you do not want the output of an expression to display, append a semi-colon to the end of your expression.

#### <a name="history"></a>Navigating History
Navigate History lets you quickly cycle through your previous submissions in the Interactive Window for editing or resubmission.

Press ```Alt+UpArrow``` to replace the current submission with a previous submission. Use ```Alt+DownArrow``` cycle backwards through previous submission navigation. 

#### <a name="prefix"></a>Navigating History by Prefix
Navigate History by Prefix lets you type a prefix and quickly cycle through your previous submissions in the Interactive Window that contain that prefix.

Press ```Ctrl+Alt+UpArrow``` to replace the current submission with a previous submission that contains the prefix. Use ```Ctrl+Alt+DownArrow``` cycle backwards through previous submission navigation. 

#### <a name="navigate"></a>Navigating the Editor
The Interactive Window imitates the Visual Studio Editor in most aspects. To navigate the editor buffer in the Window, use any of the arrow keys. 

#### <a name="copy"></a>Copy/Paste
Currently, everything you select and copy in the Interactive Window will be pasted (including carets and other symbols). RTF-paste is supported and will maintain the editor colorization. This is useful for sharing Interactive sessions over email or in Word/Powerpoint. If you paste back into the Interactive session, the carets and other symbols will automatically be stripped.  

Use ```Ctrl+C``` and ```Ctrl+V``` to copy snippets from the Interactive Window and paste to another program. See [Selecting](#select) for related tips.

#### <a name="multiline"></a>Multi-line Support
The Interactive Window supports multi-line submissions. Any incomplete statement, declaration, or expression will be treated as part of a multi-line submission. To force an expression to be evaluated, press ```Ctrl+Enter``` and to force a new-line, without completing the submission, press ```Shift-Enter```. 

You can also press ```Ctrl+Enter``` within a previous submission to append the previous submission to the current submission.

#### <a name="select"></a>Selecting
To select the current submission, press ```Ctrl+A``` once. To select the entire Interactive session, press ```Ctrl+A``` again (for a total of two times). 

#### <a name="clearsub"></a>Clear Submission
To clear the current submission, press ```Esc```. To undo this clear, press ```Ctrl+Z```. 

<hr/>

### <a name="editor"></a>Editor Features
The Interactive Window should provide the same support you get in the Visual Studio Editor wherever cross-over makes sense. 

*In this section*:
- [IntelliSense](#intellisense)
- [Find & Replace](#find)

#### <a name="intellisense"></a>IntelliSense
[IntelliSense](https://msdn.microsoft.com/en-us/library/hcw1s69b.aspx) is the general term for a number of features that help you to learn more about the code you are using, keep track of the parameters you are typing, and add calls to properties and methods with only a few keystrokes.
 - To trigger IntelliSense, start typing in the editor or press ```Ctrl+SPACE```
 - To cancel IntelliSense, press ```Esc```
 - To list members of a type (or namespace), ```Ctrl+J```
 - Hover over code artifacts to get Quick Info
 
#### <a name="find"></a>Find & Replace
Use ```Ctrl+F``` / ```Ctrl+H``` to Find/Replace inside the Interactive Window.

<hr/>

### <a name="directives"></a>Directives

*In this section*:
- [#r](#r)
- [#load](#load)
- [#clear or #cls](#clear)
- [#help](#help)
- [#reset](#reset)

#### <a name="r"></a>#r
```#r``` is used to reference assemblies. Below are some examples:
- ```#r "path/MyAssembly.dll"```
- ```#r "MicrosoftLibrary"```, e.g., ```#r "System.Collections.Generic"```

> **Note:** The Interactive Window doesn't currently support ```#r```'ing NuGet packages. As a temporary workaround, reference the NuGet DLL. 

#### <a name="load"></a>#load
```#load``` is used to execute a script file. Variables from the loaded script will shadow previously defined script variables in the new script.
*Example*: ```#load "myScriptContext.csx"```

#### <a name="clear"></a>#clear or #cls
```#clear``` and ```#cls``` clear the contents of the editor of the Interactive Window--both history and context will remain intact.

#### <a name="help"></a>#help
```#help``` will output all variable commands and key bindings. If an argument is specified to the ```#help``` directive, e.g. ```#help clear```, the help documentation for the specific command will be output.

#### <a name="reset"></a>#reset
```#reset``` will reset the execution environment to the initial state--history will remain intact. Use ```#reset noconfig``` to skip the initial configuration script (which we mostly use for default usings).

<hr/>

### <a name="repl"></a>Command-line REPL
To play with the C# REPL outside of Visual Studio, open the **Developer Command Prompt for VS2015** and type the command ```csi``` to begin your interactive session. [Here](https://github.com/dotnet/roslyn/pull/5857) is a list of arguments that can be passed to csi.

> **Note:** csi stands for "CSharp Interactive"

<hr/>

### <a name="samples"></a>Code Samples
Here are some example Interactive sessions that cover some key scenarios.

#### Playing with LINQ
```csharp
> using System.Collections.Generic;
> List<int> mylist = new List<int> { 4, 7, 2, 5, 0, 6 };
> mylist
List<int>(6) { 4, 7, 2, 5, 0, 6 }
> mylist.Where(x => x % 2 == 0)
Enumerable.WhereListIterator<int> { 4, 2, 0, 6 }
> mylist.Average()
4
>
```

#### Answer StackOverflow Questions
Code was pasted from [this StackOverflow question about async methods](http://stackoverflow.com/questions/16063520/how-do-you-create-an-async-method-in-c).

```csharp
> using System.Threading.Tasks;
> async Task<DateTime> CountToAsync(int num = 10)
. {
.     for (int i = 0; i < num; i++)
.     {
.         await Task.Delay(TimeSpan.FromSeconds(1));
.     }
. 
.     return DateTime.Now;
. }
> await CountToAsync()
[10/7/2015 2:38:24 PM]
> 
```

#### Experimenting with APIs
The following session is playing with GitHub's Octokit API.

```csharp
> #r "C:\Users\kaseyu\.nuget\packages\Octokit\0.16.0\lib\net45\Octokit.dll" //local path to DLL on machine
> using Octokit;
> var client = new GitHubClient(new ProductHeaderValue("demo"));
> var pullrequests = await client.PullRequest.GetAllForRepository("dotnet", "roslyn");
> var query = pullrequests.Where(x => x.CreatedAt > DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)));
> foreach (var i in query) {
.     Console.WriteLine($"{i.Title} by {i.User.Login}");
. }
Update nuget references to Microsoft.DiaSymReader 1.0.6 by tmat
[WiP] Adding the Csi task to MSBuildTask. by tannergooding
Allow converted nulls in expr-tree coalesce by TyOverby
Removed extra conversion emitted into expression trees for a lifted binary enum operator by VSadov
Add support for custom modifiers referencing generic types. by AlekseyTs
Add null check for appconfig by mattwar
Fix typo and remove unused fields... by KevinH-MS
Handle known-matches collisions by tmat
Fixes binder-choosing around typeof expressions by TyOverby
Fix nested path check in FilePathUtilities by mattwar
Move back to ci bootstrap on desktop for now by agocke
Collapse '#region' tags the first time a file is ever opened. by CyrusNajmabadi
Don't treat template generated files with assembly attributes as gene… by mavasani
```