# Creating a C# Language Server Framework

## Background

The [language server protocol](https://microsoft.github.io/language-server-protocol/specification) is a standard that IDE's can implement in order to provide language service functionality for any number of languages. Because of this, on the official LSP specification site there exists a [list of implementations](https://microsoft.github.io/language-server-protocol/implementors/servers/) for each language. This list contains the list of language servers for languages while simultaneously indicating their "implementation language". The implementation language is where things get interesting (was it written in C#, JS, C++ etc.). Each implementation language typically has a standard framework / library to make writing language servers easy. For instance there's a [JavaScript framework](https://github.com/microsoft/vscode-languageserver-node) and even a [C# framework](https://github.com/omniSharp/csharp-language-server-protocol/) to ease in the writing of language servers. Having a framework is appealing because language servers don't exist for there to be only a single language server for a single language; they exist so that many can be created to provide ever-extending language services for features and are language agnostic (fun fact: Python language server used to be written in C#).

## Problem

As mentioned in the [Background](#background) section there exists many language server frameworks, one of which is C# which we'll call from here on out the **C# LSP framework**. The C# LSP framework was written by the creators of [OmniSharp](http://www.omnisharp.net/), is open source and today maintained by a single individual (David Driscoll). The framework originated with the intent to make writing C# language servers easy and quickly became a catch-all for every problem in the language server ecosystem. Effectively it saw all the problems that could possible exist and tried to solve them. While attempting to solve every problem was a noble cause it became its biggest weakness. Solving every problem for every C# language server has led to heaps of dependencies, fragile request handling, arcane service resolution, lengthy startup times, awkward end-user APIs and a highly coupled internal system that's difficult to reason about. In addition to the above given the lack of investment in modernizing the O# framework from the community it has also collected significant technical debt preventing those who depend on it to evolve with the times.

As it stands today the Razor language server currently depends on the C# LSP framework and because of this it encounters the following problems:

- Slower startup time
- High dependency count
- Unstructured logging / tracing
- Race conditions in request handling
- Replication of Visual Studio LSP++ concepts

## Proposal

Being the creators of the Razor, Roslyn, and WebTools language servers we can take what we've learned and build a simplistic C# framework that Razor, C#, HTML, CSS and any other partner can sit ontop of to simplify language server development. Future improvements to the framework would translate across server boundaries, implementations would be consistent and any LSP++ implementations would be readily available for all to consume. Similarly I could envision a world where IntelliCode and other third parties may be able to use this tech to better extend Visual Studio in a reasonable way.

The new framework would opt-for simplicity. This means we wouldn't add bells and whistles to automatically understand configuration or automatically include various services in handlers. In a lot of ways we'd modernize what the Roslyn C# server has already done and lift or add bits of functionality that is unequivocally required.

This simplistic approach would mean building a core piece of infrastructure that could consume and respond to requests in an LSP centric way. Some of the things we can tackle:

- Build request handling that is `textDocument/didX` aware (things that typically mutate state) and optimize when certain requests are run to prevent races while allowing high parallelizability
- Create LSP++ APIs / handler abstractions to ease in implementation
- Do less work on startup to allow for instantaneous startup
- Structurally log specific bits of information to ease in diagnosability
- Reduce the number of dependencies we have / include in all platforms
- Couple to the Visual Studio language server protocol binaries

And lastly it goes without saying that building a server framework internally would enable us to iterate quicker on core-platform issues without waiting for the C# LSP frameworks reaction.
