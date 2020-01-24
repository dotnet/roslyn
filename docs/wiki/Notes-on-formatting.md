While working on formatting for recursive-patterns (PR [#26555](https://github.com/dotnet/roslyn/pull/26555)), I've hit a number of issues. I'm capturing some notes of what I learnt from Heejae and others. That should provide some working knowledge of how to edit and troubleshoot rules, without having very deep knowledge of the formatting engine itself.

We'll start with an overview, then some scenarios, then some troubleshooting tips.

## Overview

The formatting is driven by multiple rules. The rules are chained in a certain order, and unless a decision was already made, the next rule gets to inspect each token pair (current token and previous token) and produce an operation. The engine collects all those operations of different kinds and applies them.

There are operations for spacing, for newlines (and newline suppressions), for indenting, and for anchoring.

## Scenarios

### Spacing

The default rule is to add spaces between every pair of tokens. Then there are rules to customize that (mostly in `TokenBasedFormattingRule.cs` and `SpacingFormattingRule.cs`). Some are driven by user-controlled options.

![image](https://user-images.githubusercontent.com/12466233/39937938-2a56e9a0-5506-11e8-9c60-1b684ab7aba2.png)

Ex: spacing in method declaration is affected by options.
![image](https://user-images.githubusercontent.com/12466233/39938013-60956d48-5506-11e8-9619-c9eef2f8de39.png)

- `CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)` (forces no space)
- `CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces)` (forces exactly 1 space)
- `CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)`
- `CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)`
- `CreateAdjustSpacesOperation(space: x, option: AdjustSpacesOption.PreserveSpaces)` (set a minimum of `x` spaces, but leave any extra spaces that the user typed)

### Newlines

- `CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)`: indicates a place for a newline, but doesn't require one. 

Without this, the next line would not indented/aligned if a newline is present. Such an optional newline is present after the semi-colon terminating a statement. So two statements can remain on the same line, but if they are separated by a newline, then the second statement is indented relative to the containing block.

- `CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines)`: indicates a place for a newline (minimum of one). Such mandatory newline is present after the colon terminating a `case` label.

- `AddSuppressWrappingIfOnSingleLineOperation`

Example of typing `if () {}`:

The rules for breaking lines inside the block of an `if` are conditionally suppressed if the two braces are on the same line.

![image](https://user-images.githubusercontent.com/12466233/45203182-298bc400-b230-11e8-9f53-4818e44f1fcf.png)

But as soon as the braces are separated onto different lines and you re-format, then the suppression is lifted and more newlines take effect.

![newline-suppression](https://user-images.githubusercontent.com/12466233/45204277-8dfc5280-b233-11e8-9f1e-f1106013424b.gif)

### Indentation
The simple case is to declare a span to be indented from the containing scope.

This is done with `AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(list, brace)`.

As noted above, you need newlines (ie. known to the formatting engine, not just a newline character in source) for indentation to kick in.

### Anchoring

Anchoring a block to a token means that if formatting moves the token, then the block will be moved along with the token.
This is done with `AddAnchorIndentationOperation(list, anchor)`.

This is illustrated by the argument list for a method invocation. In both screen captures below, the formatting will cause the method name to be indented properly. As you can see, the arguments will just shift along with it, retaining their relative position (the arguments are anchored to the open parenthesis).

![anchoring1](https://user-images.githubusercontent.com/12466233/45203634-a0758c80-b231-11e8-9835-e8a289672b52.gif)

![anchoring2](https://user-images.githubusercontent.com/12466233/45203640-a3707d00-b231-11e8-82a9-b56db538ea66.gif)

## Troubleshooting

In my experience, it was not fruitful (or even necessary) to debug through the engine itself. It's very easy to get lost in the chain of rules. A better approach is to set breakpoints at a few strategic locations (TODO need to list a few). Conditional breakpoints are particularly useful, as you can choose to break only when the rule is considering a token of interest (for example, you can use `currentToken.ToString == "{"`).

From what I've seen so far, issues that I found while manually testing in the IDE fell into two categories:
- formatting (those are issues where you tweak some code, invoke formatting, and don't like the result). Those can be repro'ed in `FormattingTests.cs`.
- typing (braces and cursor placement). Those can be repro'ed in `AutomaticBraceCompletionTests.cs` (simulates typing `{` and getting a completion, then typing `enter` and getting a new formatted output as well as a cursor placement).


### TODO
- some operations apply to spans
- Triggers for formatting (user can trigger, brace completion, end-of-line or other special characters?)
- brace completion
- note that in some scenarios, there isn't a need for formatting the whole document, so some operations that are deemed irrelevant will be discarded.
- smart token formatter (place the cursor in the right default position after enter is pressed)