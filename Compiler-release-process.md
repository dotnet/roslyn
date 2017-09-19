For each release, some steps to check:

- declare the new Language Versions when a common branch becomes available (this has a checklist of its own)
- review/triage issues that should be considered during that milestone (scrub unlabeled issues)
- review public API changes
- notify partners of preview and RTM packages being published to NuGet
- notify documentation team of first preview and RTM timeline (to publish docs)
- announce the new features on the team blog and in the Visual Studio Preview release notes
- record the [language history](https://github.com/dotnet/csharplang/blob/master/Language-Version-History.md) and move the [language proposals](https://github.com/dotnet/csharplang/tree/master/proposals) to the appropriate folder
- update the package description(s)
- remove any unused Language Version

For each language feature:

- work with LDM to define the feature
- update the [Language Feature Status](https://github.com/dotnet/roslyn/blob/master/docs/Language%20Feature%20Status.md) page as we start working on features
- identify a designated reviewer
- notify Jared and Neal on PRs with public API changes
- breaking changes need to be approved by the compat council and [documented](https://github.com/dotnet/roslyn/tree/master/docs/compilers/CSharp)
- designated reviewer should document and validate a test plan
- blocking issues should be identified and resolved before merging feature back to `master` (that includes resolving `PROTOTYPE` comments)
- update the feature status when the feature is complete
