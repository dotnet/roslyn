# Language Feature Status

This document reflects the status, and planned work, for the compiler team.  It is a live document and will be updated as work progresses, features are added / removed, and as work on feature progresses.

## C# 7.1

| Feature | Branch | State | Developers | LDM Champ |
| ------- | ------ | ----- | ------ | --------- |
| [Async Main](https://github.com/dotnet/csharplang/blob/master/proposals/async-main.md) | none  | In Development | [tyoverby](https://github.com/tyoverby) | [stephentoub](https://github.com/stephentoub) |
| [Default Expressions](https://github.com/dotnet/csharplang/blob/master/proposals/target-typed-default.md) | [default](https://github.com/dotnet/roslyn/tree/features/default)  | Prototype | [jcouv](https://github.com/jcouv) | [jcouv](https://github.com/jcouv) |

## C# 7.2

| Feature | Branch | State | Developers | LDM Champ |
| ------- | ------ | ----- | ------ | --------- |
| [ref readonly](https://github.com/dotnet/csharplang/blob/master/proposals/readonly-ref.md) | [readonly-ref](https://github.com/dotnet/roslyn/tree/features/readonly-ref)  | Prototype | [vsadov](https://github.com/vsadov), [omar](https://github.com/OmarTawfikw) | [jaredpar](https://github.com/jaredpar) |
| [blittable](https://github.com/dotnet/csharplang/pull/206) | None | Proposal | None | [jaredpar](https://github.com/jaredpar) |
| [interior pointer](https://github.com/dotnet/csharplang/pull/264) | None | Proposal | [vsadov](https://github.com/vsadov) | [jaredpar](https://github.com/jaredpar) |

## C# 8.0

| Feature | Branch | State | Developers | LDM Champ |
| ------- | ------ | ----- | ------ | --------- |
| [Default Interface Methods](https://github.com/dotnet/csharplang/blob/master/proposals/default-interface-methods.md) | [defaultInterfaceImplementation](https://github.com/dotnet/roslyn/tree/features/DefaultInterfaceImplementation) | Proposal | [AlekseyTs](https://github.com/AlekseyTs) | [gafter](https://github.com/gafter) |

# FAQ

- **Is target version a guarantee?**: No.  It's explicitly not a guarantee.  This is just the planned and ongoing work to the best of our knowledge at this time.
- **Where are these State values defined?**: Take a look at the [Developing a Language Feature](contributing/Developing a Language Feature.md) document.
