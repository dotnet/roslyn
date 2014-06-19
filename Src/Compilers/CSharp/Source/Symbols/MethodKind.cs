using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents the kind of a method.
    /// </summary>
    public enum MethodKind
    {
        /// <summary>
        /// An anonymous method or lambda expression
        /// </summary>
        AnonymousFunction = 0,

        /// <summary>
        /// An instance constructor. The return type is always void.
        /// </summary>
        Constructor = 1,

        /// <summary>
        /// A user-defined conversion.
        /// </summary>
        Conversion = 2,

        /// <summary>
        /// The invoke method of a delegate.
        /// </summary>
        DelegateInvoke = 3,

        /// <summary>
        /// A destructor.
        /// </summary>
        Destructor = 4,

        /// <summary>
        /// The implicitly-defined add method associated with an event.
        /// </summary>
        EventAdd = 5,

        // EventRaise = 6

        /// <summary>
        /// The implicitly-defined remove method associated with an event.
        /// </summary>
        EventRemove = 7,

        /// <summary>
        /// An explicit interface implementation method. The ImplementedMethods
        /// property can be used to determine which method is being implemented.
        /// </summary>
        ExplicitInterfaceImplementation = 8,

        /// <summary>
        /// A user-defined operator.
        /// </summary>
        UserDefinedOperator = 9,

        /// <summary>
        /// A normal method.
        /// </summary>
        Ordinary = 10,

        /// <summary>
        /// The implicitly-defined get method associated with a property.
        /// </summary>
        PropertyGet = 11,

        /// <summary>
        /// The implicitly-defined set method associated with a property.
        /// </summary>
        PropertySet = 12,

        /// <summary>
        /// An extension method with the "this" parameter removed.
        /// </summary>
        ReducedExtension = 13,

        /// <summary>
        /// A static constructor. The return type is always void.
        /// </summary>
        StaticConstructor = 14,

        /// <summary>
        /// A built-in operator.
        /// </summary>
        BuiltinOperator = 15,
    }
}