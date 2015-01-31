using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
  public enum Clusivity : int
  {
    @In = 0,
    @Ex = -1
  }
  public static class GenericExtensions
  {


    public static bool IsBetween<T>(this T value, T lower, T upper, Clusivity lc = Clusivity.In, Clusivity uc = Clusivity.In)
      where T : IComparable<T>
    {
      return (lower.CompareTo(value) <= (int)lc) && (value.CompareTo(upper) <= (int)uc);
    }
  }
}
