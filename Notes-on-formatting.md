**This document is work-in-progress (jcouv)**

While working on formatting for recursive-patterns (PR [#26555](https://github.com/dotnet/roslyn/pull/26555)), I've hit a number of issues. I'm capturing some notes of what I learnt from Heejae and others. That should provide some working knowledge of how to edit and troubleshoot rules, without having very deep knowledge of the formatting engine itself.

We'll start with an overview, then some scenarios, then some troubleshooting tips:

## Overview

The formatting is driven by multiple rules. The rules are chained in a certain order, and unless a decision was already made, the next rule gets to inspect each token pair (current token and previous token) and produce an operation. The engine collects all those operations of different kinds and applies them.

There are operations for spacing, for newlines (and newline suppressions), for indenting, for anchoring, and probably other ones.

## Scenarios

### Spacing

The default rule is to add spaces between every pair of tokens. Then there are rules to customize that (mostly in `TokenBasedFormattingRule.cs` and `SpacingFormattingRule.cs`). Some are driven by user-controlled options.

![image](https://user-images.githubusercontent.com/12466233/39937938-2a56e9a0-5506-11e8-9c60-1b684ab7aba2.png)

Ex: spacing in method declaration is affected by options.
![image](https://user-images.githubusercontent.com/12466233/39938013-60956d48-5506-11e8-9619-c9eef2f8de39.png)

`CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)`
`CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces)`
`CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)`
`CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)`
`CreateAdjustSpacesOperation(space: 0, option: AdjustSpacesOption.PreserveSpaces)`




### TODO
some operations apply to spans
newlines and suppressions
indentation (and the relation to newlines)
gif for anchoring
Triggers for formatting (user can trigger, brace completion, end-of-line or other special characters?)
brace completion
smart token formatter (place the cursor in the right default position after enter is pressed)