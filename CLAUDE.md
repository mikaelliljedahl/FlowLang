# LLM-Friendly Backend Language (FlowLang) - Development Plan

## Project Overview
FlowLang is a backend programming language designed specifically for LLM-assisted development. It prioritizes explicitness, predictability, and safety while maintaining compatibility with existing ecosystems.

## Core Philosophy
- **Explicit over implicit**: Every operation, side effect, and dependency must be clearly declared
- **One way to do things**: Minimize choices to reduce LLM confusion and increase code consistency
- **Safety by default**: Null safety, effect tracking, and comprehensive error handling built-in
- **Self-documenting**: Code structure serves as documentation
- **Specification preservation**: Intent and reasoning are atomically linked with implementation to prevent context loss

## Development Memories

### Project Development Guidelines
- If you create a folder for testing transpiler output, remove it afterward. Don't create transpiled files in the the root directory. Always follow existing directory structure, e.g. code in the src directory, documentation in the docs folder etc

(Rest of the existing content remains unchanged)