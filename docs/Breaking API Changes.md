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

# Version 1.3.0

### Treat a method marked with both public and private flags as private

The scenario is loading an assembly where some methods, fields or nested types have accessibility flags set to 7 (all three bits set), which mean public AND private.
After the fix, such flags are loaded to mean private.
The compat change is we’re trading a compile-time success and runtime failure (native compiler) against a compile-time error (restoring the behavior of v1.2).

Details below:

- The native compiler successfully compiles the method and field case (those only yield runtime error System.TypeLoadException: Invalid Field Access Flags) and reported an accessibility error on the nested type.
- The 1.2 compiler generated errors:
```
error BC30390: 'C.Private Overloads Sub M()' is not accessible in this context because it is 'Private'.
error BC30389: 'C.F' is not accessible in this context because it is 'Private'.
error BC30389: 'C.C2' is not accessible in this context because it is 'Protected Friend'.
error BC30390: 'C2.Private Overloads Sub M2()' is not accessible in this context because it is 'Private'.
```
- The 1.3 compiler crashes.
- After fix, the same errors as 1.2 are generated again.

PR: https://github.com/dotnet/roslyn/pull/11547

### Don't emit bad DateTimeConstant, and load bad BadTimeConstant as default value instead

The change affects compatibility in two ways:

- When loading an invalid DateTimeConstant(-1), the compiler will use default(DateTime) instead, whereas the native compiler would produce code that fails to execute.
- DateTimeConstant(-1) will still count when we check that you don’t specify two default values. The compiler will produce an error, instead of succeeding (and producing IL with two attributes).

PR: https://github.com/dotnet/roslyn/pull/11536

# Version 4.1.0

### Can no longer inherit from CompletionService and CompletionServiceWithProviders

The constructors of Microsoft.CodeAnalysis.Completion and Microsoft.CodeAnalysis.Completion.CompletionServiceWithProviders are now internal.
Roslyn does not support implementing completion for arbitrary languages.

# Version 4.2.0

### Can no longer inherit from QuickInfoService

The constructors of Microsoft.CodeAnalysis.QuickInfoService are now internal.
Roslyn does not support implementing completion for arbitrary languages.

### `Microsoft.CodeAnalysis.CodeStyle.NotificationOption` is now immutable

All property setters now throw an exception.
