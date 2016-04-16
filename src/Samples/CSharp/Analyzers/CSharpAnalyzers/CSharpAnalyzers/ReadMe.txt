DESCRIPTION

This project contains sample C# analyzers to demonstrate recommended implementation models for different analysis scenarios.
Analyzers have been broadly categorized into the following two buckets based on the kind of analysis performed:
	1) Stateless analyzers: Analyzers that report diagnostics about a specific code unit, such as a symbol, syntax node, code block, compilation, etc.
			Most analyzers would fall into this bucket.
			These analyzers register one or more actions, that are:
			(a) Do not require maintaining state across analyzer actions and
			(b) Independent of the order of execution of individual analyzer actions.
	2) Stateful analyzers: Analyzers that report diagnostics about a specific code unit, but in context of an enclosing code unit such as a code block or a compilation.
			These represent slightly more complicated analyzers that require more powerful and wider analysis and hence need careful design to achieve efficient analyzer execution without memory leaks.
			These analyzers require at least one of the following kind of state manipulation for analysis:
			(a) Access to immutable state object(s) for the enclosing code unit, such as a compilation or the code block. For example, access to certain well known types defined in a compilation.
			(b) Perform analysis over the enclosing code unit, with mutable state defined and initialized in a start action for the enclosing code unit, intermediate actions that access and/or update this state, and an end action to report diagnostic on the individual code units.
			
Following sample analyzers, with simple unit tests, are included in this solution:
	1) Stateless analyzers:
			(a) SymbolAnalyzer: Analyzer for reporting symbol diagnostics.
			(b) SyntaxNodeAnalyzer: Analyzer for reporting syntax node diagnostics.
			(c) CodeBlockAnalyzer: Analyzer for reporting code block diagnostics.
			(d) CompilationAnalyzer: Analyzer for reporting compilation diagnostics.
			(e) SyntaxTreeAnalyzer: Analyzer for reporting syntax tree diagnostics.
			(f) SemanticModelAnalyzer: Analyzer for reporting syntax tree diagnostics, that require some semantic analysis.
	2) Stateful analyzers:
			(a) CodeBlockStartedAnalyzer: Analyzer to demonstrate code block wide analysis.
			(b) CompilationStartedAnalyzer: Analyzer to demonstrate analysis within a compilation, for example analysis that depends on certain well-known symbol(s).
			(c) CompilationStartedAnalyzerWithCompilationWideAnalysis: Analyzer to demonstrate compilation-wide analysis.
