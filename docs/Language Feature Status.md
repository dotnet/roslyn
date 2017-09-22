Language Feature Status
=====

This document reflects the status, and planned work in progress, for the compiler team.  It is a live document
and will be updated as work progresses, features are added / removed, and as work on feature progresses.
This is not an exhaustive list of our features but rather the ones which have active development
efforts behind them.

# C# 7.1

| Feature | Branch | State | Developers | Reviewer | LDM Champ |
| ------- | ------ | ----- | ---------- | -------- | --------- |
| [Async Main](https://github.com/dotnet/csharplang/blob/master/proposals/async-main.md) | master | Merged | [tyoverby](https://github.com/tyoverby) | [vsadov](https://github.com/vsadov) | [stephentoub](https://github.com/stephentoub) |
| [Default Expressions](https://github.com/dotnet/csharplang/blob/master/proposals/target-typed-default.md) | master  | Merged | [jcouv](https://github.com/jcouv) | [cston](https://github.com/cston) | [jcouv](https://github.com/jcouv) |
| [Ref Assemblies](https://github.com/dotnet/roslyn/blob/master/docs/features/refout.md) | master | Merged (IDE and project-system integrations ongoing) | [jcouv](https://github.com/jcouv) | [gafter](https://github.com/gafter) | N/A |
| [Infer tuple names](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-7.1/infer-tuple-names.md) | master | Merged | [jcouv](https://github.com/jcouv) | [gafter](https://github.com/gafter) | [jcouv](https://github.com/jcouv) |
| [Pattern-matching with generics](https://github.com/dotnet/csharplang/blob/master/proposals/generics-pattern-match.md) | master  | Merged | [gafter](https://github.com/gafter) | [agocke](https://github.com/agocke) | [gafter](https://github.com/gafter) |

# C# 7.2

| Feature | Branch | State | Developers | Reviewer | LDM Champ |
| ------- | ------ | ----- | ---------- | -------- | --------- |
| [ref readonly](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-7.2/readonly-ref.md) | master | Merged | [vsadov](https://github.com/vsadov), [omar](https://github.com/OmarTawfikw) | [cston](https://github.com/cston),[gafter](https://github.com/gafter) | [jaredpar](https://github.com/jaredpar) |
| [blittable](https://github.com/dotnet/csharplang/pull/206) | None | Proposal | None | | [jaredpar](https://github.com/jaredpar) |
| strongname | [strongname](https://github.com/dotnet/roslyn/tree/features/strongname) | In Progress | [Ty Overby](https://github.com/tyoverby) | | [jaredpar](https://github.com/jaredpar) |
| [interior pointer/Span<T>/ref struct](https://github.com/dotnet/csharplang/pull/264) | master | Merged | [vsadov](https://github.com/vsadov) | [gafter](https://github.com/gafter), [jaredpar](https://github.com/jaredpar) | [jaredpar](https://github.com/jaredpar) |
| [non-trailing named arguments](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-7.2/non-trailing-named-arguments.md) | master | Merged | [jcouv](https://github.com/jcouv) | [gafter](https://github.com/gafter) | [jcouv](https://github.com/jcouv) |
| [private protected](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-7.2/private-protected.md) | master | Merged | [gafter](https://github.com/gafter) | [jcouv](https://github.com/jcouv) | [gafter](https://github.com/gafter) |
| [conditional ref operator](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-7.2/conditional-ref.md) | master | Merged | [vsadov](https://github.com/vsadov) | [cston](https://github.com/cston) | [jaredpar](https://github.com/jaredpar) |

# C# 8.0

| Feature | Branch | State | Developers | Reviewer | LDM Champ |
| ------- | ------ | ----- | ---------- | -------- | --------- |
| [Default Interface Methods](https://github.com/dotnet/csharplang/blob/master/proposals/default-interface-methods.md) | [defaultInterfaceImplementation](https://github.com/dotnet/roslyn/tree/features/DefaultInterfaceImplementation) | Prototype | [AlekseyTs](https://github.com/AlekseyTs) | [gafter](https://github.com/gafter) | [gafter](https://github.com/gafter) |
| [Nullable reference type](https://github.com/dotnet/roslyn/blob/features/NullableReferenceTypes/docs/features/NullableReferenceTypes/Nullable%20reference%20types.md) | [NullableReferenceTypes](https://github.com/dotnet/roslyn/tree/features/NullableReferenceTypes) | Prototype | [cston](https://github.com/cston), [AlekseyTs](https://github.com/AlekseyTs) | | [mattwar](https://github.com/mattwar) |
| [recursive patterns](https://github.com/dotnet/csharplang/blob/master/proposals/patterns.md) | | Ready for prototype | [gafter](https://github.com/gafter) | | [gafter](https://github.com/gafter) |

# FAQ

- **Is target version a guarantee?**: No.  It's explicitly not a guarantee.  This is just the planned and ongoing work to the best of our knowledge at this time.
- **Where are these State values defined?**: Take a look at the [Developing a Language Feature](contributing/Developing%20a%20Language%20Feature.md) document.
