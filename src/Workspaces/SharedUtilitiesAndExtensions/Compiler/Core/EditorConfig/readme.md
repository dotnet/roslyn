# EditorConfig Parser with TextSpan support

The purpose of the types here is to implement an extensible editorconfig parser that allows you to get strongly typed options.

The implementation is very similar to the [`AnalyzerConfig`](../../../../../Compilers/Core/Portable/CommandLine/AnalyzerConfig.cs) type that currently lives in in the compiler except it has been expanded to be able to produce strongly typed versions of options and track spans where options are defined in the file.
The span tracking is the crucial part as naming styles (the editorconfig options that necessitated its creation) have many spans that compose to create a single option. Without some mechanism to keep track of all these locations it becomes impossible to make edits.

In addition to span tracking the section matcher (the part that determines if an ini-style section header like `[*.cs]` is a match for a particular file) has expanded support to explain why a section header was chosen as a match and has the ability to use a set of search criteria to try and find the "best" place for a new option to be written two in the file.

All of the functionality was not added to `AnalyzerConfig` because that type needs to remain as performant as possible as it is crucial to the compilation. If in the future we can do the validation necessary to prove that none of these new capabilities has an adverse effect on performance then we can start the discussion with the compiler team on merging these types.

The tests for these types can be found in `src\Workspaces\CoreTest\EditorConfigParsing`
