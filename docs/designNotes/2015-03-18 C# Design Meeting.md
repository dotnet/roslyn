C# Design Meeting Notes for Mar 18, 2015
========================================

Discussion thread for these notes can be found at https://github.com/dotnet/roslyn/issues/1677

Agenda
------

In this meeting we looked over the top [C# language feature requests on UserVoice](http://visualstudio.uservoice.com/forums/121579-visual-studio/category/30931-languages-c) to see which ones are reasonable to push on further in C# 7.



1. Non-nullable reference types (*already working on them*)
2. Non-nullary constructor constraints (*require CLR support*)
3. Support for INotifyPropertyChanged (*too specific; metaprogramming?*)
4. GPU and DirectX support (*mostly library work; numeric constraints?*)
5. Extension properties and static members (*certainly interesting*)
6. More code analysis (*this is what Roslyn analyzers are for*)
7. Extension methods in instance members (*fair request, small*)
8. XML comments (*Not a language request*) 
9. Unmanaged constraint (*requires CLR support*)
10. Compilable strings (*this is what nameof is for*)
11. Mulitple returns (*working on it, via tuples*)
12. ISupportInitialize (*too specific; hooks on object initializers?*)
13. ToNullable (*potentially part of nullability support*)
14. Statement lambdas in expression trees (*fair request, big feature!*)
15. Language support for Lists, Dictionaries and Tuples (*Fair; already working on tuples*)

A number of these are already on the table.



1. Non-nullable reference types
===============================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2320188-add-non-nullable-reference-types-in-c](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2320188-add-non-nullable-reference-types-in-c)

We're already working on this; see e.g. #1648.



2. Non-nullary constructor constraints
======================================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2122427-expand-generic-constraints-for-constructors](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2122427-expand-generic-constraints-for-constructors)

It is odd that we only support the `new()` constraint for empty parameter lists. In order to generalize this, however, we'd need CLR support to express it - see #420.



3. Support for INotifyPropertyChanged
=====================================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2255378-inotifypropertychanged](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2255378-inotifypropertychanged)

This is too specific an interface to bake in special knowledge for in the language. However, we recognize the pain of having to repeat the boilerplate around this, even as we've improved the situation here a bit with C# 6.

We think that this may be better addressed with metaprogramming. While we don't have a clear story for how to support this better, in the language or compiler tool chain, we think that Roslyn in and of itself helps here.

We'll keep watching the space and the specific scenario.



4. GPU and DirectX support
==========================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2730068-greatly-increase-support-for-gpu-programming-in-c](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2730068-greatly-increase-support-for-gpu-programming-in-c)
[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/3646222-enable-hlsl-directx-and-graphics-development-tool](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/3646222-enable-hlsl-directx-and-graphics-development-tool)

These are mostly library-level requests, independent of the language.

One feature that could potentially improve such libraries would be the ability to specify generic constraints that somehow express the presence of numeric operators. Being able to write generic methods, say, over anything that has a `+` operator, allowing `+` to be used directly in that method body, would certainly improve the experience of writing such code, and would prevent a lot of repetition.

Unfortunately, like other new constraints, such a numeric constraint facility would require new support from the CLR.



5. Generalized extension members
================================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2242236-allow-extension-properties](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2242236-allow-extension-properties)
[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2060313-c-support-static-extension-methods-like-f](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2060313-c-support-static-extension-methods-like-f)

The requests for extension properties and for static extension methods are essentially special cases of a general desire to be able to more generally specify extension member versions of all kinds of function members: properties, indexers, constructors, static members - why not?

This is a reasonable request. The main problem we have is that the current scheme for extension methods doesn't easily generalize to other kinds of members. We'd need to make some very clever syntactic tricks to do this without it feeling like a complete replacement of the current syntax.

This is certainly one that we will look at further for C# 7.



6. More code analysis
=====================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/4428274-improve-code-analysis](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/4428274-improve-code-analysis)

This feels like it is best addressed via Roslyn-based analyzers.



7. Extension methods in non-static classes
==========================================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/3359397-allow-extension-methods-to-be-defined-in-instance](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/3359397-allow-extension-methods-to-be-defined-in-instance)

We were very cautious when we first introduced extension methods, and surrounded them with a lot of restrictions. This is a well argued scenario where allowing them inside instantiable classes would enable a fluent style for private helper methods.

This seems fair enough, and we could loosen this restriction, though it's probably a relatively low priority work item. 



8. XML comments
===============

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2709987-xml-comments-schema-customization-in-c](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2709987-xml-comments-schema-customization-in-c)

This is not a language suggestion. 



9. Unmanaged constraint
=======================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/4716089-unmanaged-generic-type-constraint-generic-pointe](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/4716089-unmanaged-generic-type-constraint-generic-pointe)

This would be great in order to enable pointers over type parameters. However, it requires CLR support.



10. Compilable strings
======================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/5592955-compliable-strings](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/5592955-compliable-strings)

This is mostly addressed by `nameof` in C# 6; it's unlikely there is basis for more language level functionality here.



11. Mulitple returns
====================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2083753-return-multiple-values-from-functions-effortlessly](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2083753-return-multiple-values-from-functions-effortlessly)

We're already looking at addressing this through tuples.



12. ISupportInitialize
======================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2094881-add-support-for-isupportinitialize-on-object-initi](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2094881-add-support-for-isupportinitialize-on-object-initi)

This suggestion addresses the need to perform validation upon initialization. While depending on the ISupportInitialize interface is probably too specific, it is interesting to ponder if there is a way to e.g. hook in after an object initializer has run.



13. ToNullable
==============

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2531917-structure-all-nullable-values](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2531917-structure-all-nullable-values)

This suggestion would add a new operator to make type parameters nullable only if they are not already so.

You could certainly imagine something like this in conjunction with at least some variations of the nullability proposals we have been discussing lately.



14. Statement lambdas in expression trees
=========================================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/4255391-let-lambdas-with-a-statement-body-be-converted-to](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/4255391-let-lambdas-with-a-statement-body-be-converted-to)

The language today doesn't allow statement lambdas to be converted to expression trees, despite there being expression tree classes for most features. Moreover, in fact, it disallows several expression forms, including `await` expressions.

This would be a lovely gap to fill, especially since it would come with no conceptual overhead - more code would just work as expected. However, it is also an enormous feature to take on. We are not sure we have the weight of scenarios necessary to justify taking on the full magnitude of this.

It is also possible that we'd address a subset.



15. Language support for Lists, Dictionaries and Tuples
=======================================================

[http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2405699-build-list-dictionary-and-tuple-into-the-language](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/2405699-build-list-dictionary-and-tuple-into-the-language)

We are already looking at tuples, and we do have it on our radar to consider language syntax for lists and dictionaries in some form. Many shapes of this feature would require a strong commitment to specific BCL types. However, you could also imagine having anonymous expression forms that target type to types that follow a given pattern.
