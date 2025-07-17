The test suite for the Cadenza project is currently failing with a large number of compilation errors. Your task is to fix all the compilation errors in the `tests/Cadenza.Tests.csproj` project and ensure that all tests pass.

**Instructions:**

1.  Run the tests using the following command to see the full list of errors:
    ```bash
    dotnet test tests/Cadenza.Tests.csproj
    ```
2.  Systematically go through the errors and fix them. The errors are primarily due to the tests being out of sync with the production code. You will need to:
    *   Update method calls with the correct arguments.
    *   Remove or update references to obsolete methods and properties.
    *   Resolve ambiguous references by fully qualifying types or using aliases.
    *   Add any missing `using` directives.
3.  After fixing a set of errors, run the tests again to ensure that you are making progress and not introducing new issues.
4.  Continue this process until all compilation errors are resolved and all tests pass.

**Acceptance Criteria:**

*   The `dotnet test tests/Cadenza.Tests.csproj` command completes successfully.
*   All tests in the test suite pass.
*   No new warnings are introduced into the build.

**Do not push any code to git until all tests are passing.**

---

### How to Perform Search and Replace in Multiple Files on Linux

A common issue you will encounter is the use of outdated NUnit assertion methods. For example, `Assert.Single()` and `Assert.Contains()` are no longer the preferred way to write these assertions. You can fix these across the entire project efficiently using command-line tools.

Here is a guide on how to use `grep` to find the files containing the patterns and `sed` to replace them.

**1. Finding the Files**

First, it's a good idea to see which files will be affected. You can use `grep` to recursively search for a pattern in the `tests` directory.

*   To find all occurrences of `Assert.Single`:
    ```bash
    grep -r "Assert.Single" tests/
    ```
*   To find all occurrences of `Assert.Contains`:
    ```bash
    grep -r "Assert.Contains" tests/
    ```

**2. Performing the Replacement with `sed`**

Once you have an idea of the files that will be changed, you can use a combination of `find`, `xargs`, and `sed` to perform the replacement. The `sed -i` command modifies the files in place.

**Example 1: Replacing `Assert.Single(collection)`**

This is a bit complex because you need to capture the collection name. A simple search and replace won't work. You often have to do this manually, but for simple cases, you can use regular expressions.

Let's say you have code like `Assert.Single(myCollection);`. You want to change it to `Assert.That(myCollection.Count, Is.EqualTo(1));`.

A `sed` command to do this would look like:

```bash
find tests -type f -name "*.cs" -print0 | xargs -0 sed -i 's/Assert\.Single(\([^)]*\));/Assert.That(\1.Count, Is.EqualTo(1));/g'
```

**Breakdown of the `sed` command:**
*   `s/.../.../g`: This is the substitute command.
*   `Assert\.Single\(`: Matches the literal text `Assert.Single(`. We escape the `.` and `(`.
*   `\([^)]*\)`: This is a capture group. It matches any characters that are not a closing parenthesis `)` and captures them. This is how we get the `myCollection` part.
*   `\);`: Matches the closing `);`.
*   `Assert.That(\1.Count, Is.EqualTo(1));`: This is the replacement string. `\1` refers to the first captured group (the collection name).

**Example 2: Replacing `Assert.Contains("substring", text)`**

The old `Assert.Contains` is often used in a way that is not a direct replacement. The modern equivalent is `Assert.That(text, Does.Contain("substring"))`.

If you have `Assert.Contains("some string", myVariable);`, you want to change it to `Assert.That(myVariable, Does.Contain("some string"));`.

```bash
find tests -type f -name "*.cs" -print0 | xargs -0 sed -i 's/Assert\.Contains(\([^,]*\), \([^)]*\));/Assert.That(\2, Does.Contain(\1));/g'
```

**Breakdown of the `sed` command:**
*   `Assert\.Contains\(`: Matches the literal `Assert.Contains(`.
*   `\([^,]*\)`: Captures the first argument (the substring).
*   `, `: Matches the comma and space.
*   `\([^)]*\)`: Captures the second argument (the text to search in).
*   `\);`: Matches the closing `);`.
*   `Assert.That(\2, Does.Contain(\1));`: The replacement string. `\2` is the second capture group (the text) and `\1` is the first (the substring).

**Important:** Always make sure you have a clean git status before running these commands, so you can easily review the changes and revert them if something goes wrong.
