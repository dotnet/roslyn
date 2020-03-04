`private protected` access modifier
=================================

We propose to add a new access modifier combination `private protected` (which can appear in any order among the modifiers). This maps to the CLR notion of protectedAndInternal, and borrows the same syntax currently used in [C++/CLI](https://msdn.microsoft.com/en-us/library/ke3a209d.aspx#BKMK_Member_visibility).

A member declared `private protected` can be accessed within a subclass of its container if that subclass is in the same assembly as the member.

We modify the language specification as follows (additions in bold). Section numbers are not shown below as they may vary depending on which version of the specification it is integrated into.

-----

> The declared accessibility of a member can be one of the following:
- Public, which is selected by including a public modifier in the member declaration. The intuitive meaning of public is “access not limited”.
- Protected, which is selected by including a protected modifier in the member declaration. The intuitive meaning of protected is “access limited to the containing class or types derived from the containing class”.
- Internal, which is selected by including an internal modifier in the member declaration. The intuitive meaning of internal is “access limited to this assembly”.
- Protected internal, which is selected by including both a protected and an internal modifier in the member declaration. The intuitive meaning of protected internal is “accessible within this assembly as well as types derived from the containing class”.
- **Private protected, which is selected by including both a private and a protected modifier in the member declaration. The intuitive meaning of private protected is “accessible within this assembly by types derived from the containing class”.**

-----

> Depending on the context in which a member declaration takes place, only certain types of declared accessibility are permitted. Furthermore, when a member declaration does not include any access modifiers, the context in which the declaration takes place determines the default declared accessibility. 
- Namespaces implicitly have public declared accessibility. No access modifiers are allowed on namespace declarations.
- Types declared directly in compilation units or namespaces (as opposed to within other types) can have public or internal declared accessibility and default to internal declared accessibility.
- Class members can have any of the five kinds of declared accessibility and default to private declared accessibility. [Note: A type declared as a member of a class can have any of the five kinds of declared accessibility, whereas a type declared as a member of a namespace can have only public or internal declared accessibility. end note]
- Struct members can have public, internal, or private declared accessibility and default to private declared accessibility because structs are implicitly sealed. Struct members introduced in a struct (that is, not inherited by that struct) cannot have protected **,** ~~or~~ protected internal **, or private protected** declared accessibility. [Note: A type declared as a member of a struct can have public, internal, or private declared accessibility, whereas a type declared as a member of a namespace can have only public or internal declared accessibility. end note]
- Interface members implicitly have public declared accessibility. No access modifiers are allowed on interface member declarations.
- Enumeration members implicitly have public declared accessibility. No access modifiers are allowed on enumeration member declarations.

-----

> The accessibility domain of a nested member M declared in a type T within a program P, is defined as follows (noting that M itself might possibly be a type):
- If the declared accessibility of M is public, the accessibility domain of M is the accessibility domain of T.
- If the declared accessibility of M is protected internal, let D be the union of the program text of P and the program text of any type derived from T, which is declared outside P. The accessibility domain of M is the intersection of the accessibility domain of T with D.
- **If the declared accessibility of M is private protected, let D be the intersection of the program text of P and the program text of any type derived from T, which is declared outside P. The accessibility domain of M is the intersection of the accessibility domain of T with D.**
- If the declared accessibility of M is protected, let D be the union of the program text of T and the program text of any type derived from T. The accessibility domain of M is the intersection of the accessibility domain of T with D.
- If the declared accessibility of M is internal, the accessibility domain of M is the intersection of the accessibility domain of T with the program text of P.
- If the declared accessibility of M is private, the accessibility domain of M is the program text of T.

-----

> When a protected **or private protected** instance member is accessed outside the program text of the class in which it is declared, and when a protected internal instance member is accessed outside the program text of the program in which it is declared, the access shall take place within a class declaration that derives from the class in which it is declared. Furthermore, the access is required to take place through an instance of that derived class type or a class type constructed from it. This restriction prevents one derived class from accessing protected members of other derived classes, even when the members are inherited from the same base class.

-----

> The permitted access modifiers and the default access for a type declaration depend on the context in which the declaration takes place (§9.5.2):
- Types declared in compilation units or namespaces can have public or internal access. The default is internal access.
- Types declared in classes can have public, protected internal, **private protected**, protected, internal, or private access. The default is private access.
- Types declared in structs can have public, internal, or private access. The default is private access.

-----

> A static class declaration is subject to the following restrictions:
- A static class shall not include a sealed or abstract modifier. (However, since a static class cannot be instantiated or derived from, it behaves as if it was both sealed and abstract.)
- A static class shall not include a class-base specification (§16.2.5) and cannot explicitly specify a base class or a list of implemented interfaces. A static class implicitly inherits from type object.
- A static class shall only contain static members (§16.4.8). [Note: All constants and nested types are classified as static members. end note]
- A static class shall not have members with protected **, private protected, ** or protected internal declared accessibility.

> It is a compile-time error to violate any of these restrictions. 

-----

> A class-member-declaration can have any one of the ~~five~~ **six** possible kinds of declared accessibility (§9.5.2): public, **private protected**, protected internal, protected, internal, or private. Except for the protected internal **and private protected** combination**s**, it is a compile-time error to specify more than one access modifier. When a class-member-declaration does not include any access modifiers, private is assumed.

-----

> Non-nested types can have public or internal declared accessibility and have internal declared accessibility by default. Nested types can have these forms of declared accessibility too, plus one or more additional forms of declared accessibility, depending on whether the containing type is a class or struct:
- A nested type that is declared in a class can have any of ~~five~~ **six** forms of declared accessibility (public, **private protected**, protected internal, protected, internal, or private) and, like other class members, defaults to private declared accessibility.
- A nested type that is declared in a struct can have any of three forms of declared accessibility (public, internal, or private) and, like other struct members, defaults to private declared accessibility.

-----

> The method overridden by an override declaration is known as the overridden base method For an override method M declared in a class C, the overridden base method is determined by examining each base class type of C, starting with the direct base class type of C and continuing with each successive direct base class type, until in a given base class type at least one accessible method is located which has the same signature as M after substitution of type arguments. For the purposes of locating the overridden base method, a method is considered accessible if it is public, if it is protected, if it is protected internal, or if it is **either** internal **or private protected** and declared in the same program as C.

-----

> The use of accessor-modifiers is governed by the following restrictions:
- An accessor-modifier shall not be used in an interface or in an explicit interface member implementation.
- For a property or indexer that has no override modifier, an accessor-modifier is permitted only if the property or indexer has both a get and set accessor, and then is permitted only on one of those accessors.
- For a property or indexer that includes an override modifier, an accessor shall match the accessor-modifier, if any, of the accessor being overridden.
- The accessor-modifier shall declare an accessibility that is strictly more restrictive than the declared accessibility of the property or indexer itself. To be precise:
  - If the property or indexer has a declared accessibility of public, the accessor-modifier may be either **private protected**, protected internal, internal, protected, or private.
  - If the property or indexer has a declared accessibility of protected internal, the accessor-modifier may be either **private protected**, internal, protected, or private.
  - If the property or indexer has a declared accessibility of internal or protected, the accessor-modifier shall be **either private protected or** private.
  - **If the property or indexer has a declared accessibility of private protected, the accessor-modifier shall be private.**
  - If the property or indexer has a declared accessibility of private, no accessor-modifier may be used.

-----

> Since inheritance isn’t supported for structs, the declared accessibility of a struct member cannot be protected, **private protected**, or protected internal.

-----

