using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    public static partial class ConversionKindExtensions
    {
        public static bool IsImplicitConversion(this ConversionKind kind)
        {
            switch (kind)
            {
                case ConversionKind.AnonymousFunction:
                case ConversionKind.Boxing:
                case ConversionKind.Dynamic:
                case ConversionKind.Identity:
                case ConversionKind.ImplicitConstant:
                case ConversionKind.ImplicitEnumeration:
                case ConversionKind.ImplicitNullable:
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ImplicitReference:
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.MethodGroup:
                case ConversionKind.NullLiteral:
                    return true;
                case ConversionKind.ExplicitEnumeration:
                case ConversionKind.ExplicitNullable:
                case ConversionKind.ExplicitNumeric:
                case ConversionKind.ExplicitReference:
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.NoConversion:
                case ConversionKind.Unboxing:
                    return false;
                default:
                    Debug.Fail("Bad conversion kind!");
                    return false;
            }
        }
    }
}