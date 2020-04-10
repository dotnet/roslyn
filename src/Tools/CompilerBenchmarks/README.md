
This project contains a set of "micro" benchmarks for the compiler, focused on measuring
the time spent in specific phases of the compiler, e.g. Emit, metadata serialization, binding,
parsing, etc.

To run all benchmarks, simply run the `run-perf.ps1` file on your machine, which should produce
a simple output table containing the results. To compare the results of your change, you can
run the script before and after and attempt to compare the results. Calculating statistical
significance is beyond the scope of this document, but you can get a general idea of whether
or not your changes are significant if the different is substantially larger than the "error"
value reported in the results summary.