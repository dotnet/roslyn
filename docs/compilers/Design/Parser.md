Parser Design Guidelines
========================

This document describes design guidelines for the Roslyn parsers. These are not hard rules that cannot be violated. Use good judgment. It is acceptable to vary from the guidelines when there are reasons that outweigh their benefits.

![image](https://i0.wp.com/media.tumblr.com/tumblr_lzwm2hKMGx1qhkwbs.gif)

The parsers do not currently comply with these guidelines, even in places where there is no good reason not to. The guidelines were written after the parsers. We expect to refactor the parsers opportunistically to comply with these guidelines, particularly when there are concrete benefit from doing so.

Designing the syntax model (data model for the syntax tree) for new features is currently outside the scope of these guidelines.

#### **DO** have the parser accept input text that complies with the syntax model

The syntax model defines the contract between syntax and semantic analysis. If the parser can build a meaningful syntax tree for a sequence of input tokens, then it should be left to semantic analysis to report when that tree is not semantically valid.

The most important diagnostics to report from the parser are "missing token" and "unexpected token".

There may be reasons to violate this rule. For example, the syntax model does not have a concept of precedence, but the parser uses precedence to decide how to assemble the tree. Precedence errors are therefore reported in the parser.  As the syntactic shape of the tree is impacted here and we do not want to form illegal tree, diagnostics at this layer are sensible.

However, diagnostics should be avoided for cases where the syntactic model can be easily fitted to, but the rule is effectively a semantic one driven by external context.  A good example of this are 'language version' checks.  Outside of exceptional cases (like 'record parsing') parsing is not actually affected by language-version.  Instead, the lang version simply states if the construct can be used or not, not if it was recognized and parsed successfully into the syntax model.  These checks should happen later, like in the decl-table or binding passed of the compiler (see the DOs/DON'Ts section of this document below for reasons why).  Note: an acceptable reason to still keep these checks at the parser level would be to avoid a simple and direct parsing check turning into a 'smeared out peanut butter' check higher up (e.g. where perhaps dozens of locations might need checks added).  In that case, the negative cost to compiler maintainability would outweigh the positive benefits we get elsewhere.

#### **DO NOT** use ambient context to direct the behavior of the parser

The parser should, as much as possible, be context-free. Use context where needed only to resolve language ambiguities. For example, in contexts where both a type and an expression are permitted, but a type is preferred, (such as the right-hand-side of `is`) we parse `X.Y` as a type. But in contexts where both are permitted, but it is to be treated as a type only if followed by an identifier (such as following `case`), we use look-ahead to decide which kind of tree to build. Try to minimize the amount of context used to make parsing decisions, as that will improve the quality of incremental parsing.  

There may be reasons to violate this rule, for example where the language is specified to be sensitive to context. For example, `await` is treated as a keyword if the enclosing method has the `async` modifier. 

#### Impact of not following these DOs/DON'Ts

The reasons for the above DOs/DON'Ts are not about perceived purity of the syntax or parser designs.  Rather, they have significant impact on both maintainability and correctness of the compiler and on the perf higher up parts of the stack that directly consume the syntax model (with the IDE being the primary consumer here).  Specifically:

1. The presence of ambient context greatly impacts the ability to do incremental parsing properly.  Ambient context must be tracked in some fashion and incremental reuse across disparate contexts leads to violating the invariant that incremental reparsing produces the same tree as normal parsing.  This can and lead to fundamental brokenness that is only solved by a host restart.  As an example, consider an edit that adds 'async' to a method.  This must necessarily affect reuse of nodes within the method as they may be parsed separately.  Every piece of ambient context can have this effect and it dramatically makes it harder to reason about incremental edits and can lead to subtle and hard to diagnose or repro incremental errors in the wild.

2. The presense of diagnostics beyond just `"missing token" and "unexpected token"` has impact again to the incremental parser.  Incremental parsing cannot reuse nodes that have errors in them, as it does not know what the root cause of the error was and if it would be fixed by an edit in a disparate part of the file.  As such, any additional diagnostic forces complete reparsing of that construct and all parent nodes above it.  This adds excess time to incremental parsing and can cause higher memory churn as well as new nodes must be created.  Both of these problems directly impact downstream consumption in IDE scenarios.  In the IDE typeing latency is a high value SLA target we need to meet.  However, many features must operate within the typing window in a manner that is 'correct' wrt to the syntax tree.  This includes, but is not limited to featuers like brace-insertion as well as indentation.  Both of these must happen near-instantiously to the user.  And both need an up-to-date SyntaxTree to determine what to do properly.  As such, these trees are retrieved synchronously using incremental parsing, so as little extra work as possible must be done so we can fit all the remainder of the work into the time-slice available.  Additional diagnostics interferes with this, adding extra unnecessary CPU and causing memory churn.

3. Many IDE features take a view that they must not do more harm than good.  A core approach for that is to assume that if the tree is not well-formed, that adjusting it syntactically will likely lead to problems.  This impacts features like auto-formatting which do not want to make a bad situation worse.  By limiting diagnostics to `"missing token" and "unexpected token"` as much as possible, it becomes much easier for these higher level features to ask the question: "is the tree broken in a way that should stop me from proceeding?"  However, if the tree fits the syntactic model fine, but contains *other* diagnostics unrelated to the viability of the syntactic shape (like semantic questions of "is this being used in the wrong version of the language) then higher level features are unnecessarily disabled.  Updating these features to use compiler error codes is brittle and effectively ties them not to APIs that can be trivially queried, but opaque information about diagnostics (like where they are located and why they were emitted).  This is similar to the problem that incremental parsing faces, just for a higher level component.

#### Examples

Here are some examples of places where we might change the parser toward satisfying these guidelines, and the problems that may solve:

1. 100l : warning: use `L` for long literals

   It may seem a small thing to produce this warning in the parser, but it interferes with incremental parsing. The incremental parser will not reuse a node that has diagnostics. As a consequence, even the presence of this helpful warning in code can reduce the performance of the parser as the user types in that source. That reduced performance may mean that the editor will not appear as responsive.

   By moving this warning out of the parser and into semantic analysis, the IDE can be more responsive during typing. 

1. Member parsing

   The syntax model represents constructors and methods using separate nodes. When a declaration of the form `public M() {}` is seen, the parser checks the name of the function against the name of the enclosing type to decide whether it should represent it as a constructor, or as a method with a missing type. In this case the name of the enclosing type is a form of *ambient context* that affects the behavior of the compiler.

   By treating such as declaration as a constructor in the syntax model, and checking the name in semantic analysis, it becomes possible to expose a context-free public API for parsing a member declaration. See https://github.com/dotnet/roslyn/issues/367 for a feature request that we have been unable to do because of this problem.
