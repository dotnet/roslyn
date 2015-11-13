To avoid tests copying the same binaries to the common build directory and causing races, this
project is used to copy and deploy all the required product, xUnit and Moq binaries required
to run all the project system tests.

When adding new dependencies, reference them from this project or add them to project.json.