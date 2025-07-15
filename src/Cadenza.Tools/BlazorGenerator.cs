// Cadenza Blazor Generator - Converts Cadenza UI components to Blazor components
// This generator creates .razor files from Cadenza component ASTs

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cadenza.Tools
{
    /// <summary>
    /// Generates Blazor components from Cadenza UI component ASTs
    /// </summary>
    public class BlazorGenerator
    {
        private readonly StringBuilder _razorContent = new();
        private readonly StringBuilder _codeSection = new();
        private readonly HashSet<string> _usedServices = new();
        private readonly Dictionary<string, string> _stateVariables = new();
        private readonly List<string> _eventHandlers = new();
        
        /// <summary>
        /// Generates a Blazor component from a Cadenza ComponentDeclaration
        /// </summary>
        public string GenerateBlazorComponent(ComponentDeclaration component)
        {
            _razorContent.Clear();
            _codeSection.Clear();
            _usedServices.Clear();
            _stateVariables.Clear();
            _eventHandlers.Clear();

            // Generate component header and parameters
            GenerateComponentHeader(component);
            
            // Generate markup from render block
            GenerateMarkup(component.RenderBlock);
            
            // Generate code section
            GenerateCodeSection(component);
            
            // Combine markup and code
            var result = _razorContent.ToString();
            if (_codeSection.Length > 0)
            {
                result += Environment.NewLine + "@code {" + Environment.NewLine;
                result += _codeSection.ToString();
                result += "}" + Environment.NewLine;
            }
            
            return result;
        }

        /// <summary>
        /// Generates component header with parameters and using statements
        /// </summary>
        private void GenerateComponentHeader(ComponentDeclaration component)
        {
            // Add using statements for effects
            if (component.Effects != null)
            {
                foreach (var effect in component.Effects)
                {
                    AddEffectUsing(effect);
                }
            }
            
            // Add component parameters
            if (component.Parameters != null && component.Parameters.Count > 0)
            {
                foreach (var param in component.Parameters)
                {
                    _razorContent.AppendLine($"@* Parameter: {param.Name} *@");
                }
            }
            
            _razorContent.AppendLine();
        }

        /// <summary>
        /// Generates Blazor markup from Cadenza render block
        /// </summary>
        private void GenerateMarkup(ASTNode renderBlock)
        {
            if (renderBlock is UIElement element)
            {
                GenerateElement(element);
            }
            else if (renderBlock is ConditionalRender conditional)
            {
                GenerateConditionalMarkup(conditional);
            }
            else if (renderBlock is IterativeRender iterative)
            {
                GenerateIterativeMarkup(iterative);
            }
            else if (renderBlock is ComponentInstance componentInstance)
            {
                GenerateComponentInstance(componentInstance);
            }
            else if (renderBlock is RenderBlock block)
            {
                // Handle render block container
                GenerateRenderBlockContent(block);
            }
        }

        /// <summary>
        /// Generates HTML element markup
        /// </summary>
        private void GenerateElement(UIElement element)
        {
            var tag = MapCadenzaElementToHtml(element.Tag);
            
            _razorContent.Append($"<{tag}");
            
            // Generate attributes
            foreach (var attr in element.Attributes)
            {
                GenerateAttribute(attr);
            }
            
            if (element.Children != null && element.Children.Count > 0)
            {
                _razorContent.AppendLine(">");
                
                // Generate children
                foreach (var child in element.Children)
                {
                    GenerateMarkup(child);
                }
                
                _razorContent.AppendLine($"</{tag}>");
            }
            else
            {
                _razorContent.AppendLine(" />");
            }
        }

        /// <summary>
        /// Generates Blazor attribute from Cadenza UIAttribute
        /// </summary>
        private void GenerateAttribute(UIAttribute attr)
        {
            var attributeName = MapCadenzaAttributeToBlazor(attr.Name);
            
            if (attr.Value is StringLiteral stringLiteral)
            {
                _razorContent.Append($" {attributeName}=\"{stringLiteral.Value}\"");
            }
            else if (attr.Value is Identifier identifier)
            {
                // Check if this is an event handler
                if (attributeName.StartsWith("@"))
                {
                    _razorContent.Append($" {attributeName}=\"{identifier.Name}\"");
                }
                else
                {
                    _razorContent.Append($" {attributeName}=\"@{identifier.Name}\"");
                }
            }
            else if (attr.Value is BinaryExpression || attr.Value is UnaryExpression)
            {
                _razorContent.Append($" {attributeName}=\"@({GenerateExpression(attr.Value)})\"");
            }
            else
            {
                _razorContent.Append($" {attributeName}=\"@{GenerateExpression(attr.Value)}\"");
            }
        }

        /// <summary>
        /// Generates conditional markup using @if directive
        /// </summary>
        private void GenerateConditionalMarkup(ConditionalRender conditional)
        {
            _razorContent.AppendLine($"@if ({GenerateExpression(conditional.Condition)})");
            _razorContent.AppendLine("{");
            
            foreach (var item in conditional.ThenBody)
            {
                GenerateMarkup(item);
            }
            
            _razorContent.AppendLine("}");
            
            if (conditional.ElseBody != null && conditional.ElseBody.Count > 0)
            {
                _razorContent.AppendLine("else");
                _razorContent.AppendLine("{");
                
                foreach (var item in conditional.ElseBody)
                {
                    GenerateMarkup(item);
                }
                
                _razorContent.AppendLine("}");
            }
        }

        /// <summary>
        /// Generates iterative markup using @foreach directive
        /// </summary>
        private void GenerateIterativeMarkup(IterativeRender iterative)
        {
            var collectionExpr = GenerateExpression(iterative.Collection);
            
            if (iterative.Condition != null)
            {
                // Use LINQ Where for conditional iteration
                var conditionExpr = GenerateExpression(iterative.Condition);
                _razorContent.AppendLine($"@foreach (var {iterative.Variable} in {collectionExpr}.Where({iterative.Variable} => {conditionExpr}))");
            }
            else
            {
                _razorContent.AppendLine($"@foreach (var {iterative.Variable} in {collectionExpr})");
            }
            
            _razorContent.AppendLine("{");
            
            foreach (var item in iterative.Body)
            {
                GenerateMarkup(item);
            }
            
            _razorContent.AppendLine("}");
        }

        /// <summary>
        /// Generates component instance usage
        /// </summary>
        private void GenerateComponentInstance(ComponentInstance instance)
        {
            _razorContent.Append($"<{instance.Name}");
            
            // Generate component parameters
            foreach (var prop in instance.Props)
            {
                GenerateAttribute(prop);
            }
            
            if (instance.Children != null && instance.Children.Count > 0)
            {
                _razorContent.AppendLine(">");
                
                foreach (var child in instance.Children)
                {
                    GenerateMarkup(child);
                }
                
                _razorContent.AppendLine($"</{instance.Name}>");
            }
            else
            {
                _razorContent.AppendLine(" />");
            }
        }

        /// <summary>
        /// Generates the @code section for the Blazor component
        /// </summary>
        private void GenerateCodeSection(ComponentDeclaration component)
        {
            // Generate component parameters
            GenerateParameters(component.Parameters);
            
            // Generate state declarations
            GenerateStateDeclarations(component.State);
            
            // Generate lifecycle methods
            GenerateLifecycleMethods(component);
            
            // Generate event handlers
            GenerateEventHandlers(component.Events);
            
            // Generate service injections
            GenerateServiceInjections();
        }

        /// <summary>
        /// Generates Blazor [Parameter] declarations
        /// </summary>
        private void GenerateParameters(List<Parameter>? parameters)
        {
            if (parameters == null || parameters.Count == 0) return;
            
            _codeSection.AppendLine("    // Component Parameters");
            
            foreach (var param in parameters)
            {
                var blazorType = MapCadenzaTypeToBlazor(param.Type);
                _codeSection.AppendLine($"    [Parameter] public {blazorType} {param.Name} {{ get; set; }}");
            }
            
            _codeSection.AppendLine();
        }

        /// <summary>
        /// Generates private fields for state variables
        /// </summary>
        private void GenerateStateDeclarations(List<StateDeclaration>? stateDeclarations)
        {
            if (stateDeclarations == null || stateDeclarations.Count == 0) return;
            
            _codeSection.AppendLine("    // Component State");
            
            foreach (var state in stateDeclarations)
            {
                var blazorType = MapCadenzaTypeToBlazor(state.Type);
                _stateVariables[state.Name] = blazorType;
                
                if (state.InitialValue != null)
                {
                    var initialValue = GenerateExpression(state.InitialValue);
                    _codeSection.AppendLine($"    private {blazorType} {state.Name} = {initialValue};");
                }
                else
                {
                    _codeSection.AppendLine($"    private {blazorType} {state.Name};");
                }
            }
            
            _codeSection.AppendLine();
        }

        /// <summary>
        /// Generates lifecycle methods like OnInitializedAsync
        /// </summary>
        private void GenerateLifecycleMethods(ComponentDeclaration component)
        {
            if (component.OnMount != null)
            {
                _codeSection.AppendLine("    // Component Lifecycle");
                _codeSection.AppendLine("    protected override async Task OnInitializedAsync()");
                _codeSection.AppendLine("    {");
                
                GenerateStatements(component.OnMount);
                
                _codeSection.AppendLine("    }");
                _codeSection.AppendLine();
            }
        }

        /// <summary>
        /// Generates event handler methods
        /// </summary>
        private void GenerateEventHandlers(List<EventHandler>? eventHandlers)
        {
            if (eventHandlers == null || eventHandlers.Count == 0) return;
            
            _codeSection.AppendLine("    // Event Handlers");
            
            foreach (var handler in eventHandlers)
            {
                var returnType = handler.Effects?.Contains("Network") == true ? "Task" : "void";
                var asyncModifier = returnType == "Task" ? "async " : "";
                
                _codeSection.AppendLine($"    private {asyncModifier}{returnType} {handler.Name}()");
                _codeSection.AppendLine("    {");
                
                foreach (var statement in handler.Body)
                {
                    GenerateStatements(statement);
                }
                
                _codeSection.AppendLine("    }");
                _codeSection.AppendLine();
                
                _eventHandlers.Add(handler.Name);
            }
        }

        /// <summary>
        /// Generates service injection statements
        /// </summary>
        private void GenerateServiceInjections()
        {
            if (_usedServices.Count == 0) return;
            
            _codeSection.AppendLine("    // Service Injections");
            
            foreach (var service in _usedServices)
            {
                var serviceType = MapEffectToBlazorService(service);
                _codeSection.AppendLine($"    [Inject] private {serviceType} {service}Service {{ get; set; }}");
            }
            
            _codeSection.AppendLine();
        }

        /// <summary>
        /// Maps Cadenza element names to HTML tags
        /// </summary>
        private string MapCadenzaElementToHtml(string flowLangElement)
        {
            return flowLangElement switch
            {
                "container" => "div",
                "heading" => "h1",
                "button" => "button",
                "text_input" => "input",
                "text" => "span",
                "image" => "img",
                "list" => "ul",
                "list_item" => "li",
                "table" => "table",
                "table_row" => "tr",
                "table_header" => "th",
                "table_cell" => "td",
                _ => flowLangElement
            };
        }

        /// <summary>
        /// Maps Cadenza attributes to Blazor attributes
        /// </summary>
        private string MapCadenzaAttributeToBlazor(string flowLangAttribute)
        {
            return flowLangAttribute switch
            {
                "on_click" => "@onclick",
                "on_change" => "@onchange",
                "on_input" => "@oninput",
                "on_submit" => "@onsubmit",
                "class" => "class",
                "id" => "id",
                "text" => "value",
                "placeholder" => "placeholder",
                "disabled" => "disabled",
                "src" => "src",
                "alt" => "alt",
                _ => flowLangAttribute
            };
        }

        /// <summary>
        /// Maps Cadenza types to Blazor/C# types
        /// </summary>
        private string MapCadenzaTypeToBlazor(string flowLangType)
        {
            return flowLangType switch
            {
                "string" => "string",
                "int" => "int",
                "bool" => "bool",
                "float" => "float",
                "double" => "double",
                "List<string>" => "List<string>",
                "List<int>" => "List<int>",
                "Option<string>" => "string?",
                "Option<int>" => "int?",
                _ => flowLangType
            };
        }

        /// <summary>
        /// Maps Cadenza effects to Blazor services
        /// </summary>
        private string MapEffectToBlazorService(string effect)
        {
            return effect switch
            {
                "Network" => "HttpClient",
                "LocalStorage" => "IJSRuntime",
                "Database" => "IDbContext",
                "Logging" => "ILogger",
                "DOM" => "IJSRuntime",
                _ => $"I{effect}Service"
            };
        }

        /// <summary>
        /// Adds using statements for effects
        /// </summary>
        private void AddEffectUsing(string effect)
        {
            _usedServices.Add(effect);
            
            var usingStatement = effect switch
            {
                "Network" => "@using System.Net.Http",
                "LocalStorage" => "@using Microsoft.JSInterop",
                "Database" => "@using Microsoft.EntityFrameworkCore",
                "Logging" => "@using Microsoft.Extensions.Logging",
                "DOM" => "@using Microsoft.JSInterop",
                _ => $"@using Cadenza.Services"
            };
            
            if (!_razorContent.ToString().Contains(usingStatement))
            {
                _razorContent.Insert(0, usingStatement + Environment.NewLine);
            }
        }

        /// <summary>
        /// Generates C# expression code from Cadenza AST
        /// </summary>
        private string GenerateExpression(ASTNode expression)
        {
            return expression switch
            {
                Identifier id => id.Name,
                StringLiteral str => $"\"{str.Value}\"",
                NumberLiteral num => num.Value.ToString(),
                BooleanLiteral boolean => boolean.Value.ToString().ToLower(),
                BinaryExpression binary => $"({GenerateExpression(binary.Left)} {GetOperatorSymbol(binary.Operator)} {GenerateExpression(binary.Right)})",
                UnaryExpression unary => $"{GetUnaryOperatorSymbol(unary.Operator)}{GenerateExpression(unary.Operand)}",
                FunctionCall call => $"{call.Name}({string.Join(", ", call.Arguments.Select(GenerateExpression))})",
                MemberAccess member => $"{GenerateExpression(member.Object)}.{member.Member}",
                _ => expression.ToString()
            };
        }

        /// <summary>
        /// Generates statements for code blocks
        /// </summary>
        private void GenerateStatements(ASTNode statement)
        {
            if (statement is Assignment assignment)
            {
                _codeSection.AppendLine($"        {assignment.Target} = {GenerateExpression(assignment.Value)};");
            }
            else if (statement is FunctionCall call)
            {
                var callExpr = GenerateExpression(call);
                if (call.Name == "set_state")
                {
                    // Handle Cadenza set_state calls
                    var args = call.Arguments;
                    if (args.Count == 2)
                    {
                        var stateVar = GenerateExpression(args[0]);
                        var newValue = GenerateExpression(args[1]);
                        _codeSection.AppendLine($"        {stateVar} = {newValue};");
                        _codeSection.AppendLine($"        StateHasChanged();");
                    }
                }
                else
                {
                    _codeSection.AppendLine($"        {callExpr};");
                }
            }
            else if (statement is IfStatement ifStmt)
            {
                _codeSection.AppendLine($"        if ({GenerateExpression(ifStmt.Condition)})");
                _codeSection.AppendLine("        {");
                foreach (var stmt in ifStmt.ThenBody)
                {
                    GenerateStatements(stmt);
                }
                _codeSection.AppendLine("        }");
                
                if (ifStmt.ElseBody != null)
                {
                    _codeSection.AppendLine("        else");
                    _codeSection.AppendLine("        {");
                    foreach (var stmt in ifStmt.ElseBody)
                    {
                        GenerateStatements(stmt);
                    }
                    _codeSection.AppendLine("        }");
                }
            }
        }

        /// <summary>
        /// Generates render block content
        /// </summary>
        private void GenerateRenderBlockContent(RenderBlock renderBlock)
        {
            // Handle render block as container
            foreach (var item in renderBlock.Items)
            {
                GenerateMarkup(item);
            }
        }

        /// <summary>
        /// Gets C# operator symbol from Cadenza operator
        /// </summary>
        private string GetOperatorSymbol(string flowLangOperator)
        {
            return flowLangOperator switch
            {
                "+" => "+",
                "-" => "-",
                "*" => "*",
                "/" => "/",
                "==" => "==",
                "!=" => "!=",
                ">" => ">",
                "<" => "<",
                ">=" => ">=",
                "<=" => "<=",
                "&&" => "&&",
                "||" => "||",
                _ => flowLangOperator
            };
        }

        /// <summary>
        /// Gets C# unary operator symbol
        /// </summary>
        private string GetUnaryOperatorSymbol(string flowLangOperator)
        {
            return flowLangOperator switch
            {
                "!" => "!",
                "-" => "-",
                "+" => "+",
                _ => flowLangOperator
            };
        }
    }

    /// <summary>
    /// Represents a render block AST node
    /// </summary>
    public record RenderBlock(List<ASTNode> Items) : ASTNode;
}