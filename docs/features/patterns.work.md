This is a checklist (moved from #9375) of implementation of pattern matching as specified in [patterns.md](./patterns.md). For reference a previous prototype was at https://github.com/semihokur/pattern-matching-csharp

Open design issues (needing LDM decisions)
- [ ] There would be an ambiguity with a hypothetical "type pattern" and the constant pattern. This is the reason we do not allow the latter in an is-pattern expression, and don't allow the former in a sub-property pattern. But that is irregular. Can we come up with name lookup rules that support both?

  ```
    class Color { }
    class D
    {
        const int Color = 3;
        void f(object x) { if (x is Color) Console.WriteLine("c"); }
        // Current name-lookup-rules mean that this "Color" refers to the type, not the constant
        // I bet that folks would like to pattern-match on constants, e.g.
        // if x is Message { PayloadKind is OnedrivePayloadKinds.Request, Body is string req } ...
    }
  ```
  - [ ] If we support `3 is 3`, is that a constant expression? Can a `match` expression be constant?
- [ ] Do we want pattern-matching in the `switch` statement? Or do we want a separate statement-based construct instead? (#8821)
    - [ ] What does `goto case` mean?
      - [ ] Do we limit `goto case` to constants?
      - [ ] Or do we allow a non-constant expression in a `goto case` statement?
      - [ ] If limited to constants, must its value match an existing constant label?
      - [ ] Can a constant `case` label with a `when` clause be the target of `goto case`?
    - [ ] How do we write the rules for the switch-expression (conversion) and matching labels (i.e. constant patterns) so that it approximates the current behavior?
    - [ ] Do we allow multiple cases in the same `switch` block if one of them defines a pattern variable?
    
    ```
    Expr IdentitySimplification(Expr src)
    {
      switch (src)
      {
         case Sum(0,i):
         case Sum(i,0):
            return i; // this is pretty darn slick!
         case Mul(1,i):
         case Mul(i,1):
            return i;
         default:
            return src;
      }
    }
    ```
- [ ] What syntax do we want for the `match` expression? (#8818)
  - [ ] Does a `match` expression have to be complete? If not, what happens? Warning? Exception? Both?
  - [ ] What about the `case` expression?
- [ ] Under what condition do we give diagnostics for pattern-matching based on subsumption or completion? See http://www.cs.tufts.edu/~nr/cs257/archive/norman-ramsey/match.pdf for a discussion of implementation approaches. We probably need to select (and specify?) an approach.
- [ ] Should we combine the property pattern with the type pattern so as to allow giving the object a name? If so, what syntax?
- [ ] What do we think about the let statement?
  - [ ] Do we require the match be irrefutable? If not, do we give a warning? Or let definite-assignment issues alert the user to any problems?
  - [ ] Do we support an else?
  - [ ] Is there a compatibility issue? (e.g. if the user has a type named `let`) 
- [ ] There are some scoping questions for pattern variables. #9452
  - [ ] Need to get LDM approval for design change around scope of pattern variables declared within a constructor initializer #9452 
  - [ ] Also questions about multiple field initializers, local initializers, ctor-initializers (how far does the scope extend?)
- [ ] Need detailed spec for name lookup of property in a property pattern #9255
  - [ ] [Pattern Matching] Should a property-pattern be capable of referencing an event? #9515
- [x] Two small clarifications need to be integrated into the spec (#7703)
- [ ] We need to specify and implement the user-defined pattern forms: user-defined `operator is`?
  - [ ] static void with self-receiver
  - [ ] static bool for active patterns
  - [ ] instance bool for captured data (regex)
- [ ] Do we want to support "breakpoints inside" patterns (#9095)?
- [ ] Do we want to support named "arguments" in recursive patterns? e.g. `if (p is Point(X: 3, Y: 4)) ...`
- [ ] What is the correct precedence of *throw-expression*? Should *assignment* be allowed as its sub-expression?
- [ ] @jaredpar suggested that, by analogy with the integral types, we should match floating-point literal patterns across floating-point types.
- [ ] Should we allow throw expression on right of && and || ? #9453
- [ ] Should `operator is` be allowed to take a ref first parameter, for example for value types? Is the answer the same as for all other operators?
- [ ] We need to specify the meaning of the things we need the decision tree for: subsumption, completeness, irrefutable.
- [ ] Will we support pattern matching with anonymous types?
- [ ] Will we support pattern matching with arrays?
- [ ] Will we support pattern matching with List<T>, Dictionary<K, V>? Think particularly of F#'s really useful list deconstruction, which feels impossible with IEnumerable since patterns shouldn't generally be executing methods like GetEnumerator/MoveNext.
- [ ] How should the type `dynamic` interact with pattern matching?
- [ ] In what situations should the compiler be required by specification to warn about a match that must fail? E.g. switch on a `byte` but case label is out of range?
- [ ] Should it be possible to name the whole matched thing in a property pattern, like `case Point p { X is 2 }:` ?
- [ ] Should it be possible to omit the type name, if the switch statement is statically known, to make it easier to write?

  ```
  Rectangle r;
  if (r is Rectangle { Width is 100, Height is var height }) 
  if (r is { Width is 100, Height is var height }) // slicker
  ```
- [ ] Will the fact that `null` doesn't match against any type be confusing? This will often force folks to use `var`, even when their preferred coding style is to always use explicit types. I can see why this behavior is implied by the current behavior of "is", and it makes sense when doing checks based on runtime-type, but it feels ugly when doing pattern-matching in cases where all types are statically known.
  ```
  (int i, string s) t = (5, null);
  if (t is (5,var s1)) ... // matches
  if (t is (5,string s1)) .. // doesn't match
  ```
- [ ] We've seen simple cases for Point, but will (user-defined pattern-matching operators) in practice produce readable code?

Implementation progress checklist:
- [x] Allow declaration of `operator is` and use it for recursive patterns.
- [ ] **Match constant patterns with appropriate integral conversions** (#8819)
- [ ] **Add a decision tree** to enable
  - [ ] completeness checking: a mutli-armed pattern-matching expression is required to be complete
  - [ ] subsumption checking: a branch of a switch statement or match expression may not be subsumed by the totality of previous branches (#8823)
  - [ ] irrefutable: a pattern that *always* matches given its context.
  - [ ] Generate efficient code like `switch` does in corresponding situations. (#8820)
- [ ] Test `operator is` across assembly boundaries.
- [x] Scoping for variables introduced in patterns (binders)
- [x] `SemanticModel.GetDeclaredSymbol` for pattern variable declarations.
- [x] Simple pattern matching expressions `expression is Type Identifier` in most statements.
- [x] Extend the parser to handle all of the other specified pattern-matching operations.
  - [x] Add tests for the parser, including precedence tests for the edge cases.
  - [ ] Augment `TestResource.AllInOneCSharpCode` to handle all pattern forms.
- [x] Check for feature availability in the parser (error if feature not supported).
  - [ ] Do not generate any new syntax nodes in C# 6 mode. 
- [x] Error pattern matching to a nullable value type
- [x] Implement pattern matching to a type that is an unconstrained type variable (requires a double type test)
- [ ] Semantics and code-gen for all pattern forms
  - [x] Type ID
  - [x] *
  - [x] 3
    - [x] matching with exact type for integral constants (as a short-term hack)
  - [x] `var` ID
  - [x] Type { ID is Pattern ... }
  - [x] Type ( Pattern ... )
- [ ] Extend the switch statement to handle patterns
  - [x] Parser
  - [x] Syntax Tests
  - [x] Binding
  - [ ] Binding (failure cases) tests
  - [x] Flow analysis
  - [x] Lowering
  - [x] Code-gen tests
- [x] An expression form for mutli-armed pattern-matching (`match`?)
- [ ] Extend the scope of a pattern variable declared in a catch filter to the catch block. (#8814)
- [ ] Implement and test pattern variable scoping for all statements (#8817)
  - [ ] Test for error on reusing a variable name, and lambda-capturing.
- [ ] Test for name conflicts with locals in enclosing scopes for normal and "odd" contexts.
- [ ] Need a custom diagnostic for accessing a static property in a property pattern. Are there other contexts where the diagnostics need improvement?
- [ ] Data-flow analysis and region analysis should be modified to handle pattern variables, which are definitely assigned when a pattern match succeeds.
  - [ ] Region analysis APIs versus pattern matching #9277 
  - [ ] Can't extract method on case expression in match/case clause. #9105
  - [ ] Consider pattern matching for extract method scenarios #9244
  - [ ] PreciseAbstractFlowPass doesn't override visit for BoundMatchCase and BoundConstantPattern nodes #9422 
- [ ] Control-flow analysis should be modified to handle patterns that either always match or never match.
- [ ] Lots more Tests and code coverage; #9542
  - [ ] Tests for error cases in a property pattern, such as when the named member
    - [x] Does not exist
    - [x] Is static
    - [ ] Is an event
    - [ ] Is a method
    - [ ] Is an indexed property
    - [ ] Is a nested type
    - [x] Is inaccessible
    - [ ] Is ambiguous
    - [ ] Does not exist
- [ ] `IOperation` support for pattern-matching (#8699)
- [ ] Some unit tests that were disabled during development need to be re-enabled (#8778)
- [ ] WRN_UnreferencedVarAssg "The variable '...' is assigned but its value is never used" is not reported for pattern variables #9021
- [ ] ERR_BadEmbeddedStmt "Embedded statement cannot be a declaration or labeled statement" is not reported for a [let] statement #9029
- [ ] Unexpected ERR_UseDefViolation error reported within an 'if' block #9121
- [ ] Unexpected ERR_UseDefViolation error #9154
- [ ] Internationalize diagnostics for pattern-matching #9283
- [ ] SymbolInfo for bad property in a property pattern should contain candidate symbols #9284
- [ ] Compiler crash with match expressions in lambda analysis. #9430
- [x] PatternVariableFinder is lacking explicit visibility modifier #9530
- [x] PatternVariableFinder doesn't follow style conventions for field names #9531
- [ ] Test code coverage of pattern-matching implementation. #9542

IDE Features
- [ ] 'let' not offered in statement context. #9083
- [ ] After 'let' no completion to allow you specify the type. #9084
- [ ] 'when' and 'else' not offered when typing a 'let' declaration.
- [ ] Rename cannot be triggered from a `let` declaration name. #9086
- [ ] Rename of a 'let' variable reference produce 'unresolvable conflicts' at every location. #9088
- [ ] Generate type not offered for complex pattern type. #9089
- [ ] Generate field/property not offered for complex pattern member. #9090
- [ ] QuickInfo on 'let' declaration shows nothing. #9091
- [ ] No formatting for match/case expressions. #9094
- [ ] Indentation on colon not working with case clauses in a match/case expression. #9098
- [ ] 'throw' not offered in expression contexts. #9099
- [ ] No Property name completion in a complex pattern #9231

Possible positional patterns inferred by constructor
- [ ] Reorder parameters does not fix up use sites in patterns. #9100
- [ ] Signature help not offered in positional pattern. #9101
- [ ] can't extract method on condition in match/case clause. #9106
- [ ] Find references on a constructor does not find usages in a position pattern. #9107
- [ ] Generate constructor not offered on position pattern. #9108

Related features possibly to be added at the same time as pattern-matching:
- [x] #5154 expression-based switch ("match")
- [x] #5143 make "throw expression" an expression form
- [ ] #6183 "out var" declarations
- [ ] #188 Completeness checking for "match" and Algebraic Data Types
- [x] #6400 destructuring assignment (let statement)
- [ ] #206 Record types
- [ ] #5172 "with" expressions
