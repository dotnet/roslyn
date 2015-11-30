This walkthrough is a beginner's guide to learning basic interactive concepts and how to navigate the C# Interactive Window. To learn more about the Interactive window, watch [this video](channel9.msdn.com/Events/Visual-Studio/Connect-event-2015/103) or check out [our documentation](https://github.com/dotnet/roslyn/wiki/Interactive-Window).

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
1. On the **View** menu, choose **Other Windows** and then **C# Interactive**. It is best to drag this to the document bay and dock the Interactive window at the bottom of the document bay. It is common to work in an editor buffer and the REPL, switching back and forth for entering and saving code snippets, particularly if you are writing a script.
1. To honor our ancestry, let's enter the obligatory 'Hello World' program in the REPL. Type the following, and press ```Enter```:
  
  ```csharp
  > Console.Write("Hello, World!")
  ```
  
1. There are commands in the REPL that look like directives. The ```#help``` command describes common commands (input starting with #) and shortcuts for getting started:

 ```csharp
 > #help
 ```
 
1. Type the following input, and then press ```Enter```. Notice that IntelliSense completion helps while you type.
 
  ```csharp
  > using System.IO;
  ```
  Then input the following command, which you can do by using ```Alt+UpArrow``` to invoke history, backspace to delete "IO;", then type "Net;", and press ```Enter```.
  ```csharp
  > using System.Net;
  ```

1. Enter the following partial statement without a semicolon at the end, and press ```Enter```
  ```csharp
  > var url =
  ```
  Because the statement is not yet complete, the REPL does not execute it, and the code continues to the next line. Paste in the following code, and then press ```Enter```. This code is a continuation of the previous line.
  
  ```csharp
  "http://ichart.finance.yahoo.com/table.csv?s=MSFT&d=10&e=10&f=2015&g=d&a=2&b=13&c=1986&ignore=.csv";
  ```
  
  Because the line is complete, pressing ```Enter``` at the end of the input executes the code.
  
1. Now view the contents of the variable by entering it as an expression. Type just the variable name without a semicolon at the end:
  ```csharp
  > url
  ```

1. Here's another way to view the results of an expression evaluation. Type the following, and press ```Enter```.
  ```csharp
  > Console.Write("url: " + url)
  ```
  
1. Enter the following lines into the Interactive window. The code uses a WebRequest instance to download data from the web site, and puts the result in the ```csv``` string variable. For more information and a similar example, see [WebRequest Class](https://msdn.microsoft.com/en-us/library/system.net.webrequest.aspx).
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

1. Now that we have some data we pulled, let's inspect the data briefly before moving forward. Enter the following line without a semicolon at the end:
  ```csharp
  > csv.Length
  ```
  
  The REPL displays the length of the ```csv``` string on the next line.
  
1. Ok, there's a lot of data, so maybe we can break it up. Let's see how many lines there are:
  ```csharp
  > csv.Split('\n').Length
  ```

1. Still a lot of lines, so let's peek at the first couple of hundred characters and see if we can glean something of the string's structure or how long the lines are. Enter the following code:
  ```csharp
  > Console.Write(csv.Substring(0,200))
  ```
  
  You'll see output similar to the following:
  
  <p>Date,Open,High,Low,Close,Volume,Adj Close</p>
  <p>2015-11-10,54.07,54.130001,53.27,53.509998,55020300,53.509998</p>
  <p>2015-11-09,54.549999,54.869999,53.560001,54.16,32466400,54.16</p>
  <p>2015-11-06,54.09,54.98,53.959999,5</p>

1. Now we can see what the structure of the data is. Let's build a query to extract the adjusted closing price from the last column (Skip(1) skips header row). You can use ```Shift+Enter``` at the end of a line to avoid executing the input until you’ve entered everything; ```Enter``` only evaluates if the expression looks complete:
  ```csharp
  var prices = csv.Split('\n').Skip(1)
     			  .Select(line => line.Split(','))
     			  .Where(values => values.Length == 7)
     			  .Select(values => new { date = DateTime.Parse(values[0]), 
					   					  price = float.Parse(values[6]) });
  ```

1. Let's print out a bit of the prices from the query. You can use ```Shift+Enter``` after the first to lines to avoid executing them immediately. If you use ```Enter``` inside the 'foreach' loop, the code won't execute until you type the final curly brace and then press ```Enter```.
  ```csharp
  foreach (var p in prices.Take(10))
  	  Console.WriteLine(p)
  ```

Here is what your final session should look like:
```csharp
var url = "http://ichart.finance.yahoo.com/table.csv?s=MSFT&d=10&e=10&f=2015&g=d&a=2&b=13&c=1986&ignore=.csv";
var request = WebRequest.Create(url);
var response = request.GetResponse();
var dataStream = response.GetResponseStream();
var reader = new StreamReader(dataStream);
var csv = await reader.ReadToEndAsync();
reader.Close();
dataStream.Close();
response.Close();
var prices = csv.Split('\n').Skip(1)
                .Select(line => line.Split(','))
                .Where(values => values.Length == 7)
                .Select(values => new {
                    date = DateTime.Parse(values[0]),
                    price = float.Parse(values[6])
                });
foreach (var p in prices.Take(10))
        Console.WriteLine(p);
```

You're done. Enjoy using the REPL and please provide feedback! If you are interested in learning more about the C# Interactive window, watch [this video](channel9.msdn.com/Events/Visual-Studio/Connect-event-2015/103) or check out [our documentation](https://github.com/dotnet/roslyn/wiki/Interactive-Window).