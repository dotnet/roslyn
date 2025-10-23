Bound Node Design
=================

This document discusses design principles for the bound nodes.

### The shape of the bound tree should correspond to the shape of the program's static semantics

Generally speaking, that means that there is an isomorphism between the syntax and bound nodes, except when there is a mismatch between the shape of the syntax and semantics, in which case they model the shape of the semantics.  When possible, we prefer the correspondence be as direct as possible.  Here are two examples that illustrate this:
1. Parenthesized expressions do not appear in the bound nodes because they have no semantic meaning.
2. Query expressions are given a semantic meaning by correspondence to a translated form, so the bound nodes may model the translated form.

Default visit order for bound nodes should match order of evaluation, which usually matches lexical order.

### Bound nodes should capture all semantic information embedded in the syntax

A consumer of the bound nodes should not need to examine the syntax from which they were produced to understand the meaning of the bound nodes.  All relevant semantic information that comes from the syntax should be summarized in the fields of the bound node.  If a consumer of a bound node needs to refer to the syntax to affect the meaning of the code, that is a design smell.

### Every bound node type is either abstract or sealed

In `BoundNodes.xml`, a `Node` should have a base that is `BoundNode` or a node type declared `AbstractNode`. Do not inherit a concrete node type from another concrete node type.
