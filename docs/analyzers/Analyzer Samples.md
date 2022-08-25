**Samples Location:**

Sample analyzers to demonstrate recommended implementation models for different analysis scenarios have been added to [Samples.sln](https://github.com/dotnet/roslyn-sdk/tree/main/Samples.sln).

**Description:**

Analyzers have been broadly categorized into the following three buckets based on the kind of analysis performed:
  1. Stateless analyzers: Analyzers that report diagnostics about a specific code unit, such as a symbol, syntax node, code block, compilation, etc. Most analyzers would fall into this bucket.
     These analyzers register one or more actions, that:
     1. Do not require maintaining state across analyzer actions and are
     2. Independent of the order of execution of individual analyzer actions.
  2. Stateful analyzers: Analyzers that report diagnostics about a specific code unit, but in context of an enclosing code unit such as a code block or a compilation. These represent slightly more complicated analyzers that require more powerful and wider analysis and hence need careful design to achieve efficient analyzer execution without memory leaks.
     These analyzers require at least one of the following kind of state manipulation for analysis:
     1. Access to immutable state object(s) for the enclosing code unit, such as a compilation or the code block. For example, access to certain well known types defined in a compilation.
     2. Perform analysis over the enclosing code unit, with mutable state defined and initialized in a start action for the enclosing code unit, intermediate actions that access and/or update this state, and an end action to report diagnostic on the individual code units.
  3. Additional File analyzers: Analyzers that read data from non-source text files included in the project.
		
**Contents:**
	
Following sample analyzers, with simple unit tests, are provided:
  1. [Stateless analyzers](https://github.com/dotnet/roslyn-sdk/tree/master/samples/CSharp/Analyzers/Analyzers.Implementation/StatelessAnalyzers):
     1. SymbolAnalyzer: Analyzer for reporting symbol diagnostics.
     2. SyntaxNodeAnalyzer: Analyzer for reporting syntax node diagnostics.
     3. CodeBlockAnalyzer: Analyzer for reporting code block diagnostics.
     4. CompilationAnalyzer: Analyzer for reporting compilation diagnostics.
     5. SyntaxTreeAnalyzer: Analyzer for reporting syntax tree diagnostics.
     6. SemanticModelAnalyzer: Analyzer for reporting syntax tree diagnostics, that require some semantic analysis.
  2. [Stateful analyzers](https://github.com/dotnet/roslyn-sdk/tree/master/samples/CSharp/Analyzers/Analyzers.Implementation/StatefulAnalyzers):
     1. CodeBlockStartedAnalyzer: Analyzer to demonstrate code block wide analysis.
     2. CompilationStartedAnalyzer: Analyzer to demonstrate analysis within a compilation, for example analysis that depends on certain well-known symbol(s).
     3. CompilationStartedAnalyzerWithCompilationWideAnalysis: Analyzer to demonstrate compilation-wide analysis.
  3. [Additional File analyzers](https://github.com/dotnet/roslyn-sdk/tree/master/samples/CSharp/Analyzers/Analyzers.Implementation/AdditionalFileAnalyzers):
     1. SimpleAdditionalFileAnalyzer: Demonstrates reading an additional file line-by-line and using the data in analysis.
     2. XmlAdditionalFileAnalyzer: Demonstrates writing an additional file out to a `Stream` so that it can be read back as a structured document (in this case, XML).
