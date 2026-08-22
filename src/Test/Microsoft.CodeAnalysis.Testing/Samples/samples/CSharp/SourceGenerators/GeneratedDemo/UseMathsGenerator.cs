using Maths;
using static System.Console;

namespace GeneratedDemo
{
    public static class UseMathsGenerator
    {
        public static void Run()
        {
            WriteLine($"The area of a (10, 5) rectangle is: {Formulas.AreaRectangle(10, 5)}");
            WriteLine($"The area of a (10) square is: {Formulas.AreaSquare(10)}");
            WriteLine($"The GoldHarmon of 3 is: {Formulas.GoldHarm(3)}");
        }
    }
}
