**This document lists known breaking changes in Roslyn (VS2015 and later) from the native C# compiler (VS2013 and previous).**

<!-- 
*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").*

Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*
-->

### (C#) Deviations from the Language Specification or Native Compiler

This page tracks known cases where the Roslyn compiler intentionally deviates from the previous versions or the language specifications. It includes both breaking changes and changes which enable things which would not be permitted by either the language specification or the native compiler. Bug numbers without links refer to internal Microsoft bug tracking systems.

- In some cases, due to a bug in the native compiler, programs with pointers to structs with one or more
   type parameters compiled without error. All such programs should now produce errors in Roslyn. See
   [#5712](https://github.com/dotnet/roslyn/issues/5712) for examples and details.
- When calling a method group with only instance methods in a static context with dynamic arguments, the
   native compiler generated no warnings/errors and emitted code that would always throw when executed.
   Roslyn produces an error in this situation. See [#11341](https://github.com/dotnet/roslyn/pull/11341) for when this decision was made,
   [#11256](https://github.com/dotnet/roslyn/pull/11256) for when it was discovered, and [#10463](https://github.com/dotnet/roslyn/issues/10463) for the original issue that led to this.
- Native compiler used to generate warnings (169, 414, 649) on unused/unassigned fields of abstract classes.
   Roslyn 1 (VS 2015) didn't produce these warnings. With [#14628](https://github.com/dotnet/roslyn/pull/14628) these warnings should be reported again.
- In preview and RC releases of VS 2017 and C#7.0, deconstruction was allowed with a Deconstruct method that returned something (non-void return type).
   Starting in RC.3, the Deconstruct method is required to return void. This will allow for future versions of C# to attach special semantics to the return value.
   See issue [#15634](https://github.com/dotnet/roslyn/issues/15634) and PR [#15922](https://github.com/dotnet/roslyn/pull/15922) for more details.
- Breaking Change: Unicode characters U+30FB and U+FF65 no longer parse as valid identifier characters (Bug 3246). This is due to a change in the Unicode specification.
- Breaking Change: Unicode character escape sequences no longer parse within a preprocessor directive keyword (Bug 1433).
- Breaking Change: Unicode character U+180E (Mongolian Vowel Separator) no longer parses as valid identifier character (Bug 4189).
- Breaking Change: Unicode formatting characters are no longer considered when comparing preprocessing constant identifiers for equality (Bug 4361).
- Breaking Change: If a lambda converted to a parameter type of an overload containing symbols from an unreferenced assembly that overload candidate is no longer excluded (Bug 6171).
- Breaking Change: Dependencies in name resolution between base type specifiers will now result in an error (Bug 3189).
- Breaking Change: Overload resolution now considers more specific constructions of contravariant delegate types better than less specific ones (Bug 6302).
- Breaking Change: Ambiguous types are no longer ignored when detecting an ambiguity between an attribute name with and without the -Attribute suffix (Bug 9923).
- Breaking Change: XML documentation comment parsing no longer allows the interleaving of single-line (///) and multi-line (/**) comment styles (Bug 1895).
- Breaking Change: Formatting characters are now ignored when comparing pre-processing constant identifiers (Bug 6415).
- Breaking Change: Obsolete ! arity suffix in metadata is no longer supported (Bug 10264).
- Breaking Change: CS0185 is now reported when locking on a value of a type parameter type that is not known to be a reference type (Bug 10755).
- Breaking Change: CS0185 is now reported when locking on a value of a type parameter type that is known to be a value type (Bug 10756).
- Breaking change: A trailing null character is no longer allowed in indexer name (Bug 10421).
- Breaking Change: CS0131 is now reported when attempting assign to a property or indexer of a value expression when the type of the value is a type parameter known to be a value type (Bug 9484).
- Breaking Change: CS1612 is now reported when attempting to mutate the return value of a property access or method invocation when the type of the value is a type parameter known to be a value type (Bug 9836).
- Breaking Change: CS1939 is now consistently reported when passing a range variable as an 'out' or 'ref' parameter (Bug 12030=529267).
- Breaking Change: Destructor declarations no longer override non-destructor "Finalize" methods declared in base classes (Bug 11372).
- Breaking Change: CS0266 is now reported when invoking a user-defined increment or decrement operator would require performing a implicit narrowing conversion on the operand, the number 1, or the result of the invocation (Bug 12046).
- Breaking Change: CS0029 is now reported when attempting to implicitly convert a value of a nullable type parameter type to another type parameter type which could be the non-nullable form of the same type when constructed (Bug 12106).
- Breaking Change: When choosing a user-defined conversion to a nullable type in a null coalescing expression an implicit conversion to the underlying type of the nullable is preferred over an explicit conversion to the nullable type itself (Bug 12506).
- Breaking Change: An assignment expression in the right operand of a null coalescing expression is no longer considered definite when the left operand is an assignment expression whose right operand is a null literal (Bug 12508, 529363).
- Breaking Change: Reference equality operator is no longer allowed in constant expressions (Bug 13138).
- Breaking Change: CS0591 is now reported when the dllName or EntryPoint values specified for DllImportAttribute or the MarshalType or MarshalCookie values specified for MarshalAsAttribute contain embedded null characters or unpaired surrogates (Bug 12909).
- Breaking Change: CS0208 is now consistently reported whenever a pointer to a managed type appears in code (Bug 12469).
- Breaking Change: CS0034 is now reported when attempting to subtract the null literal from a nullable enum value (Bug 13358).
- Breaking Change: CS0165 is now reported when a variable is accessed within an if statement which is uninitialized prior to the if statement even when the condition of the if statement is a ternary expression in which the variable is "definitely assigned when true" in either branch (Bug 13704).
- Breaking Change: CS0472 is now reported when comparing any non-nullable value type to the null literal (Bug 13826).
- Breaking Change: Well-known attributes (e.g. Obsolete, Conditional) are now recognized by namespace, name, and shape without regard to containing assembly; behavior may change where these attributes were applied but not recognized before (Bug 15015).
- Breaking Change: Producing multiple output files by providing a sequence of command-line argument groups in a single invocation of the command-line compiler is no longer supported (Bug 15049).
- Breaking Change: When transitively referencing multiple versions of an assembly which are then unified, MemberRef tokens emitted into the output assembly now reflect the directly referenced assembly version (Bug 15580).
- Breaking Change: CS1683, CS1684 are now reported as errors rather than warnings (Bug 15603).
- Breaking Change: When using the /nostdlib option the compiler now enforces required well-known types or members are defined and well-formed rather than omitting generated members or instructions adaptively (Bug 15914).
- Breaking Change: Attribute type 'using' directive aliases which lack the -Attribute suffix no longer shadow suffixed type names; ambiguities between attributes imported from multiple namespaces which were previously hidden are now correctly reported (Bug 16042).
- Breaking Change: Redundant identity conversion nodes are no longer generated in expression trees when a comparison between a null literal and a value type causes a lifted comparison and no explicit identity cast exists in source (Bug 16342).
- Breaking Change: Formatting characters are no longer considered when comparing identifiers for equivalence (Bug 16471).
- Breaking Change: Binary expressions which require a user-defined conversion of one of the operands to a nullable type now correctly generate a lifted binary expression tree node rather than throwing InvalidOperationException at runtime (Bug 16671).
- Breaking Change: Well-known types may now be declared in source; such declarations will be preferred over well-known types defined in metadata (Bug 16753).
- Breaking Change: Unused locals whose names match those of their types and whose type names are used in the containing scope to qualify nested types or static members are now recognized as unused; CS0219, CS0168 are now correctly reported (Bug 17395, 1162136).
- Breaking Change: CS0135 is now more consistently reported (Bug 16055).
- Breaking Change: Iterator finally blocks which throw exceptions are no longer executed twice (Bug 16470).
- Breaking Change: The use of a type name to qualify a nested type or static member of the type in a scope containing a field with that name and of that type is no longer incorrectly considered a use of the field (Bug 16583).
- Breaking Change: CS0464 is now reported instead of CS0458 when using a user-defined relative comparison operator to compare a value type with the null literal (Bug 18049).
- Breaking Change: Nested expressions within an initializer of a fixed statement are no longer permitted to take the address of the object being fixed; CS0212 is now reported (Bug 529549).
- Breaking Change: CS0019 is now reported when both operands of the null-coalescing operator (??) are null literals (Bug 529566).
- Breaking Change: Native imports invalid properties (accessor with less restricted modifier) from netmodule as separate get and set methods (Bug 530766).
- Breaking Change: CS1980 is now consistently reported when System.Runtime.CompilerServices.DynamicAttribute is required by the compiler but its definition is missing any members, even if those members are themselves not required (Bug 531108).
- Breaking Change: CS0121 is now reported in binding an 'await' expression when invoking GetAwaiter() on the awaited expression explicitly would produce an ambiguity (Bug 576847).
- Breaking Change: CS0675 is now consistently reported when Bitwise-or operator is used on a sign-extended operand (Bug 620823).
- Breaking Change: Accessing an enum member through a simple type name which also could resolve to a member field of the same name and type as the enumeration no longer suppresses warning CS0414 (Bug 531378).
- Breaking Change: Arrays of type dynamic[], empty or otherwise, are no longer considered valid constant expressions in attribute specifications (Bug 552859).
- Breaking Change: CS1657 is now reported when using 'ref' to pass a method group as the argument to a delegate creation expression (Bug 531610).
- Breaking Change: Multiple XML Documentation Comments specified using the /*\* */ syntax are no longer merged before validating their contained XML is well-formed (616980).
- Breaking Change: CS1658 is now reported when an XML Documentation Comment cref attribute does not use valid member name syntax or is not well-formed XML (Bug 612181).
- Breaking Change: CS0518 is now reported when passing /NoStdLib switch to csc.exe while compiling an assembly which declares its own System.Object type but references other assemblies (Bug 530359).
- Breaking Change: Otherwise ambiguous overloads where the types of specified parameters do not exactly match may no longer be resolved by preferring the overload which requires the use of fewer default argument values (Bug 531434).
- Breaking Change: CS0165 is now reported when using a variable of a structure type defined in metadata which contains only private reference type fields before initializing the variable (Bug 631463).
- Breaking Change: CS0618 is now reported when initializing an obsolete field in an object member initializer (Bug 641128).
- Breaking Change: CS0618 is now reported when accessing an event defined in metadata that is marked with ObsoleteAttribute (Bug 641112).
- Breaking Change: Generic structure type references encoded in metadata as customer modifiers on System.ValueType are no longer supported; CS0570 is now reported when consuming members which use such references as parameters or return types (Bug 631452).
- Breaking Change: The overload resolution tie-breaker preferring the candidate that requires passing fewer default argument values is now correctly applied only after determining the types of the supplied parameters to be equivalent (Bug 620903).
- Breaking Change: CS1730 is now reported when an assembly or module level attribute target is used to when applying an attribute to a member (Bug 528676).
- Breaking Change: Named arguments may no longer be parenthesized; syntax errors may result (Bug 602168).
- Breaking Change: TypeForwardedTo attributes applied to netmodules are now properly exported and validated when that netmodule is compiled into an assembly, CS0012 errors may be reported (Bug 654825).
- Breaking Change: When attempting to pass a dynamic argument to an instance method without having an instance as the receiver CS0120 is now reported at compile time rather than the call throwing an exception at runtime (Bug 657938).
- Breaking Change: When attempting to pass a pointer-type argument to an dynamically-bound method invocation CS1978 is now reported at compile time rather than the call throwing an exception at runtime (Bug 669104).
- Breaking Change: CS7086 is now reported when a referenced module's filename doesn't match its name as embedded in metadata (Bug 686703).
- Breaking Change: CS1570 is now reported when multiple documentation comments applied to the same member are not each independently well-formed (Bug 616980).
- Breaking Change: Custom security attributes deriving from SecurityAttribute are now correctly emitted as such (Bug 529619).
- Breaking Change: CS4003 is now reported when 'await' is used as an identifier inside of a non-async lambda nested within an async method or lambda expression (Bug 640295).
- Breaking Change: The "least generic" overload resolution tie-breaker rule is now correctly applied before the "least default arguments" tie-breaker (C# Language Specification, §7.5.3.2) (Bug 653849)
- Breaking Change: The "more declared parameters" overload resolution tie-breaker rule is now correctly applied before the "least default arguments" tie-breaker (C# Language Specification, §7.5.3.2) (Bug 689615).
- Breaking Change: CS1930 is now reported when introducing a new range variable in a join ... into ... clause with the same name as a previously defined range variable in the same query without an intervening select ... into ... clause (Bug 633575).
- Breaking Change: CS0453 is now reported when defining an alias to a constructed generic type in a using statement whose type arguments do not meet the constraints of the generic type definition, regardless of whether that alias is ever used (Bug 670983).
- Breaking Change: CS1056 is now reported whenever a null character (U+0000) appears in source outside of a character or string literal (Bug 714410).
- Breaking Change: When calling methods in referenced assemblies which depend on embedded interop types, MethodRef tokens emitted into the output assembly now refer to the canonical interop types rather than the embedded local types (Bug 707277).
- Breaking Change: CS7023 is now reported when the type operand of an 'is' expression is a static type (Bug 720297).
- Breaking Change: CS7086 is now reported when the module name stored in a referenced module does not match its filename (Bug 720788).
- Breaking Change: CS8003 is now reported when the key specified with AssemblySignatureKeyAttribute is now well-formed regardless of whether the assembly being compiled is configured to be signed (Bug 721514).
- Breaking Change: CS0012 is now reported when referencing a member whose type or parameter types depend on ModOpt types declared in unreferenced assemblies (Bug 722567).
- Breaking Change: Anonymous type names appearing in metadata may differ from those generated by previous versions of the compilers (727118).
- Escaped identifiers in attribute target specifiers (which are not valid) now produce a new, more specific warning (Bug 2591).
- Breaking Change: CS1734 is now reported when a <paramref> tag in a documentation comment applied to a read only property refers to the 'value' parameter (Bug 738679).
- Breaking Change: CS0208 is now generated when the type of a 'ref' or 'out' parameter is or contains a pointer to a managed type (Bug 737715).
- Breaking Change: "long form" metadata signatures are no longer supported. CLI Part II 23.2.16. (Bug vstfdevdiv\DevDiv2\DevDiv 741391)
      long-form: (ELEMENT_TYPE_CLASS, TypeRef-to-System.String )
      short-form: ELEMENT_TYPE_STRING
- Breaking Change: Extension methods defined in assemblies which do not have an assembly-level Extension attribute applied are no longer recognized (Bug 656365).
- Breaking Change: Custom core libraries which reference other assemblies are no longer recognized; CS0518 may be reported when attempting to consume system types declared in such libraries (Bug 741406).
- Breaking Change: Managed resources cannot be added or linked when building a module. (DevDiv2/DevDiv 759256)
- Breaking Change: Adding a module with managed resources to an assembly being built will no longer promote the resources from the added module to the primary module. 
- Breaking Change: Use of short-circuiting forms of user-defined '&' or '|' operators now correctly requires that both corresponding 'true' and 'false' operators be declared on the same type declaring the '&' or '|'; otherwise CS0218 is now reported (Bug 770424).
- Breaking Change: CS1700 is now reported when the 'assemblyName' argument of an InternalsVisibleToAttribute specification includes null characters (Bug 770437).
- Breaking Change: CS1010 is now generated when the final closing quote of a #pragma checksum directive is omitted (Bug 770700).
- Breaking Change: Overload resolution candidates which do not require binding to obsolete members within lambda expressions are no longer preferred to candidates which do (Bug 775932).
- Breaking Change: Unicode character U+200B (ZERO WIDTH SPACE) no longer parses as valid whitespace (Bug 789624).
- Breaking Change: Unicode character U+205F (MEDIUM MATHEMATICAL SPACE) no longer parses as a valid identifier character (Bug 794847).
- Breaking Change: Interlocked.CompareExchange method is now required when declaring field-like events; errors will be reported when declaring such events while targeting platforms on which this member is not present (Bug 528573).
- Breaking Change: CS0837 is now reported when the first operand of an 'is' or 'as' expression is a method group (Bug 864605).
- Breaking Change: In XML Documentation comments, crefs can no longer refer to inaccessible members (Bug 568006).
- Breaking Change: Bad assembly version number now gets a new warning number due to a more specific diagnostic. (Bug 746685)
- Breaking Change: Destructor declarations no longer override non-destructor "Finalize" methods declared in base classes (Bug DevDiv 529119)
- Breaking Change: Reflection may report different results for the declaring type of methods generated by lambdas which do not capture any variables from their containing scope (GitHub #1983).
- Breaking Change: CS0012 is now reported when referencing declarations from metadata which use custom modifiers (ModOpt/ModReq) defined in unreferenced assemblies (Bug 1116450).
- XML Documentation comments now allow the </> token to close elements (Bug 1271).
- Roslyn compiler no longer unifies local types with canonical types within the assembly being compiled (Bug 1457).
- CS0023 is no longer reported when using the unary +, -, and ~ operators in front of a null literal (Bug 5605).
- CS1560 is no longer reported when filenames in #line directives exceed 255 characters in length (Bug 8920).
- Resolving overloads which differ only in params parameter at a call site which uses named arguments no longer produces an ambiguity (Bug 10754).
- Implicit conversions which resolve unambiguously in implicit contexts no longer resolve ambiguously in explicit contexts (Bug 11202).
- CS0609 is no longer reported when IndexerNameAttribute is applied to an indexer override (Bug 12894).
- CS0266 is no longer reported when mixing multi-dimensional array index expression types between int and ulong (Bug 12999).
- CS0219 is no longer reported when a value type variable is acquired or declared as the resource of a using statement (Bug 13485).
- CS2022 is no longer reported when specifying '/out' or '/target' after source file names when invoking csc.exe (Bug 15049).
- CS0591 is no longer reported when the value of an AttributeUsageAttribute contains set bits beyond the AttributeTargets.All mask; instead these bits are ignored (Bug 16409).
- As in the native compiler, unsafe code is not allowed anywhere in an iterator method (including in lambdas).  There is a special error code (CS1629).
- Shim methods generated by the compiler when implementing interface members with base class members now correctly copy the OutAttribute, if specified, and parameter names from the implemented interface member (Bug 531294).
- Values of constrained type parameter types may now make use of user-defined conversion operators defined on their effective base types (Bug 530651).
- 'Add' methods bound in object collection initializers may now be extension methods (Bug 602206).
- CS3019 is no longer reported for any members contained in assembly to which CLSCompliant(false) has been applied (Bug 741655).
- Floating-point literals are now rounded to the nearest value per IEEE spec, where previous compilers sometimes produced values off by 1 least significant bit.

### C# Spec deviation: Logical operator overload resolution

from DevDiv #656739: 

``` cs
using System;
class InputParameter
{
    public static implicit operator bool(InputParameter inputParameter)
        {
        throw null;
        }
    public static implicit operator int(InputParameter inputParameter)
        {
        throw null;
        }
}
class Program
{
    static void Main(string[] args)
        {
                InputParameter i1 = new InputParameter();
                InputParameter i2 = new InputParameter();
        bool b = i1 || i2;
        }
}
```

The C# spec, section 7.12, says "An  operation of the form x && y or x || y is processed by  applying overload resolution (§7.3.4) as if the  operation was written x & y or x | y."

Prior to DevDiv #656739, Roslyn reported that || was ambiguous in the example above because | would be.  Unfortunately, dev11 only treats || as | when considering user-defined operators (or when one of the operands is dynamic).  That is, for built-in operators, it distinguishes between || and |.  Since there is no || operator for ints, there is no ambiguity.
### C# Spec deviations: new expressions of enum types can have a constant value

Bug 9510: Error CS0182 (An attribute argument must be a constant expression) should not be issued for 'new' expressions of enum types

As per the specification, new expressions of enum types cannot have a constant value. However native compiler violates this for paramterless value type constructors and Roslyn maintains compatibility by constant folding paramterless value type constructors. This applies also to enum types.
### C# spec deviation: New Int32() treated as constant 0

```
            // DELIBERATE SPEC VIOLATION:
            // The spec does not allow "new int()" to be treated as a constant 
            // The native compiler treats "new int()" (and so on) as a "zero" constant expression, 
            // despite the fact that the specification does not include object creation expressions on the
            // list of legal constant expressions.
```
### C# spec deviation: Parameterless Constructor

See bug 4424

``` cs
class C
{
    public C(params int[] x) { }
}
class D : C
{
}
```

**ACTUAL RESULT**: error CS1729: 'C' does not contain a constructor that takes 0 arguments
**EXPECTED RESULT**: no errors (although the spec requires exactly parameterless ctor, Dev10 and previous versions always supported any ctors that can be invoked with an empty argument list)
### C# spec deviation: Parenthesized null

According to the spec, this is illegal:

``` cs
Object o = (null);
```

Because the null conversion doesn't apply to parenthesized null expressions, only null literals.

Maybe similar issues with lambdas.
### C# spec deviation: ambiguous lookup in multiply-inherited interface

Looking at the spec, the enclosed program would appear to be an error because of the following part of 7.4 (member lookup):
 
    · Finally, having removed hidden members, the result of the lookup is determined:
        o If the set consists of a single member that is not a method, then this member is the result of the lookup.
        o Otherwise, if the set contains only methods, then this group of methods is the result of the lookup.
        o Otherwise, the lookup is ambiguous, and a binding-time error occurs.
 
However, the Dev10 compiler accepts the code and gives only a warning that there is an ambiguity and it is using the method group rather than the property.
 
Roslyn uses the property and ignores the method (thus giving a type error in this particular case).
 
Do you think we should make all three (spec, Dev10, and Roslyn) behave the same?

``` cs 
    delegate void MyAction<T>(T x);
     
    interface I1
    {
        object Y { get; }
    }
     
    interface I2
    {
        void Y(long l);
    }
     
    interface I3 : I1, I2 { }
     
    public class Program : I3
    {
        object I1.Y
        {
            get
            {
                return null;
            }
        }
     
        void I2.Y(long l) { }
     
        public static void Main(string[] args)
        {
            I3 p = new Program();
            MyAction<long> o = p.Y; // lookup is ambiguous?
            long l = 12;
            o(l);
        }
    }
```
### C# spec deviation: Dev10 will not report unreachable empty statements (C#)

Dev10 doesn't report the following unreachable statement, and so we don't either (see bug 3552):

``` cs
class Program
{
    static void Main()
    {
        if (false)
        { // this block is unreachable
        }
    }
}
```

More precisely, Dev10 will not report when an "empty" statement, a throw statement, or a block statement itself is unreachable.  However, Dev10 will complain if an unreachable block contains some other kind of (unreachable) statement.
### C# spec deviation: Static class as an interface method's parameter (C#)

Dev10 allows a static class to be used as the type of an interface method parameter and an interface method return type.  This is disallowed by the language spec (10.1.1.3.1) but permitted by the Dev10 compiler.

We follow suit in Roslyn so as not to break compatibility with (probably useless) existing programs.
### C# spec deviation: null??null and other null expressions

            // NOTE: We extend the specification here. The C# 3.0 spec does not describe
            // a "null type". Rather, it says that the null literal is typeless, and is
            // convertible to any reference or nullable type. However, the C# 2.0 and 3.0
            // implementations have a "null type" which some expressions other than the
            // null literal may have. (For example, (null??null), which is also an
            // extension to the specification.)
### C# Spec Deviation: parenthesized lambda expression

Technically, in the specification a parenthesized lambda expression is not an anonymous function expression (and is therefore never legal).  However, Dev10 and Roslyn both ignore the parens (except for precedence), accepting code such as the following:

``` cs
var tmp = (Func<int,int>)( x => x+1 );
```
### Method hiding and overload resolution

The C# language spec says that hidden methods are removed early in the overload resolution process.  However, that is false.  The enclosed program (which works in Dev11 and Roslyn) demonstrates that methods with the same signature are not hidden.

``` cs
    using System;
     
    class Base
    {
        public void M(int x) { Console.WriteLine("Base.M(x:" + x + ")"); }
    }
     
    class Derived : Base
    {
        public new void M(int y) { Console.WriteLine("Derived.M(y:" + y + ")"); }
    }
     
    public class Test
    {
        public static void Main()
        {
            Derived d = new Derived();
            d.M(x: 1);
            d.M(y: 2);
        }
    }
```

Clearly this behavior does not agree with the specification, and has been in the product long enough that it probably makes more sense to adjust the specification.
 
This is related to Roslyn bug 12989.
### Reserved Members

The native compiler (improperly) extended the C# language concept of reserved members to type parameters.  For example, the following code is rejected by the native compiler:

``` cs
        public abstract class Foo<Item>
        {
            public Item this[int arg] { get { return default(Item); } }
        }
 
    Error: The type 'Foo<Item>' already contains a definition for 'Item' 
```

Roslyn does not reject this code.
### Compiling primitive types

The source for the special type Int32 is a struct that contains a member of its own type. That is not allowed by the C# language specification, yet the compiler must allow it and properly compile that source to produce metadata for that primitive type.

Same for other primitive types.
### Right operand of ?? operator is not required to be definitely assigned if left operand is non-null constant

The following test case should “pass” to match Dev10 behavior:

``` cs
    using System;
    static class Program
    {
        static void Main()
        {
            const string x = "pass";
            string y;
            string z = x ?? y;
            Console.WriteLine(z);
        }
    }
```

However, this is not justified by the current language specification.  Since Dev10 accepts this code, I recommend we modify the specification to allow this.

### Roslyn requires more assembly references than previous compilers

Roslyn does not ignore method overloads that reference symbols from assemblies to which the compilation lacks an assembly reference. If one attempts to use a method group containing such a method, for example in overload resolution, such a method was ignored in prior compilers. In VS2015 an error CS0012 is given. See [#9370](https://github.com/dotnet/roslyn/issues/9370)

### Roslyn sometimes requires the use of the index operator when accessing a VB indexed property

While the Roslyn C# compiler permits the elision of the index operator of an indexed property (declared, for example, in VB) when the property can be invoked with no parameters (e.g., it has a parameter with a default value), the C# compiler will not permit a (second) indexing of the result unless the first index operation was explicit. This is a breaking change from C# 5, which permitted the elision of the indexer that had default parameters. See [#17045](https://github.com/dotnet/roslyn/issues/17045).

