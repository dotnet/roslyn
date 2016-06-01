## Extension Everything

Basic syntax:

    extension class ExtClass : OtherType
    {
    }

* Must be `class`
* If the `extension` keyword is present, the extends clause must be present (does not default to `object`)
* `OtherType` must not be extension class.
* `OtherType` may be a class, static class, struct, interface, or enum.
* Modifiers allowed on a class with `extension`: access modifiers, `unsafe`, `partial` (i.e. not `abstract`, `sealed`, `static`)
* Unresolved question: NamedTypeSymbols are all fine. What about arrays, pointers, and type parameters? (`dynamic` is not allowed)

Members defined on ExtClass behave like they're defined on OtherType (with some exceptions).

The member kinds currently planned on being supported are:

* Instance methods (same as old extension methods)
* Static methods
* Instance properties
* Static properties
* Indexers
* Events (possibly)
* Constructors (possibly)
* Operators (specifics to be nailed down later - might want to disallow implicit conversions?)

Other member kinds are valid to be defined in an extension class, but do not project onto the extended class. Such kinds are:

* Nested class (maybe?)

Other kinds are forbidden and produce an error:

* Static fields (maybe? Might go into "not projected" category.)
* Instance fields
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

## IL member transformations (examples):

    extension class IntDictionaryExtensions<T> : Dictionary<int, T>
        where T : class
    {
        public void AddWithDoubleKey(double key, T value)
        {
            this.Add((int)key, value);
        }
        
        public void ConvertAndAdd<TOther>(Dictionary<int, TOther> others)
        {
            foreach (var other in others)
            {
                this.Add(other.Key, (T)other.Value);
            }
        }
        
        public int SumOfKeys => this.Keys.Sum();
        
        public static int UselessProperty
        {
            set
            {
                Console.WriteLine(value);
            }
        }
        
        public int this[double index]
        {
            get
            {
                return this[(int)index];
            }
            set
            {
                this[(int)index] = value;
            }
        }
        
        public Dictionary<int, T>(List<T> items) : this(items.Count)
        {
            foreach (var item in items)
            {
                this.Add(item.GetHashCode(), item);
            }
        }
    }

### is transformed into:

    static class IntDictionaryExtensions<T>
        where T : class
    {
        // needed for the case where, for example, we only had UselessProperty defined
        // (i.e. Dictionary<int, T> appears nowhere in any members)
        private void ExtensionClassMarker(Dictionary<int, T> extendedType)
        {
        }
        
        public static void AddWithDoubleKey(Dictionary<int, T> @this, double key, T value)
        {
            @this.Add((int)key, value);
        }
        
        public static void ConvertAndAdd<TOther>(Dictionary<int, T> @this, Dictionary<int, TOther> others)
        {
            foreach (var other in others)
            {
                @this.Add(other.Key, (T)other.Value);
            }
        }
        
        // indexers are basically properties with more parameters in their accessors.
        // (the @this parameter is just inserted as the first arg of the property accessor)
        public static int SumOfKeys[Dictionary<int, T> @this] => @this.Keys.Sum();
        
        // no change
        public static int UselessProperty
        {
            set
            {
                Console.WriteLine(value);
            }
        }
        
        public static int this[Dictionary<int, T> @this, double index]
        {
            get
            {
                return @this[(int)index];
            }
            set
            {
                @this[(int)index] = value;
            }
        }
        
        public static Dictionary<int, T> New(List<T> items)
        {
            var @this = new Dictionary<int, T>(items.Count);
            foreach (var item in items)
            {
                @this.Add(item.GetHashCode(), item);
            }
        }
    }

## IL member transformations (formal):

    extension class Ext<Tp1, ...> : BaseType<Ta1, ...> where Tp1 : ..., <<constraints>>
    {
    }
    -->
    [Extension] // or a new attribute
    static class Ext<Tp1, ...>
            where Tp1 : ..., <<constraints>>
    {
        void <>_UnspeakableSpecialName(BaseType<Ta1, ...> param)
        {
            // never called, just to record BaseType.
            // While the CLR spec allows base types on static (abstract&sealed) classes,
            // can't use that because extending from array, type param, etc.
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
    
    // If `this()` not specified, uses standard lookup rules for empty constructor.
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
