# Contributing to Roslyn

One of the easiest ways to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests with code changes.

## General feedback and discussions?

Start a discussion on the [repository issue tracker](https://github.com/dotnet/roslyn/issues).

## Bugs and feature requests?

❗ **IMPORTANT: If you want to report a security-related issue, please see the `Reporting security issues and bugs` section below.**

Before reporting a new issue, try to find an existing issue if one already exists. If it already exists, upvote (👍) it. Also, consider adding a comment with your unique scenarios and requirements related to that issue.  Upvotes and clear details on the issue's impact help us prioritize the most important issues to be worked on sooner rather than later. If you can't find one, that's okay, we'd rather get a duplicate report than none.

If you can't find an existing issue, log a new issue in this GitHub repository.

## Creating Issues

- **DO** use a descriptive title that identifies the issue to be addressed or the requested feature. For example, when describing an issue where the compiler is not behaving as expected, write your bug title in terms of what the compiler should do rather than what it is doing – “C# compiler should report CS1234 when Xyz is used in Abcd.”
- **DO** specify a detailed description of the issue or requested feature.
- **DO** provide the following for bug reports
    - Describe the expected behavior and the actual behavior. If it is not self-evident such as in the case of a crash, provide an explanation for why the expected behavior is expected.
    - Provide example code that reproduces the issue.
    - Specify any relevant exception messages and stack traces.
- **DO** subscribe to notifications for the created issue in case there are any follow up questions.

## Reporting security issues and bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC)  secure@microsoft.com. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://technet.microsoft.com/security/ff852094.aspx).

## Writing Code

### Finding an issue to work on

  Over the years we've seen many PRs targeting areas of the framework, which we didn't plan to expand further at the time.
  In all these cases we had to say `no` to those PRs and close them. That, obviously, is not a great outcome for us. And it's especially bad for the contributor, as they've spent a lot of effort preparing the change.
  To resolve this problem, we've decided to separate a bucket of issues, which would be great candidates for community members to contribute to. We mark these issues with the `help wanted` label. You can find all these issues at [IDE](https://aka.ms/roslyn-ide-bugs-help-wanted) and [Compiler](https://aka.ms/roslyn-compiler-bugs-help-wanted).

  Within that set, we have additionally marked issues that are good candidates for first-time contributors. Those do not require too much familiarity with the framework and are more novice-friendly. Those are marked with the `good first issue` label. The full list of such issues can be found at [https://github.com/dotnet/roslyn/labels/good%20first%20issue](https://github.com/dotnet/roslyn/labels/good%20first%20issue).

  If you would like to make a contribution to an area not documented here, first open an issue with a description of the change you would like to make and the problem it solves so it can be discussed before a pull request is submitted.

  
### Resources to help you get started

Here are some resources to help you get started on how to contribute code or new content.

* Look at the [Contributor documentation](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md) to get started on building the source code on your own.
* Finding a bug to fix in the [IDE](https://aka.ms/roslyn-ide-bugs-help-wanted) or [Compiler](https://aka.ms/roslyn-compiler-bugs-help-wanted)
* Finding a feature to implement in the [IDE](https://aka.ms/roslyn-ide-feature-help-wanted) or [Compiler](https://aka.ms/roslyn-compiler-feature-help-wanted)

### Identifying the scale

If you would like to contribute to one of our repositories, first identify the scale of what you would like to contribute. If it is small (grammar/spelling or a bug fix) feel free to start working on a fix. 

If you are submitting a feature or substantial code contribution, please discuss it with the team and ensure it follows the product roadmap.
You might also read these two blogs posts on contributing code: [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) by Miguel de Icaza and [Don't "Push" Your Pull Requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/) by Ilya Grigorik. 

All code submissions will be rigorously reviewed and tested further by the Roslyn team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.

### Before writing code

  To file a API proposal, look for the relevant issue in the `New issue` page or simply click [this link](https://github.com/dotnet/roslyn/issues/new?assignees=&labels=Feature+Request%2CConcept-API&projects=&template=api-suggestion.md), as part of the [API review process](<docs/contributing/API Review Process.md>).

## Coding Style

The Roslyn project is a member of the [.NET Foundation](https://github.com/orgs/dotnet) and follows the same [developer guide](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md).  The repo also includes [.editorconfig](http://editorconfig.org) files to help enforce this convention.  Contributors should ensure they follow these guidelines when making submissions.  

### CSharp

- **DO** use the coding style outlined in the [.NET Runtime Coding Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)
- **DO** use plain code to validate parameters at public boundaries. Do not use Contracts or magic helpers.

```csharp
if (argument == null)
{
    throw new ArgumentNullException(nameof(argument));
}
```

- **DO** use `Debug.Assert()` for checks not needed in release builds. Always include a “message” string in your assert to identify failure conditions. Add assertions to document assumptions on non-local program state or parameter values, e.g. “At this point in parsing the scanner should have been advanced to a ‘.’ token by the caller”.
- **DO** avoid allocations in compiler hot paths:
    - Avoid LINQ.
    - Avoid using `foreach` over collections that do not have a `struct` enumerator.
    - Consider using an object pool. There are many usages of object pools in the compiler to see an example.

### Visual Basic Conventions

- **DO** apply the spirit of C# guidelines to Visual Basic when there are natural analogs. 
- **DO** place all field declarations at the beginning of a type definition

### Tips 'n' Tricks
Our team finds using [this enhanced source view](http://sourceroslyn.io/) of Roslyn helpful when developing.

## How to submit a PR

We are always happy to see PRs from community members both for bug fixes as well as new features.
To help you be successful we've put together a few simple rules to follow when you prepare to contribute to our codebase.

### Before submitting the pull request

Before submitting a pull request, make sure that it checks the following requirements:

* You find an existing issue with the "help-wanted" label or discuss with the team to agree on adding a new issue with that label
* You post a high-level description of how it will be implemented and receive a positive acknowledgement from the team before getting too committed to the approach or investing too much effort in implementing it.
* You add test coverage following existing patterns within the codebase
* Your code matches the existing syntax conventions within the codebase
* Your PR is small, focused, and avoids making unrelated changes

If your pull request contains any of the below, it's less likely to be merged.

* Changes that break backward compatibility
* Changes that are only wanted by one person/company. Changes need to benefit a large enough proportion of Roslyn developers.
* Changes that add entirely new feature areas without prior agreement
* Changes that are mostly about refactoring existing code or code style
* Very large PRs that would take hours to review (remember, we're trying to help lots of people at once). For larger work areas, please discuss with us to find ways of breaking it down into smaller, incremental pieces that can go into separate PRs.

### Submitting a pull request

You will need to sign a [Contributor License Agreement](https://cla.dotnetfoundation.org/) when submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to follow the instructions provided by the CLA bot when you send the pull request. This needs to only be done once for any .NET Foundation OSS project.

If you don't know what a pull request is read this article: <https://help.github.com/articles/using-pull-requests>. Make sure the repository can build and all tests pass. Familiarize yourself with the project workflow and our coding conventions. 

- **DO** ensure submissions pass all Azure DevOps legs and are merge conflict free.
- **DO** follow the [.editorconfig](http://editorconfig.org/) settings for each directory. 
- **DO** submit language feature requests as issues in the [C# language](https://github.com/dotnet/csharplang#discussion) / [VB language](https://github.com/dotnet/vblang) repos.  Once a feature is championed and validated by LDM, a developer will be assigned to help begin a prototype on this repo inside a feature branch.
- **DO NOT** submit language features as PRs to this repo first, or they will likely be declined.
- **DO** submit issues for other features. This facilitates discussion of a feature separately from its implementation, and increases the acceptance rates for pull requests.
- **DO NOT** submit large code formatting changes without discussing with the team first.

When you are ready to proceed with making a change, get set up to build (either on [Windows](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md) or on [Unix](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Unix.md)) the code and familiarize yourself with our developer workflow. 

### Reviewing pull requests

Our repository gets a high volume of pull requests and reviewing them all is a significant time commitment. Our team priorities often force us to focus on reviewing a subset of the active pull requests at a given time: the active set. Pull requests that are not in our active set will be placed in the [Backlog milestone](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+is%3Aopen+milestone%3ABacklog+). This is done to make it clear to both the roslyn team and contributors where our effort is focused.

Contributors are free to work on pull requests in the Backlog milestone, solicit feedback, etc ... Our team does value these contributions and will provide guidance and reviews when possible. However our team will typically prioritize reviewing pull requests in the active set over those in Backlog. Further if a particular pull request gets stale it's likely our team will move it into Backlog until the contributor has time to re-engage with the change.

Like issues, pull requests will move in and out of Backlog as priorities change. 

### Feedback

Your pull request will now go through extensive checks by the subject matter experts on our team. Please be patient; we have hundreds of pull requests across all of our repositories. Update your pull request according to feedback until it is approved by one of the Roslyn team members. After that, one of our team members may adjust the branch you merge into based on the expected release schedule.

**During pull request review**
A core contributor will review your pull request and provide feedback.

### Automatic repo rules
To ensure that there is not a large backlog of inactive PRs, the pull request will be marked as stale after two weeks of no activity. After another two weeks, it will be reset  to 'Draft' state.

## Merging pull requests

When your pull request has had all feedback addressed, it has been signed off by one or more reviewers with commit access, and all checks are green, we will commit it.

## Code of conduct

See [CODE-OF-CONDUCT.md](./CODE-OF-CONDUCT.md)
