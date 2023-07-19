This walkthrough is a beginner's guide to learning basic interactive concepts and how to navigate the C# Interactive Window. To learn more about the Interactive window, watch [this video](https://channel9.msdn.com/Events/Visual-Studio/Connect-event-2015/103) or check out [our documentation](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Interactive-Window.md).

*Note*: This walkthrough is adapted from [Bill Chiles](https://github.com/billchi-ms)' original. Thanks, Bill!

## Introduction: What is Interactive?

With the new **C# Interactive window**, you get immediate feedback on what an expression will return or what an API call does. The Interactive window is much like the Immediate window, but the Interactive window has many improvements such as IntelliSense features and the ability to redefine functions and classes. After entering a code snippet at the REPL prompt, the code simply executes right away. You can enter statements and expressions, as well as class and function definitions. You do not need to create a project, define a class, define a Main, write your expression in a ```Console.WriteLine()``` call, and then enter the code for a bogus ```Console.ReadLine``` to keep the cmd.exe window alive. When you type each line of code that is a complete expression or construct and then press ```Enter```, the code executes.  If the entered code is incomplete when you press ```Enter```, it does not execute, and you can continue entering code.

The Interactive window is a read-eval-print loop (or [REPL](http://en.wikipedia.org/wiki/REPL)). As you can see, REPLs are productivity enhancers that have long been the purview of dynamic and functional languages. The Roslyn REPL for C# brings all the goodness you associate with VS such as code-completion, syntax-coloring, Quick Fixes, etc., to the REPL experience.

To use this walkthrough, you must first install [Visual Studio 2015 Update 1](http://go.microsoft.com/fwlink/?LinkId=691129).

## Walkthrough

This walkthrough demonstrates the following features or attributes of the C# Interactive window:
- Launching REPL with default execution context
- Using directives
- History commands with ```Alt+UpArrow``` and ```Alt+DownArrow```
- Enter to execute if input is complete
- ```Ctrl+Enter``` to force execution of current input, or fetch previous input if the caret is in a previous submission
- ```Shift+Enter``` to insert a newline without executing the current input
- Var declarations
- Colorization, completion, param tips
- Expression evaluation
- Multi-line input
- Multi-line history with editing
- Shows redirecting Console I/O to REPL

### Steps
1. Open [Visual Studio 2015 Update 1](http://go.microsoft.com/fwlink/?LinkId=691129)

2. On the **View** menu, choose **Other Windows** and then **C# Interactive**. It is best to drag this to the document bay and dock the Interactive window at the bottom of the document bay. It is common to work in an editor buffer and the REPL, switching back and forth for entering and saving code snippets, particularly if you are writing a script.

3. To honor our ancestry, let's enter the obligatory 'Hello World' program in the REPL. Type the following, and press ```Enter```:
  
```csharp
> Console.Write("Hello, World!")
```
  
4. There are commands in the REPL that look like directives. The ```#help``` command describes common commands (input starting with #) and shortcuts for getting started:

```csharp
> #help
```
 
5. Type the following input, and then press ```Enter```. Notice that IntelliSense completion helps while you type.
 
```csharp
> using System.IO;
```

Then input the following command, which you can do by using ```Alt+UpArrow``` to invoke history, backspace to delete "IO;", then type "Net;", and press ```Enter```.

```csharp
> using System.Net;
```

6. Enter the following partial statement without a semicolon at the end, and press ```Enter```

```csharp
> var url =
```

Because the statement is not yet complete, the REPL does not execute it, and the code continues to the next line. Paste in the following code, and then press ```Enter```. This code is a continuation of the previous line.
  
```csharp
"https://download.microsoft.com/download/4/C/8/4C830C0C-101F-4BF2-8FCB-32D9A8BA906A/Import_User_Sample_en.csv";
```
  
Because the line is complete, pressing ```Enter``` at the end of the input executes the code.
  
7. Now view the contents of the variable by entering it as an expression. Type just the variable name without a semicolon at the end:

```csharp
> url
```

8. Here's another way to view the results of an expression evaluation. Type the following, and press ```Enter```.

```csharp
> Console.Write("url: " + url)
```
  
9. Enter the following lines into the Interactive window. The code uses a WebRequest instance to download data from the web site, and puts the result in the ```csv``` string variable. For more information and a similar example, see [WebRequest Class](https://msdn.microsoft.com/en-us/library/system.net.webrequest.aspx).

```csharp
> var request = WebRequest.Create(url);
> var response = request.GetResponse();
> var dataStream = response.GetResponseStream();
> var reader = new StreamReader(dataStream);
> var csv = await reader.ReadToEndAsync();
> reader.Close();
> dataStream.Close();
> response.Close();
```
 
Note that we can have asynchronous code inside the REPL. The REPL will wait for the async code to be evaluated before continuing.

10. Now that we have some data we pulled, let's inspect the data briefly before moving forward. Enter the following line without a semicolon at the end:

```csharp
> csv.Length
```
  
  The REPL displays the length of the ```csv``` string on the next line.
  
11. Ok, there's a lot of data, so maybe we can break it up. Let's see how many lines there are:

```csharp
> csv.Split('\n').Length
```

12. Only a few lines, so let's peek at the first couple of hundred characters and see if we can glean something of the string's structure or how long the lines are. Enter the following code:

```csharp
> Console.Write(csv.Substring(0,600))
```
  
You'll see output similar to the following:
  
```
User Name,First Name,Last Name,Display Name,Job Title,Department,Office Number,Office Phone,Mobile Phone,Fax,Address,City,State or Province,ZIP or Postal Code,Country or Region
chris@contoso.com,Chris,Green,Chris Green,IT Manager,Information Technology,123451,123-555-1211,123-555-6641,123-555-9821,1 Microsoft way,Redmond,Wa,98052,United States
ben@contoso.com,Ben,Andrews,Ben Andrews,IT Manager,Information Technology,123452,123-555-1212,123-555-6642,123-555-9822,1 Microsoft way,Redmond,Wa,98052,United States
david@contoso.com,David,Longmuir,David Longmuir,IT Manager,Information Technology,12
```

13. Now we can see what the structure of the data is. Let's build a query to extract the volume from the last column (Skip(1) skips header row). You can use ```Shift+Enter``` at the end of a line to avoid executing the input until you've entered everything; ```Enter``` only evaluates if the expression looks complete:

```csharp
var users = csv.Split('\n').Skip(1)
                .Select(line => line.Split(','))
                .Where(values => values.Length == 15)
                .Select(values => new { 
                    firstName = values[1], 
                    lastName = values[2], 
                    officeNumber = int.Parse(values[6]) 
                });
```

14. Let's print out a bit of the users from the query. You can use ```Shift+Enter``` after the first to lines to avoid executing them immediately. If you use ```Enter``` inside the 'foreach' loop, the code won't execute until you type the final curly brace and then press ```Enter```.

```csharp
foreach (var u in users)
    Console.WriteLine(u)
```

Here is what your final session should look like:

```csharp
using System.IO;
using System.Net;

var url = "https://download.microsoft.com/download/4/C/8/4C830C0C-101F-4BF2-8FCB-32D9A8BA906A/Import_User_Sample_en.csv";
var request = WebRequest.Create(url);
var response = request.GetResponse();
var dataStream = response.GetResponseStream();
var reader = new StreamReader(dataStream);
var csv = await reader.ReadToEndAsync();
reader.Close();
dataStream.Close();
response.Close();
var users = csv.Split('\n').Skip(1)
                .Select(line => line.Split(','))
                .Where(values => values.Length == 15)
                .Select(values => new {
                    firstName = values[1],
                    lastName = values[2],
                    officeNumber = int.Parse(values[6])
                });

foreach (var u in users)
     Console.WriteLine(u);
```

You're done. Enjoy using the REPL and please provide feedback! If you are interested in learning more about the C# Interactive window, watch [this video](https://channel9.msdn.com/Events/Visual-Studio/Connect-event-2015/103) or check out [our documentation](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Interactive-Window.md).
