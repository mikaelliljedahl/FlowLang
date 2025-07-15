# Phase 5 - Runtime Bridge Fixes Plan

## Overview
Fix the FlowLang.Runtime bridge to enable .flow tools compilation. This is the critical path blocker for self-hosting migration.

## Current Issues
1. **Namespace Access**: Generated C# code can't access `FlowLang.Runtime` namespace
2. **Missing Methods**: Runtime lacks methods that .flow tools expect
3. **Syntax Incompatibilities**: FlowLang syntax doesn't transpile correctly to C#
4. **Result Type Integration**: Match expressions and Result patterns broken

## Tasks

### Task 1: Fix Namespace Integration
**Problem**: `FlowLang.Runtime` not accessible from generated C# code
**Solution**: 
- Add proper `using FlowLang.Runtime;` to generated C# files
- Ensure runtime namespace is included in compilation references
- Test with simple runtime call

### Task 2: Add Missing Runtime Methods
**Missing from analysis of linter.flow compilation errors**:
- `FlowLang.Runtime.StringRuntime.ToString(int)` ✅ ADDED
- `FlowLang.Runtime.FileSystemRuntime.FindFiles()` ✅ ADDED  
- `FlowLang.Runtime.ProcessRuntime.GetExitCode()` ✅ ADDED
- `FlowLang.Runtime.ProcessRuntime.GetErrorOutput()` ✅ ADDED
- `FlowLang.Runtime.ConsoleRuntime.WriteLine()` ✅ ADDED

### Task 3: Fix FlowLang Transpiler Issues
**Problems identified**:
- `List<T>.length` → should be `List<T>.Count` in C#
- `true`/`false` literals not recognized
- `match` expressions don't generate C# code
- `Result<T,E>` pattern matching broken
- Variable scoping in match blocks

### Task 4: Test Runtime Bridge
**Test with existing .flow tools**:
```bash
# Target: Get these to compile
./bin/release/flowc-core --compile src/FlowLang.Tools/linter.flow -o bin/linter.exe
./bin/release/flowc-core --compile src/FlowLang.Tools/dev-server.flow -o bin/dev-server.exe
./bin/release/flowc-core --compile src/FlowLang.Tools/simple-dev-server.flow -o bin/simple-dev-server.exe
```

### Task 5: Runtime Bridge Testing
**Create test .flow file**:
```flowlang
function test_runtime() uses [Logging, FileSystem] -> Result<string, string> {
    FlowLang.Runtime.LoggingRuntime.LogInfo("Testing runtime bridge")
    let files = FlowLang.Runtime.FileSystemRuntime.FindFiles(".", "*.flow", true)
    FlowLang.Runtime.ConsoleRuntime.WriteLine("Found files: " + files.length.ToString())
    return Ok("Runtime bridge working")
}
```

## Expected Outcomes
- All .flow tools compile successfully
- Runtime bridge methods work correctly
- Foundation for self-hosting migration complete
- Can proceed with tooling migration

## Success Criteria
- [ ] `linter.flow` compiles to working executable
- [ ] `dev-server.flow` compiles to working executable  
- [ ] `simple-dev-server.flow` compiles to working executable
- [ ] Runtime bridge test passes
- [ ] All FlowLang.Runtime methods accessible from .flow code

## Priority
**HIGH** - This is the critical path for Phase 5 self-hosting migration.