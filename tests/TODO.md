# TODO: Fix Failing Tests

The test suite is currently failing to build, preventing any tests from running. This is a critical issue that must be resolved before any new code is pushed to the repository.

## How to Run Tests

To run the tests and see the errors, execute the following command from the root of the project:

```bash
dotnet test tests/Cadenza.Tests.csproj
```

## Summary of Errors

The test suite is suffering from a large number of compilation errors, indicating that the tests are significantly out of sync with the production code. 
## Suggested Action Plan


1.  **Triage the Errors:** Start by examining the errors in a single test file, for example.
2.  **Fix a Single Test:** Focus on fixing one test at a time. This will likely involve updating the test to match the new API of the code it is testing.
3.  **Run Tests Frequently:** After fixing each test, run the test suite again to ensure that the fix did not introduce any new errors.
4.  **Continue until all tests pass:** Repeat the process until all compilation errors are resolved and all tests pass.
