# Documentation for IDE CodeStyle analyzers

## Overview

1. Official documentation for all [IDE analyzer diagnostic IDs](../../src/Analyzers/Core/Analyzers/IDEDiagnosticIds.cs) is added to `dotnet/docs` repo at <https://github.com/dotnet/docs/tree/main/docs/fundamentals/code-analysis/style-rules>.

2. Each IDE diagnostic ID has a dedicated documentation page. For example:
   1. Diagnostic with code style option: <https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0011>
   2. Diagnostic without code style option: <https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0004>
   3. Multiple diagnostic IDs sharing the same option(s): <https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0003-ide0009>. In this case, a [redirection](https://github.com/dotnet/docs/blob/7ae7272d643aa1c6db96cad8d09d4c2332855960/.openpublishing.redirection.fundamentals.json#L11-L14) for all diagnostic IDs must be added.

3. We have tabular indices for IDE diagnostic IDs and code style options for easier reference and navigation:
   1. Primary index (rule ID + code style options): <https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/>
   2. Secondary index (code style options only): <https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/language-rules#net-style-rules>
   3. Indices for each preference/option group. For example:
      1. Modifier preferences: <https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/modifier-preferences>
      2. Expression-level preferences: <https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/expression-level-preferences>
 
## Action items when adding or updating IDE analyzers

Roslyn IDE team (@dotnet/roslyn-ide) is responsible for ensuring that the documentation for IDE analyzers is up-to-date. Whenever we add a new IDE analyzer, add new code style option(s) OR update semantics of existing IDE analyzers the following needs to be done:

1. **Action required in [dotnet/docs](https://github.com/dotnet/docs) repo**:
   1. If you _creating a Roslyn PR for an IDE analyzer_:
      1. Please follow the steps at [Contribute docs for 'IDExxxx' rules](https://docs.microsoft.com/contribute/dotnet/dotnet-contribute-code-analysis#contribute-docs-for-idexxxx-rules) to add documentation for IDE analyzers to [dotnet/docs](https://github.com/dotnet/docs) repo.
      2. Ideally, the documentation PR should be created in parallel to the Roslyn PR or immediately following the approval/merge of Roslyn PR.
   2. If you are _reviewing a Roslyn PR for an IDE analyzer_:
      1. Please ensure that the analyzer author is made aware of the above doc contribution requirements, especially if it is a community PR.
      2. If the documentation PR in is not being created in parallel to the Roslyn PR, please ensure that a [new doc issue](https://github.com/dotnet/docs/issues) is filed in `dotnet/docs` repo to track this work.
 
2. **No action required in Roslyn repo**: Help links have been hooked up to IDE code style analyzers in Roslyn repo in the base analyzer types, so every new IDE analyzer diagnostic ID will automatically have `https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ideXXXX` as its help link. There is no action required on the Roslyn PR to ensure this linking.
 
If you feel any of the existing IDE rule docs have insufficient or incorrect information, please submit a PR to [dotnet/docs](https://github.com/dotnet/docs) repo to update the documentation.
