# Razor Basics

Razor is a templating language used in ASP.NET for creating dynamic web pages. It's not a programming language itself, but a markup syntax for embedding code (C# or VB.NET) into HTML.

In a Razor file, you can use a combination of several languages:

| Language | Usage | Supported in .NET Core and .NET 5+ |
| --- | --- | --- |
| **Razor syntax** | Used to embed and execute server-side code within HTML. | Yes |
| **C#** | The server-side language used within Razor templates. Most commonly used with Razor. | Yes |
| **HTML** | Used to structure the content on web pages. | Yes |
| **JavaScript** | Used for client-side scripting in Razor templates. | Yes |
| **CSS** | Used for styling web pages. | Yes |
| **VB.NET** | Can be used in Razor syntax in the older .NET Framework. | No |

Please note that while Razor syntax does support VB.NET in the older .NET Framework, VB.NET is not supported in .NET Core or .NET 5 and onwards. In these newer frameworks, only C# is supported.

## Razor File Types

Razor files typically come in three extensions: `.cshtml`, `.vbhtml`, and `.razor`. Each extension corresponds to a specific type of Razor file and determines its usage within an application:

| File Extension | Type | Description | Usage |
| --- | --- | --- | --- |
| `.cshtml` | Razor View | Part of the MVC (Model-View-Controller) pattern, where the View is responsible for the presentation logic. Located within the Views folder of an MVC application and associated with a Controller. | Used in MVC applications for complex scenarios where separation of concerns is important. |
| `.cshtml` | Razor Page | A page-based programming model that makes building web UI easier and more productive. Located within the Pages folder of a Razor Pages application and includes a `@page` directive at the top. | Used in Razor Pages applications for simpler scenarios where a full MVC model might be overkill. |
| `.razor` | Razor Component (Blazor) | Used in Blazor, a framework for building interactive client-side web UI with .NET. Each `.razor` file is a self-contained component that can include both the markup and the processing logic. | Used in Blazor applications for building interactive client-side web UIs. |
| `.vbhtml` | Razor View (VB.NET) | Part of the MVC (Model-View-Controller) pattern, where the View is responsible for the presentation logic. Located within the Views folder of an MVC application and associated with a Controller. | Used in older MVC applications written in VB.NET. |

## Razor Editors: Legacy vs New

| Aspect | Razor Legacy | Legacy .NET Core Razor Editor | New .NET Core Razor Editor |
| --- | --- | --- | --- |
| **Introduction** | Introduced with ASP.NET MVC 3. | Older Razor editor for ASP.NET Core projects. | Updated Razor editor introduced in Visual Studio 2019 version 16.8. |
| **Usage** | Used in ASP.NET MVC and ASP.NET Web Pages applications. | Used for editing Razor views and pages in ASP.NET Core projects. | Used for editing Razor views and pages in ASP.NET Core projects. |
| **Source code** | Closed source. | Closed source. | [Open source on GitHub](https://github.com/dotnet/razor/) |
| **File Extensions** | `.cshtml` for C#, `.vbhtml` for VB.NET. | `.cshtml` and `.razor` | `.cshtml` and `.razor` |
| **Functionality** | Creates dynamic web pages that combine HTML and server-side code. | Provides basic features like syntax highlighting and IntelliSense for Razor syntax. | Provides improved functionality and performance, including better IntelliSense, improved syntax highlighting, support for Razor formatting, better diagnostics, and features like "Go to Definition" and "Find All References" for Razor components and their parameters. |
| **Support** | Supported for maintaining existing applications. | Phased out, not recommended for new projects. | Actively supported and recommended for new projects. |
| **Configuration** | Used by default for .NET Framework applications. | The legacy .NET Core editor is off by default. | The new .NET Core editor is used by default. |
| **Implementation** | Monolithic design, language services implemented by the editor, no LSP or TextMate grammars, limited VS integration. | Same as Razor Legacy | Uses LSP for language services, TextMate grammars for syntax highlighting, integrated with VS editor API, includes Blazor support. |

## Razor Support Across ASP.NET Versions

Different versions of ASP.NET support different features of Razor. Here's a summary:

| TFM | Razor Support |
| --- | --- |
| **.NET Framework (<= 4.8)** | Supports Razor syntax with C# and VB.NET. Used in ASP.NET MVC and ASP.NET Web Pages applications. |
| **.NET Core 1.x - 3.1** | Supports Razor syntax with C# only. Used in ASP.NET Core MVC, Razor Pages applications, and had preview support for Blazor. |
| **.NET 5+** | Supports Razor syntax with C# only. Used in ASP.NET Core MVC, Razor Pages, and Blazor applications. |