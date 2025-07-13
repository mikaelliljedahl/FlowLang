using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowLang.Compiler;

namespace FlowLang.Targets
{
    /// <summary>
    /// JavaScript target generator for FlowLang
    /// Transpiles FlowLang to JavaScript with Node.js/Browser compatibility
    /// Specialized for LLM-generated UI components
    /// </summary>
    public class JavaScriptGenerator : ITargetGenerator
    {
        public async Task<TargetGenerationResult> GenerateAsync(Program program, TargetConfiguration config)
        {
            var sourceCode = await GenerateJavaScriptCodeAsync(program, config);
            var additionalFiles = GenerateAdditionalFiles(program, config);
            
            return new TargetGenerationResult
            {
                SourceCode = sourceCode,
                AdditionalFiles = additionalFiles,
                Dependencies = GetRequiredDependencies(),
                BuildInstructions = GetBuildInstructions()
            };
        }

        public string GetTargetName() => "JavaScript";

        public List<string> GetSupportedFeatures()
        {
            return new List<string>
            {
                "UI Components",
                "State Management", 
                "Effect System",
                "API Client Generation",
                "Result Types",
                "String Interpolation",
                "Event Handling",
                "Reactive Updates",
                "Async Operations"
            };
        }

        public TargetCapabilities GetCapabilities()
        {
            return new TargetCapabilities
            {
                SupportsAsync = true,
                SupportsParallelism = true,
                SupportsGarbageCollection = true,
                SupportsReflection = true,
                SupportsExceptions = true,
                SupportsInterop = true,
                SupportedEffects = new List<string> { "DOM", "Network", "LocalStorage", "WebSocket", "Logging" }
            };
        }

        private async Task<string> GenerateJavaScriptCodeAsync(Program program, TargetConfiguration config)
        {
            var sb = new StringBuilder();
            
            // Generate module header with FlowLang runtime imports
            GenerateModuleHeader(sb);
            
            // Generate UI components
            foreach (var statement in program.Statements)
            {
                switch (statement)
                {
                    case ComponentDeclaration component:
                        GenerateComponent(sb, component);
                        break;
                        
                    case AppStateDeclaration appState:
                        GenerateAppState(sb, appState);
                        break;
                        
                    case ApiClientDeclaration apiClient:
                        GenerateApiClient(sb, apiClient);
                        break;
                        
                    case FunctionDeclaration function:
                        GenerateFunction(sb, function);
                        break;
                }
            }
            
            // Generate module exports
            GenerateModuleExports(sb, program);
            
            return sb.ToString();
        }

        private void GenerateModuleHeader(StringBuilder sb)
        {
            sb.AppendLine("// FlowLang Generated JavaScript - LLM Optimized");
            sb.AppendLine("// This code is designed for maximum LLM readability and predictability");
            sb.AppendLine();
            sb.AppendLine("import { FlowLangRuntime, Result, StateManager, ApiClient } from '@flowlang/runtime';");
            sb.AppendLine("import { createElement, useState, useEffect } from 'react';");
            sb.AppendLine();
            sb.AppendLine("// FlowLang Result type implementation");
            sb.AppendLine("const Ok = (value) => ({ type: 'Ok', value, isOk: true, isError: false });");
            sb.AppendLine("const Error = (error) => ({ type: 'Error', error, isOk: false, isError: true });");
            sb.AppendLine();
        }

