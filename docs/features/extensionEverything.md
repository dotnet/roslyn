## Extension Everything

Basic syntax:

    extension class ExtClass : OtherType
    {
    }

* Must be `class`, `OtherType` must not be extension class.
* Modifiers allowed on a class with `extension`: access modifiers, `unsafe`, `partial`
* Modifiers not allowed: `abstract`, `sealed`, `static`
* Unresolved question: NamedTypeSymbols are all fine. What about arrays, pointers, and type parameters? (`dynamic` is not allowed) - Also, what about static classes? (and only allowing static extensions) - e.g. `Math.Sinf` (float math library)

Members defined on ExtClass behave like they're defined on OtherType (with some exceptions).

The member kinds currently planned on being supported are:

* Instance methods (same as old extension methods)
* Static methods
* Instance properties
* Static properties
* Indexers
* Constructors (possibly)

Other member kinds are valid to be defined in an extension class, but do not project onto the extended class. Such kinds are:

* Static fields
* Nested class (maybe?)

Other kinds are forbidden and produce an error:

* Instance fields
* Events
* Old-style extension methods
* Static constructors
* Methods with `override`, etc.

I believe there's debate on whether properties with set should be allowed. (`{ get; set; }` and `{ set; }`, but with bodies) - will make decision eventually.

## Compiler internals

* `SyntaxKind.ExtensionKeyword`
* `DeclarationModifiers.Extension`
* `{Named?}TypeSymbol.IsExtensionClass`
* `{Named?}TypeSymbol.ExtensionClassType` (null if `IsExtensionClass==false`)
* `Symbol.IsInExtensionClass => this.ContainingType?.IsExtensionClass ?? false;`
* `Symbol {Named?}TypeSymbol.GetUnderlyingMember(Symbol)` (explained later)
* `{somewhere}.ReplaceExtensionClassMembers` (explained later)
* ... and lots of little private/internal changes for extension member lookup, lowering, emit, etc.

Basic process:

1. Parse. Discover extension keyword, throw on SyntaxKind.ExtensionKeyword to class modifiers, DeclarationModifiers.Extension gets added to the resulting type.
2. When instance member lookup fails, search for extension members. Resolve ambiguities (or report diagnostics, depending on what it is). Methods follow standard overload rules, properties do overload resolution based on conversion to the extended class (`List<int>.Foo` will pick `extension C1<T> : List<T>` over `extension C2<T> : IEnumerable<T>`), other member kinds are similar. (Target type for getters, and assigned type for setters, probably aren't considered?)
3. When doing lowering, convert references to extension class members to the signature that will eventually be emitted to IL. (These signature transformations are described later) - the API to do this is `Symbol {Named?}TypeSymbol.GetUnderlyingMember(Symbol)`, which is identity on non-extension members (possibly throw when given a symbol of a non-extension class?). Possibly batch conversion of symbols can be obtained by a (extension?) method alongside `NamespaceOrTypeSymbol.GetMembers` (`GetUnderlyingMembers`?) that would call the conversion on all members.
4. When doing emitting, convert symbols to that same form and emit to IL.
5. Emit some representation of the extended type so we can load it later.
6. When loading, detect that representation from the previous step and run the inverse of `GetUnderlyingMember` to report in symbol tables.

## IL member transformations:

    extension class Ext<Tp1, ...> : BaseType<Ta1, ...> where Tp1 : ..., <<constraints>>
    {
    }
    -->
    [Extension] // or a new attribute
    static class Ext<Tp1, ...>
    {
        void <>_UnspeakableSpecialName<Tp1_rename, ...>(BaseType<Ta1, replace refs to Tp1, ...> param)
            where Tp1_rename : ..., <<constraints>>
        {
            // never called, just to record BaseType.
            // While the CLR spec allows base types on static (abstract&sealed) classes,
            // can't use that because extending from array, type param, etc.
            // Also can't re-use class "Tp1, ..." params,
            // since constraints need to be specified.
            // (although maybe leave constraints on class, and transfer only type to param?)
        }
    }
    
    RetType InstanceMethod<T1, ...>(ParamType1 p1, ...);
    -->
    static RefType InstanceMethod<T1, ...>(BaseType<Ta1, ...> @this, ParamType1 p1, ...);
    
    static RetType StaticMethod<T1, ...>(ParamType1 p1, ...);
    -->
    // no change - only callers change, swap out BaseType with Ext (and type args)
    
    // Properties themselves can't be generic (not even IL allows it, I think)
    RetType InstanceProperty { get {...} }
    --> this one is weird, so describing it in IL-ish terms
    // Old compiler/other languages might get really confused. Looks like an indexer, ish?
    static RetType get_InstanceProperty(BaseType<Ta1, ...> @this);
    .property <<blah, no `instance` (so static)>>
    {
        .get RetType get_InstanceProperty(BaseType<Ta1, ...>);
    }
    
    // see beginning of article, this might not be allowed
    RetType InstanceProperty { set {...} }
    -->
    static void set_InstanceProperty(BaseType<Ta1, ...> @this, RetType @value);
    .property <<blah, no `instance` (so static)>>
    {
        .set void set_InstanceProperty(BaseType<Ta1, ...>, RetType);
    }
    
    RetType InstanceProperty { get {...} set {...} }
    -->
    // the reasonable combination of the get-only and set-only examples
    
    static RetType StaticProperty { ... }
    // no change - only callers change, swap out BaseType with Ext (and type args)
    
    RetType InstanceIndexer[ParamType p1, ...] { get {...} set {...} }
    -->
    // similar to properties, but:
    static RetType get_InstanceProperty(BaseType<Ta1, ...> @this, ParamType p1, ...);
    static void set_InstanceProperty(BaseType<Ta1, ...> @this, ParamType p1, ..., RetType @value);
    
    // MUST call `this(...)`
    BaseType(ParamType p1, ...) : this(arg1, ...)
    {
        statements;
    }
    -->
    static BaseType<Ta1, ...> SomeMethodName(ParamType p1, ...)
    {
        var @this = new BaseType<Ta1, ...>(arg1, ...);
        // or possibly (if `this(...)` was another ext constructor)
        var @this = Ext<Tp1, ...>.SomeMethodName(arg1, ...);
        statements;
    }
    // Do we want SomeMethodName to be speakable? If so, what do we call it?

## Minor notes

Example of how type parameters might be useful:

    extension class ResourceGuard<T> : T where T : IDisposable, new()
    {
        static R Use<R>(Func<T, R> func)
        {
            using (var resource = new T())
            {
                return func(resource);
            }
        }
    }
    // ...
    var thing = MemoryStream.Use(mem => ... use mem as buffer ...);
