Option Explicit On
Option Strict On
Option Infer On

Imports CSV

Friend Class UseCsvGenerator

    Public Shared Sub Run()

        Console.WriteLine("## CARS")
        Cars.All.ToList().ForEach(Sub(c) Console.WriteLine(c.Brand & vbTab & c.Model & vbTab & c.Year & vbTab & c.Cc & vbTab & c.Favorite))
        Console.WriteLine(vbCr & "## PEOPLE")
        People.All.ToList().ForEach(Sub(p) Console.WriteLine(p.Name & vbTab & p.Address & vbTab & p._11Age))

    End Sub

End Class
