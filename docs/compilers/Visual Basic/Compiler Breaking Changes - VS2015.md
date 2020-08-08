**This document lists known breaking changes in Roslyn (VS2015 and later) from the native VB compiler (VS2013 and previous).**

<!--
*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*
-->

### For VB (Deviations from the Language Specification or Native Compiler)

This list tracks known cases where the Roslyn compiler intentionally deviates from the language specification or previous versions. It includes both breaking changes and changes which enable things which would not be permitted by either the language specification or the native compiler. The bug numbers refer to Microsoft-internal bug databases. The checklist marks our progress moving the necessary documentation into this repository.

- [ ] When specifying the output extension as one of the known output types (example /out:foo.dll), but specifying a different target type
   (example /t:exe) the compiler will now keep the specified extension and won't add the target extension (example foo.dll.exe). See
   [#13681](https://github.com/dotnet/roslyn/issues/13681) for examples and details.
- [ ] Comments are now allowed after implicit line continuation.
- [ ] Breaking Change: Unicode characters U+30FB and U+FF65 no longer parse as valid identifier characters (Bug 3246).
- [ ] Breaking Change: Unicode character escape sequences no longer parse within a preprocessor directive keyword (Bug 1433).
- [ ] Breaking Change: Protected members of one generic construction of a type are now accessible from other constructions of that type; overload resolution may change (Bug 4107).
- [ ] Breaking Change: Unicode character U+180E (Mongolian Vowel Separator) no longer parses as valid identifier character (Bug 4189).
- [ ] Breaking Change: Explicit line-continuation at the end of comments is now ignored in disabled text (Bug 4450).
- [ ] Breaking Change: Explicit line-continuation immediately after a hash token at the beginning of a line  is now ignored in disabled text (Bug 4452).
- [ ] Breaking Change: Inherits clauses in Interfaces no longer resolve names dependent on resolution of previous Inherits clauses (Bug 4536).
- [ ] Breaking Change: Decimal literals with more than 29 decimal digits of precision may round differently for mid-point values (Bug 4870).
- [ ] Breaking Change: Extension method lookup no longer skips declaration spaces which would produce an ambiguous overload resolution (Bug 8965).
- [ ] Breaking Change: Preprocessing directive after label are no longer parsed (Bug 8982).
- [ ] Breaking Change: Extension Method resolution no longer searches modules imported at the file level before searching containing types and namespaces (Bug 8943).
- [ ] Breaking Change: Extension method candidates whose names differ in case are no longer excluded from otherwise ambiguous overload resolution candidate sets (Bug 10689).
- [ ] Breaking Change: Partial classes may no longer be used to extend VB Core runtime types (Bug 12427).
- [ ] Breaking Change: VB Core members which are unused in an assembly are now removed; referencing assemblies which depend on these members through the InternalsVisibleToAttribute will break (Bug 12449).
- [ ] Breaking Change: When choosing between a lifted nullable operator and a source operator which requires unwrapping a nullable type the lifted nullable operator is preferred (Bug 12953).
- [ ] Breaking Change: Changes to left operand of an AndAlso or OrElse expression which occur when evaluating the right operand are no longer considered when computing the final result of the expression (Bug 13031).
- [ ] Breaking Change: Delegate relaxation is no longer permitted when using Handles in cases where relaxation would require ignoring only some (rather than all) parameters of the target delegate signature (Bug 13578).
- [ ] Breaking Change: BC30826 is now consistently reported whenever the EndIf keyword is used in either normal code or preprocessing directives (Bug 13687).
- [ ] Breaking Change: VB Runtime types embedded with VBCore are now marked NotInheritable (Bug 13775).
- [ ] Breaking Change: Event handlers registered with Handles are now registered deterministically based on their lexical order (Bug 13880).
- [ ] Breaking Change: BC42105 is now reported when any reachable code paths of a function returning any value types (not just intrinsics) fails to return a value (Bug 14121).
- [ ] Breaking Change: Overrides of Shadows methods will cause shadowed members to become visible through late-bound name lookup (Bug 14733).
- [ ] Breaking Change: BC42335 is now more consistently reported when a type inference failure for an array literal passed as an argument in a generic extension method call causes type argument inference for the call to fail (Bug 15274).
- [ ] Breaking Change: BC30113 is now reported when specifying the /rootnamespace option passing a name that contains multiple consecutive dots (Bug 15581).
- [ ] Breaking Change: Expression tree conversions of lambda expressions no longer succeed when an explicit conversion to a delegate type rather than an expression tree type is specified in source (Bug 16795).
- [ ] Breaking Change: MarshalAsAttribute applied to the return type of a 'WriteOnly' interface property is now correctly copied to the last parameter of the 'Set' accessor rather than the first (which may not always be the 'value' parameter) (Bug 619823).
- [ ] Breaking Change: Instance properties declared in metadata which have Shared (static) accessors or Shared properties with instance accessors are now consider invalid and unreferencable (Bug 528038).
- [ ] Breaking Change: Passing an empty string as the value of the /doc switch to vbc.exe is no longer considered a valid equivalent of /doc:- (Bug 531020).
- [ ] Breaking Change: BC2032 is now reported when passing an invalid filename as the value of the /doc switch to vbc.exe (Bug 531021).
- [ ] Breaking Change: BC36534 is now reported when an expression tree conversion is attempted on a unary plus or minus expression which invokes a lifted user-defined operator with non-nullable argument and a nullable return type (Bug 531424).
- [ ] Breaking Change: Element type inference for an array literal used as one of the result values of a ternary 'If' expression now correctly considers the element type of the other result value (Bug 563487).
- [ ] Breaking Change: BC30742 is now consistently reported when invoking the Asc function with vbNullString as its argument whether that invocation appears as an expression or a statement (Bug 574290).
- [ ] Breaking Change: BC30068 is now correctly reported when using a captured range variable as a For Each control variable (Bug 575055).
- [ ] Breaking Change: Logical negations of equality comparison expressions are no longer condensed into inequality comparisons when converted to expression trees (Bug 531513).
- [ ] Breaking Change: BC36595 is now correctly reported when the 'On Error Resume Next' statement appears in a method which also contains a variable captured by a lambda or query expression (Bug 531560).
- [ ] Breaking Change: Enumerations declared in referenced assemblies which contain fields of types other than the underlying type of the enumeration are now considered invalid and unreferencable (Bug 571038).
- [ ] Breaking Change: BC31210 is now reported when a partial type declared in user code conflicts with VB runtime types embedded using VBCore (Bug 568936).
- [ ] Breaking Change: BC36534 is now reported when an expression tree conversion is attempted on binary expression which invokes a lifted user-defined operator with non-nullable arguments and a nullable return type (Bug 531423).
- [ ] Breaking Change: BC40007 is now correctly reported when a default property declaration conflicts with a default property declaration in a non-immediate base class (Bug 530872).
- [ ] Breaking Change: XML namespace attributes (xmlns:<prefix>) specified on XML literal expressions are always sorted before other attributes on the resultant XElement nodes and their textual representation (Bug 531504).
- [ ] Breaking Change: XML literal expressions may not produce the same output with regard to order of elements, attributes, or the presence or absence of redundant namespace prefixes as in previous versions (Bug 569075).
- [ ] Breaking Change: When binding an AddressOf expression, otherwise ambiguous overloads where one overload has no required parameters may no longer be resolved by preferring the overload with no required parameters (Bug 531430).
- [ ] Breaking Change: When evaluating operands of a binary expression operands are now consistently evaluated in left-to-right order (Bug 634745).
- [ ] Breaking Change: Attribute name resolution now ignores all non-type members when evaluating qualified names; this may lead to different results when binding previously accessed nested types through Shared members (Bug 528671).
- [ ] Breaking Change: Overload resolution between otherwise equally applicable overloads of a method which both accept ParamArray parameters now correctly prefers the candidate requiring fewer arguments to be passed in the parameter array (Bug 629484).
- [ ] Breaking Change: InvalidOperationException is now correctly thrown, rather than NullReferenceException, when converting a null (nullable (value-type-constrained type-parameter type)) value to its underlying type (Bug 574611).
- [ ] Breaking Change: BC30516 is now reported when constructing a COM interface instance via a CoClass where the arguments passed don't resolve to any particular constructor of the CoClass type or any are passed and the interface is an embedded interop type (Bug 665954).
- [ ] Breaking Change: Declaring a default property in a derived type now causes all properties of the same name in any base types to be considered default property overloads, even those not originally declared as Default properties (Bug 531592).
- [ ] Breaking Change: BC42308 is now reported when using the <paramref> tag in an XML documentation comment without specifying the 'name' attribute (Bug 658605).
- [ ] Breaking Change: BC36969 is now reported when attempting to declare a Partial constructor (Bug 699668).
- [ ] Breaking Change: BC42305 is now reported when tags which are required to be unique appear more than once in the same documentation comment (Bug 669574).
- [ ] Breaking Change: BC30512 is now reported when an implicit conversion of a boxed value to a nullable structure is required to invoke a user-defined operator (Bug 529642).
- [ ] Breaking Change: Canonical interop type definitions may no longer be redefined in the compilation in which they are being embedded; BC31539 is now reported instead (Bug 675850).
- [ ] Breaking Change: BC30652 may be reported when invoking overloaded methods, properties, or constructors where one or more overloads depend on types in unreferenced assemblies (Bug 708169).
- [ ] Breaking Change: BC42307 is now reported whenever a <param> tag appears in a documentation comment which does not match a parameter on the corresponding declaration, regardless of whether the tag is nested in other tags (Bug 681924).
- [ ] Breaking Change: Full-Width hash tokens are now recognized when starting preprocessing directives inside of disabled text (Bug 658448).
- [ ] Breaking Change: Anonymous type names appearing in metadata may differ from those generated by previous versions of the compilers (727118).
- [ ] Breaking Change: "long form" metadata signatures are no longer supported. CLI Part II 23.2.16.(Bug vstfdevdiv\DevDiv2\DevDiv 741391) 
  long-form: (ELEMENT_TYPE_CLASS, TypeRef-to-System.String )
  short-form: ELEMENT_TYPE_STRING
- [ ] Breaking Change: U+07F4, the 'NKO HIGH TONE APOSTROPHE', is no longer considered a valid comment start character after pre-processing directives appearing in disable text; syntax errors may result (Bug 584727).
- [ ] Breaking Change: CS3024 is now consistently reported for generic types whose type parameters are constrained to non-CLS compliant types (Bug 761339).
- [ ] Breaking Change: Import aliases now correctly take precedence over members of imported namespaces (Bug 763620).
- [ ] Breaking Change: Documentation comment crefs applied to delegate declarations may no longer refer to members of the delegate through simple unqualified names (Bug 805309).
- [ ] Breaking Change: The dominant type of all elements from all literals is now used when inferring a single-type for multiple array literals (e.g. If expressions, jagged arrays, ParamArrays) (Bug 826101).
- [ ] Breaking Change: BC30910 is now reported when a Public class in a Friend Assembly attempts to inherit a Friend class defined in another (Bug 810799).
- [ ] Breaking Change: BC30456 is now reported if an object member initializer of an instantiated CoClass interface type refers to a member of the CoClass type not defined on the interface (Bug 672416).
- [ ] Breaking Change: Type names with the -Attribute suffix are now correctly preferred to type names without the suffix when resolving attribute names, even when those types are not valid attribute types (Bug 879792).
- [ ] Breaking Change: Assemblies and types not correctly annotated with the System.Runtime.CompilerServices.ExtensionAttribute are no longer included when searching for extension methods (Bug 530908).
- [ ] Breaking Change: Reflection may report different results for the declaring type of methods generated by lambdas which do not capture any variables from their containing scope (GitHub #1983).
- [ ] #Region/#End Region statements are now permitted within method bodies and to cross method boundaries (Bug 3382).
- [ ] End {Operator|RaiseEvent|RemoveHandler|Set|Sub|Function|Get|AddHandler} statements are no longer required to be the first statement on a physical line (Bugs 3228, 3229, 3230, 3231, 3232, 3386, 3387, 3388).
- [ ] Several bugs previous reported as parse errors are now reported as semantic errors (e.g. duplicate modifiers, modifiers invalid in context).
- [ ] XML Documentation comments now allow the </> token to close elements (Bug 1271).
- [ ] Roslyn compiler no longer unifies local types with canonical types within the assembly being compiled (Bug 1457).
- [ ] Implicit line-continuation now allowed between Group and Join keywords in Group Join clause (Bug 998).
- [ ] The language specification implies that implicitly declared variable (if Option Explicit Off is in effect) are not allowed anywhere in a field initializer. Dev10 allows them in lambda in field initializers, exception for single-line Function lambdas. Roslyn allows them also in single-line Function lambdas.
- [ ] BC30277 is no longer reported for type character mismatches at the declaration site (Bug 9012).
- [ ] BC30098 is no longer reported when setting a read-write property in an attribute specifier of an attribute imported from metadata which overrides only the get accessor of a base class property (Bug 10469).
- [ ] BC40007 is reported for changes in the default property name involving both indirect and direct base types. It used to only be reported for directly inherited types.
- [ ] Partial method declarations may now contain comments and pre-processing directives (Bug 12013).
- [ ] BC31146 is no longer reported when whitespace appears around the XML name in the GetXmlNamespace operator; the whitespace is now ignored (Bug 607560).
- [ ] Generated event accessors are no longer emitted with the 'synchronized' metadata flag but instead contain inline synchronization instructions within their bodies (Bug 13465).
- [ ] BC2014 is no longer reported when a double-quoted comma-delimited list of warning numbers is specified for the vbc /nowarn switch (Bug 15172).
- [ ] BC2001 is no longer reported when the name of a hidden file is specified (Bug 15173).
- [ ] BC31396 is no longer reported when the ArgIterator type appears as a type argument to a late-bound method invocation (Bug 568937).
- [ ] EmbeddedAttribute is now consistently applied to the InternalXmlHelper class embedded into user assemblies which make use of XML literals as well as the assemblies themselves regardless of whether VBCore is being used (Bug 17155).


### Rules of constant expressions in VB

According to @AlekseyTs , "Dev10 considers some expressions as invalid in context of a constant expression, even if those expressions are not going to be evaluated. Lambdas, Query Expressions, and probably Object Initializers fall into that category."

These rules are being relaxed in Roslyn.  A constant expression will be allowed of the form expr.ID where ID is a constant member of the type of the expression.  The expression will not be evaluated and a warning will be issued.  There are no restrictions on the form of expr, other than that it must be syntactically and semantically valid.
