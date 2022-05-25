For each release, some steps to check:

- start: declare the new Language Versions when a common branch becomes available (this has a checklist of its own)
- start: review/triage issues that should be considered during that milestone (scrub unlabeled issues)
- preview: review public API changes
- preview: remove any unused Language Version
- preview/RTM: notify partners of preview and RTM packages being published to NuGet
- preview/RTM: notify documentation team of first preview and RTM timeline (to publish docs)
- preview/RTM: update Visual Studio release notes
- RTM: announce the new features on the team blog and in the Visual Studio Preview release notes
- RTM: record the [language history](https://github.com/dotnet/csharplang/blob/main/Language-Version-History.md) and move the [language proposals](https://github.com/dotnet/csharplang/tree/main/proposals) to the appropriate folder
- update the package description(s)

For each language feature:

- work with LDM to identify a champion and define the feature
- update the [Language Feature Status](https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md) page as we start working on features
- identify a designated reviewer
- notify Jared and Neal on PRs with public API changes
- breaking changes need to be approved by the compat council and [documented](https://github.com/dotnet/roslyn/tree/main/docs/compilers/CSharp)
- designated reviewer should document and validate a test plan
- blocking issues should be identified and resolved before merging feature back to `main` (that includes resolving `PROTOTYPE` comments)
- update the feature status when the feature is complete

To add a language version:
- add it to the compiler (tests have a checklist)
- add it to project-systems so that it appears in the UI drop-down
- for VB, add it in an internal repo for legacy project system
- add it to [release.json](https://github.com/dotnet/core/pull/1454) (needs to be confirmed)