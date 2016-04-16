API Breaking Changes
====

# Version 1.1.0 

### Removed VisualBasicCommandLineParser.ctor 
During a toolset update we noticed the constructor on `VisualBasicCommandLineParser` was `public`.  This in turn made many of the `protected` members of `CommandLineParser` a part of the API surface as it gave external customers an inheritance path.  

It was never the intent for these members to be a part of the supported API surface.  Creation of the parsers is meant to be done via the `Default` singleton properties.  There seems to be little risk that we broke any customers here and hence we decided to remove this API.  

PR: https://github.com/dotnet/roslyn/pull/4169

### Changed Simplifier methods to throw ArgumentNullExceptions 
Changed Simplifier.ReduceAsync, Simplifier.ExpandAsync, and Simplifier.Expand methods to throw ArgumentNullExceptions if any non-optional, nullable arguments are passed in.  Previously the user would get a NullReferenceException for synchronous methods and an AggregateException containing a NullReferenceException for asynchronous methods.

PR: https://github.com/dotnet/roslyn/pull/5144

