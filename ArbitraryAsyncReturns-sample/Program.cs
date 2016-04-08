using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;

class C
{
    static void Main()
    {
        MainAsync().GetAwaiter().GetResult();
    }

    static async Task MainAsync()
    {
        // Test ValueTask
        var i1 = await g(0);
        Console.WriteLine(i1);
        var i2 = await g(100);
        Console.WriteLine(i2);

        // Test ITask
        ITask<string> its = f();
        ITask<object> ito = its;
        var io = await ito;
        Console.WriteLine(io);

        // Weaving Task
        await h();

        // IAsyncAction
        //await uwp();
    }

    static async ITask<string> f()
    {
        await Task.Yield();
        return "hello";
    }

    static async ValueTask<int> g(int delay)
    {
        if (delay > 0) await Task.Delay(delay);
        return delay;
    }

    static async WeavingTask h()
    {
        await new WeavingConfiguration(() => Console.WriteLine("Weave suspend"), () => Console.WriteLine("Weave resume"));

        Console.WriteLine("h.A");
        await Delay(0);
        Console.WriteLine("h.B");
        await Delay(10);
        Console.WriteLine("h.C");
        await Delay(100);
        Console.WriteLine("h.D");
    }

    static async Task Delay(int i)
    {
        Console.WriteLine($"  about to delay {i}");
        await Task.Delay(i);
        Console.WriteLine($"  done delay {i}");
    }

    static async IAsyncAction uwp()
    {
        await Task.Delay(100);
    }

}

