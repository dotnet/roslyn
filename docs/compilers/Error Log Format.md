The C# and Visual Basic compilers support a /errorlog:<file> switch on
the command line to log all diagnostics in a structured, JSON format.

The log format is SARIF (Static Analysis Results Interchange Format):
See https://sarifweb.azurewebsites.net/ for the format specification,
JSON schema, and other related resources.