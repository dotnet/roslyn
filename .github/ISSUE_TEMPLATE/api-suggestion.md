---
name: API proposal
about: Propose a change to the public API surface
title: ''
labels: [Feature Request, Concept-API]
assignees: ''

---

## Background and Motivation

<!--
We welcome API proposals! We have a process to evaluate the value and shape of new APIs. There is an overview of our process [here](https://github.com/dotnet/roslyn/blob/main/docs/contributing/API%20Review%20Process.md). This template will help us gather the information we need to start the review process.
First, please describe the purpose and value of the new API here.
-->

## Proposed API

<!--
Please provide a sketch of the public API signature diff that you are proposing. Be as specific as you can: the more specific the proposal, the easier the process will be. An example diff is below.
You may find the [Framework Design Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/framework-design-guidelines-digest.md) helpful.
https://github.com/dotnet/roslyn/issues/53410 is a good example issue.
-->

```diff
namespace Microsoft.CodeAnalysis.Operations
{
     public class ISwitchExpressionOperation
     {
+        public bool IsExhaustive { get; }
     }
```

## Usage Examples

<!--
Please provide code examples that highlight how the proposed API additions are meant to be consumed.
This will help suggest whether the API has the right shape to be functional, performant and useable.
You can use code blocks like this:
-->

``` C#
// some lines of code here
```

## Alternative Designs

<!--
Were there other options you considered, such as alternative API shapes?
How does this compare to analogous APIs in other ecosystems and libraries?
-->

## Risks

<!--
Please mention any risks that to your knowledge the API proposal might entail, such as breaking changes, performance regressions, etc.
-->
