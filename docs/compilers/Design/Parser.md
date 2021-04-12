Parser Design Guidelines
========================

This document describes design guidelines for the Roslyn parsers. These are not hard rules that cannot be violated. Use good judgment. It is acceptable to vary from the guidelines when there are reasons that outweigh their benefits.

![image](https://i0.wp.com/media.tumblr.com/tumblr_lzwm2hKMGx1qhkwbs.gif)

The parsers do not currently comply with these guidelines, even in places where there is no good reason not to. The guidelines were written after the parsers. We expect to refactor the parsers opportunistically to comply with these guidelines, particularly when there are concrete benefit from doing so.

Designing the syntax model (data model for the syntax tree) for new features is currently outside the scope of these guidelines.

#### **DO** have the parser accept input text that complies with the syntax model

The syntax model defines the contract between syntax and semantic analysis. If the parser can build a meaningful syntax tree for a sequence of input tokens, then it should be left to semantic analysis to report when that tree is not semantically valid.

The most important diagnostics to report from the parser are "missing token" and "unexpected token".

There may be reasons to violate this rule. For example, the syntax model does not have a concept of precedence, but the parser uses precedence to decide how to assemble the tree. Precedence errors are therefore reported in the parser.

#### **DO NOT** use ambient context to direct the behavior of the parser

The parser should, as much as possible, be context-free. Use context where needed only to resolve language ambiguities. For example, in contexts where both a type and an expression are permitted, but a type is preferred, (such as the right-hand-side of `is`) we parse `X.Y` as a type. But in contexts where both are permitted, but it is to be treated as a type only if followed by an identifier (such as following `case`), we use look-ahead to decide which kind of tree to build. Try to minimize the amount of context used to make parsing decisions, as that will improve the quality of incremental parsing.  

There may be reasons to violate this rule, for example where the language is specified to be sensitive to context. For example, `await` is treated as a keyword if the enclosing method has the `async` modifier. 

#### Examples

Here are some examples of places where we might change the parser toward satisfying these guidelines, and the problems that may solve:

1. 100l : warning: use `L` for long literals

   It may seem a small thing to produce this warning in the parser, but it interferes with incremental parsing. The incremental parser will not reuse a node that has diagnostics. As a consequence, even the presence of this helpful warning in code can reduce the performance of the parser as the user types in that source. That reduced performance may mean that the editor will not appear as responsive.

   By moving this warning out of the parser and into semantic analysis, the IDE can be more responsive during typing. 

1. Member parsing

   The syntax model represents constructors and methods using separate nodes. When a declaration of the form `public M() {}` is seen, the parser checks the name of the function against the name of the enclosing type to decide whether it should represent it as a constructor, or as a method with a missing type. In this case the name of the enclosing type is a form of *ambient context* that affects the behavior of the compiler.

   By treating such as declaration as a constructor in the syntax model, and checking the name in semantic analysis, it becomes possible to expose a context-free public API for parsing a member declaration. See https://github.com/dotnet/roslyn/issues/367 for a feature request that we have been unable to do because of this problem.