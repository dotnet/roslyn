# Running Microbenchmarks

Compile the solution in Release mode (so binaries are available in release)

In Visual Studio:

- Set the Microsoft.AspNetCore.Razor.Microbenchmarks project as the "Startup" project
- Set the configuration to "Release"
- Run without debugging (ctrl + F5)

You can also debug your benchmark by using the Debug configuration and running normally; however, numbers generated in that way wont be valuable.

Command Line:
To run a specific benchmark add it as parameter.

```cmd
dotnet run -c Release -f net472 <benchmark_name>
```

If you run without any parameters, you'll be offered the list of all benchmarks and get to choose.

```cmd
dotnet run -c Release -f net472
```

Profiling:
To configure BDM generate an .etl stack trace (Windows only), specify "--profiler ETW" at the command-line.