        private void GenerateComponent(StringBuilder sb, ComponentDeclaration component)
        {
            sb.AppendLine($"// FlowLang Component: {component.Name}");
            sb.AppendLine($"// Effects: [{string.Join(", ", component.Effects)}]");
            sb.AppendLine($"// State: [{string.Join(", ", component.StateVariables)}]");
            sb.AppendLine($"// Events: [{string.Join(", ", component.Events)}]");
            
            // Generate React component function
            var parameterNames = string.Join(", ", component.Parameters.Select(p => p.Name));
            sb.AppendLine($"export function {component.Name}({{{parameterNames}}}) {{");
            
            // Generate state declarations
            foreach (var stateDecl in component.StateDeclarations)
            {
                GenerateStateDeclaration(sb, stateDecl, 1);
            }
            
            // Generate lifecycle effects
            if (component.Lifecycle?.OnMount != null)
            {
                sb.AppendLine("    // Component mount lifecycle");
                sb.AppendLine("    useEffect(() => {");
                sb.AppendLine("        // On mount logic");
                GenerateStatements(sb, new List<ASTNode> { component.Lifecycle.OnMount }, 2);
                sb.AppendLine("    }, []);");
                sb.AppendLine();
            }
            
            // Generate event handlers
            foreach (var handler in component.EventHandlers)
            {
                GenerateEventHandler(sb, handler, 1);
            }
            
            // Generate render function
            sb.AppendLine("    // Explicit render logic - LLM friendly");
            sb.AppendLine("    return (");
            GenerateUIElement(sb, component.RenderTree, 2);
            sb.AppendLine("    );");
            
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private void GenerateStateDeclaration(StringBuilder sb, StateDeclaration stateDecl, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            var initialValue = stateDecl.InitialValue != null ? 
                GenerateExpression(stateDecl.InitialValue) : GetDefaultValue(stateDecl.Type);
                
            sb.AppendLine($"{indent}// State: {stateDecl.Name} ({stateDecl.Type})");
            sb.AppendLine($"{indent}const [{stateDecl.Name}, set{PascalCase(stateDecl.Name)}] = useState({initialValue});");
        }

        private void GenerateEventHandler(StringBuilder sb, EventHandler handler, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            var parameters = string.Join(", ", handler.Parameters.Select(p => p.Name));
            
            sb.AppendLine($"{indent}// Event handler: {handler.Name}");
            sb.AppendLine($"{indent}// Effects: [{string.Join(", ", handler.Effects)}]");
            sb.AppendLine($"{indent}const {handler.Name} = ({parameters}) => {{");
            
            GenerateStatements(sb, handler.Body, indentLevel + 1);
            
            sb.AppendLine($"{indent}}};");
            sb.AppendLine();
        }

        private void GenerateUIElement(StringBuilder sb, UIElement element, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            // Generate JSX element
            sb.Append($"{indent}<{element.TagName}");
            
            // Generate attributes
            foreach (var (attrName, attrValue) in element.Attributes)
            {
                sb.Append($" {attrName}={{{GenerateExpression(attrValue)}}}");
            }
            
            // Generate event handlers
            foreach (var uiEvent in element.Events)
            {
                sb.Append($" {uiEvent.EventType}={{{uiEvent.HandlerName}}}");
            }
            
            if (element.Children.Any())
            {
                sb.AppendLine(">");
                
                // Generate children
                foreach (var child in element.Children)
                {
                    GenerateUIElement(sb, child, indentLevel + 1);
                }
                
                sb.AppendLine($"{indent}</{element.TagName}>");
            }
            else
            {
                sb.AppendLine(" />");
            }
        }

        private void GenerateAppState(StringBuilder sb, AppStateDeclaration appState)
        {
            sb.AppendLine($"// FlowLang App State: {appState.Name}");
            sb.AppendLine($"// Effects: [{string.Join(", ", appState.Effects)}]");
            sb.AppendLine($"class {appState.Name} {{");
            
            // Generate constructor with initial state
            sb.AppendLine("    constructor() {");
            sb.AppendLine("        this.state = {");
            foreach (var property in appState.Properties)
            {
                var defaultValue = property.DefaultValue != null ? 
                    GenerateExpression(property.DefaultValue) : GetDefaultValue(property.Type);
                sb.AppendLine($"            {property.Name}: {defaultValue},");
            }
            sb.AppendLine("        };");
            sb.AppendLine("        this.listeners = [];");
            sb.AppendLine("    }");
            sb.AppendLine();
            
            // Generate state actions
            foreach (var action in appState.Actions)
            {
                GenerateStateAction(sb, action, 1);
            }
            
            // Generate utility methods
            sb.AppendLine("    // State management utilities");
            sb.AppendLine("    setState(updates) {");
            sb.AppendLine("        this.state = { ...this.state, ...updates };");
            sb.AppendLine("        this.notifyListeners();");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    subscribe(listener) {");
            sb.AppendLine("        this.listeners.push(listener);");
            sb.AppendLine("        return () => this.listeners = this.listeners.filter(l => l !== listener);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    notifyListeners() {");
            sb.AppendLine("        this.listeners.forEach(listener => listener(this.state));");
            sb.AppendLine("    }");
            
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"export const {appState.Name.ToLower()}Instance = new {appState.Name}();");
            sb.AppendLine();
        }

        private void GenerateStateAction(StringBuilder sb, StateAction action, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            var parameters = string.Join(", ", action.Parameters.Select(p => p.Name));
            
            sb.AppendLine($"{indent}// Action: {action.Name}");
            sb.AppendLine($"{indent}// Effects: [{string.Join(", ", action.Effects)}]");
            sb.AppendLine($"{indent}// Updates: [{string.Join(", ", action.UpdatedProperties)}]");
            sb.AppendLine($"{indent}async {action.Name}({parameters}) {{");
            
            GenerateStatements(sb, action.Body, indentLevel + 1);
            
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        private void GenerateApiClient(StringBuilder sb, ApiClientDeclaration apiClient)
        {
            sb.AppendLine($"// FlowLang API Client: {apiClient.Name}");
            sb.AppendLine($"// Generated from service: {apiClient.FromService}");
            sb.AppendLine($"class {apiClient.Name} {{");
            
            // Generate constructor with configuration
            sb.AppendLine("    constructor(config = {}) {");
            foreach (var (key, value) in apiClient.Configuration)
            {
                sb.AppendLine($"        this.{key} = config.{key} || {GenerateExpression(value)};");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
            
            // Generate API methods
            foreach (var method in apiClient.Methods)
            {
                GenerateApiMethod(sb, method, 1);
            }
            
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"export const {apiClient.Name.ToLower()}Instance = new {apiClient.Name}();");
            sb.AppendLine();
        }

        private void GenerateApiMethod(StringBuilder sb, ApiMethod method, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            var parameters = string.Join(", ", method.Parameters.Select(p => p.Name));
            
            sb.AppendLine($"{indent}// API Method: {method.Name}");
            sb.AppendLine($"{indent}// Effects: [{string.Join(", ", method.Effects)}]");
            sb.AppendLine($"{indent}async {method.Name}({parameters}) {{");
            sb.AppendLine($"{indent}    try {{");
            sb.AppendLine($"{indent}        const response = await fetch(`${{this.baseUrl}}/{method.Name}`, {{");
            sb.AppendLine($"{indent}            method: 'POST',");
            sb.AppendLine($"{indent}            headers: {{");
            sb.AppendLine($"{indent}                'Content-Type': 'application/json',");
            sb.AppendLine($"{indent}                'Authorization': `Bearer ${{this.authToken}}`,");
            sb.AppendLine($"{indent}            }},");
            sb.AppendLine($"{indent}            body: JSON.stringify({{ {parameters} }}),");
            sb.AppendLine($"{indent}        }});");
            sb.AppendLine();
            sb.AppendLine($"{indent}        if (!response.ok) {{");
            sb.AppendLine($"{indent}            return Error(`API call failed: ${{response.status}}`);");
            sb.AppendLine($"{indent}        }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}        const data = await response.json();");
            sb.AppendLine($"{indent}        return Ok(data);");
            sb.AppendLine($"{indent}    }} catch (error) {{");
            sb.AppendLine($"{indent}        return Error(error.message);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        private void GenerateFunction(StringBuilder sb, FunctionDeclaration function)
        {
            var parameters = string.Join(", ", function.Parameters.Select(p => p.Name));
            var isAsync = function.Effects?.Contains("Network") == true || function.Effects?.Contains("Database") == true;
            var asyncKeyword = isAsync ? "async " : "";
            
            sb.AppendLine($"// FlowLang Function: {function.Name}");
            if (function.Effects?.Any() == true)
            {
                sb.AppendLine($"// Effects: [{string.Join(", ", function.Effects)}]");
            }
            sb.AppendLine($"export {asyncKeyword}function {function.Name}({parameters}) {{");
            
            GenerateStatements(sb, function.Body, 1);
            
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private void GenerateStatements(StringBuilder sb, List<ASTNode> statements, int indentLevel)
        {
            foreach (var statement in statements)
            {
                GenerateStatement(sb, statement, indentLevel);
            }
        }

        private void GenerateStatement(StringBuilder sb, ASTNode statement, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            switch (statement)
            {
                case ReturnStatement ret:
                    sb.AppendLine($"{indent}return {GenerateExpression(ret.Expression)};");
                    break;
                    
                case SetStateCall setState:
                    sb.AppendLine($"{indent}set{PascalCase(setState.StateName)}({GenerateExpression(setState.Value)});");
                    break;
                    
                default:
                    sb.AppendLine($"{indent}// TODO: Implement {statement.GetType().Name}");
                    break;
            }
        }

        private string GenerateExpression(ASTNode expression)
        {
            return expression switch
            {
                NumberLiteral num => num.Value.ToString(),
                Identifier id => id.Name,
                BinaryExpression binary => $"({GenerateExpression(binary.Left)} {binary.Operator} {GenerateExpression(binary.Right)})",
                _ => "null"
            };
        }

        private string GetDefaultValue(string type)
        {
            return type switch
            {
                "int" => "0",
                "string" => "''",
                "bool" => "false",
                var t when t.StartsWith("Option<") => "null",
                var t when t.StartsWith("List<") => "[]",
                _ => "null"
            };
        }

        private string PascalCase(string input)
        {
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        private void GenerateModuleExports(StringBuilder sb, Program program)
        {
            sb.AppendLine("// Module exports - explicit for LLM clarity");
            var components = program.Statements.OfType<ComponentDeclaration>();
            var appStates = program.Statements.OfType<AppStateDeclaration>();
            var apiClients = program.Statements.OfType<ApiClientDeclaration>();
            
            foreach (var component in components)
            {
                sb.AppendLine($"export {{ {component.Name} }};");
            }
            
            foreach (var appState in appStates)
            {
                sb.AppendLine($"export {{ {appState.Name}, {appState.Name.ToLower()}Instance }};");
            }
            
            foreach (var apiClient in apiClients)
            {
                sb.AppendLine($"export {{ {apiClient.Name}, {apiClient.Name.ToLower()}Instance }};");
            }
        }

        private List<AdditionalFile> GenerateAdditionalFiles(Program program, TargetConfiguration config)
        {
            var files = new List<AdditionalFile>();

            // Generate package.json
            files.Add(new AdditionalFile
            {
                Name = "package.json",
                Type = "build",
                Content = GeneratePackageJson(program)
            });

            // Generate HTML template
            files.Add(new AdditionalFile
            {
                Name = "index.html",
                Type = "html",
                Content = GenerateHtmlTemplate(program)
            });

            // Generate FlowLang runtime
            files.Add(new AdditionalFile
            {
                Name = "flowlang-runtime.js",
                Type = "runtime",
                Content = GenerateFlowLangRuntime()
            });

            return files;
        }

        private string GeneratePackageJson(Program program)
        {
            return @"{
  ""name"": ""flowlang-generated-app"",
  ""version"": ""1.0.0"",
  ""type"": ""module"",
  ""main"": ""index.js"",
  ""scripts"": {
    ""dev"": ""vite"",
    ""build"": ""vite build"",
    ""preview"": ""vite preview""
  },
  ""dependencies"": {
    ""react"": ""^18.2.0"",
    ""react-dom"": ""^18.2.0"",
    ""@flowlang/runtime"": ""^1.0.0""
  },
  ""devDependencies"": {
    ""@vitejs/plugin-react"": ""^4.0.0"",
    ""vite"": ""^4.4.0""
  }
}";
        }

        private string GenerateHtmlTemplate(Program program)
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>FlowLang Generated App</title>
</head>
<body>
    <div id=""root""></div>
    <script type=""module"" src=""/src/main.jsx""></script>
</body>
</html>";
        }

        private string GenerateFlowLangRuntime()
        {
            return @"// FlowLang JavaScript Runtime
// Provides Result types, state management, and effect tracking

export class FlowLangRuntime {
    static observability = {
        functionCalls: new Map(),
        effectUsage: new Map(),
        
        recordFunctionCall(functionName, effects) {
            this.functionCalls.set(functionName, {
                count: (this.functionCalls.get(functionName)?.count || 0) + 1,
                effects: effects,
                lastCalled: new Date()
            });
            
            effects.forEach(effect => {
                this.effectUsage.set(effect, (this.effectUsage.get(effect) || 0) + 1);
            });
        },
        
        getStats() {
            return {
                functionCalls: Object.fromEntries(this.functionCalls),
                effectUsage: Object.fromEntries(this.effectUsage)
            };
        }
    };
}

export const Result = {
    Ok: (value) => ({ type: 'Ok', value, isOk: true, isError: false }),
    Error: (error) => ({ type: 'Error', error, isOk: false, isError: true })
};

export class StateManager {
    constructor(initialState = {}) {
        this.state = initialState;
        this.listeners = [];
    }
    
    setState(updates) {
        this.state = { ...this.state, ...updates };
        this.notifyListeners();
    }
    
    subscribe(listener) {
        this.listeners.push(listener);
        return () => this.listeners = this.listeners.filter(l => l !== listener);
    }
    
    notifyListeners() {
        this.listeners.forEach(listener => listener(this.state));
    }
}";
        }

        private List<string> GetRequiredDependencies()
        {
            return new List<string>
            {
                "react@^18.2.0",
                "react-dom@^18.2.0", 
                "@flowlang/runtime@^1.0.0",
                "@vitejs/plugin-react@^4.0.0",
                "vite@^4.4.0"
            };
        }

        private Dictionary<string, string> GetBuildInstructions()
        {
            return new Dictionary<string, string>
            {
                ["install"] = "npm install",
                ["dev"] = "npm run dev",
                ["build"] = "npm run build",
                ["preview"] = "npm run preview",
                ["test"] = "npm test"
            };
        }
    }
}