using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;
using System.Text;

namespace SyntaxAnalyser.Services;

public class CodeGenerator(ITokenizer tokenizer) : ICodeGenerator
{
    private readonly ISymbolTable _symbolTable = new SymbolTable();
    private string _currentClassName = string.Empty;
    private string _currentSubroutine = string.Empty;
    private int _expCount = 0;

    public string CompileClass()
    {
        var vmWriter = new VmWriter();
        
        _symbolTable.Clear();

        _ = tokenizer.Eat("class");
        
        _currentClassName = tokenizer.Identifier;
        tokenizer.Advance();
        
        
        _ = tokenizer.Eat("{");
        while (tokenizer is { HasMoreTokens: true, CurrentToken.Value: "field" or "static" })
        {
            CompileClassVarDec();
        }
        
        // CompileSubroutines
        while (tokenizer is { HasMoreTokens: true, CurrentToken.Value: "method" or "function" or "constructor" })
        {
            vmWriter.AppendLines(CompileSubroutine());
        }
        _ = tokenizer.Eat("}");
        
        return vmWriter.ToString();
    }
    
    public void CompileClassVarDec()
    {
        var kind = ParseSymbolKind(tokenizer.CurrentToken.Value);
        tokenizer.Advance();

        var type = TryCompileType();
        tokenizer.Advance();

        while (tokenizer is {HasMoreTokens: true, CurrentToken.Type: TokenType.Identifier})
        {
            var name = tokenizer.CurrentToken.Value;
            _symbolTable.Define(name, type, kind);
            tokenizer.Advance();
            
        }
    }
    public string CompileSubroutine()
    {
        _symbolTable.StartSubroutine();
        var vmWriter = new VmWriter();
        
        var kind = TryCompileSubroutineType();
        tokenizer.Advance();
        
        var type = TryCompileType(allowVoid: true);
        tokenizer.Advance();
        
        _currentSubroutine = tokenizer.Identifier;
        tokenizer.Advance();

        if (kind ==  SubroutineType.Method)
        {
            _symbolTable.Define("this", _currentClassName, SymbolKind.Argument);
        }
        
        var name = $"{_currentClassName}.{_currentSubroutine}";
        
        vmWriter.Indent();
        _ = tokenizer.Eat("(");
        _ = CompileParameterList();
        _ = tokenizer.Eat(")");
        vmWriter.Unindent();
        _ = tokenizer.Eat("{");
        var body = CompileSubroutineBody();
        _ = tokenizer.Eat("}");
        
        
        

        var argCount = _symbolTable.VarCount(SymbolKind.Var);
        var fn = $"function {name} {argCount}";
        vmWriter.AppendLines(fn);
        if (kind ==  SubroutineType.Constructor)
        {
            var fieldCount = _symbolTable.VarCount(SymbolKind.Field);
            vmWriter.AppendLine(Push(fieldCount));
            vmWriter.AppendLine("call Memory.alloc 1");
            vmWriter.AppendLine("pop pointer 0");
        }
        else if (kind ==  SubroutineType.Method)
        {
            vmWriter.AppendLine("push argument 0");
            vmWriter.AppendLine("pop pointer 0");
        }
        vmWriter.AppendLines(body);
        
        vmWriter.Unindent();
        
        

        return vmWriter.ToString();
    }
    
    public string CompileParameterList()
    {
        if (tokenizer is { TokenType: TokenType.Symbol, Symbol: ')' })
        {
            return string.Empty;
        }
        SaveArgument();
        while (tokenizer is { TokenType: TokenType.Symbol, Symbol: ',' })
        {
            tokenizer.Advance();
            SaveArgument();
        }
        return string.Empty;
    }
    private void SaveArgument()
    {
        var type = TryCompileType();
        tokenizer.Advance();
        var name = tokenizer.Identifier;
        tokenizer.Advance();
        _symbolTable.Define(name, type, SymbolKind.Argument);
    }

    public void CompileVarDec()
    {
        _ = tokenizer.Eat("var");
        var type = TryCompileType();
        tokenizer.Advance();
        
        var name = tokenizer.Identifier;
        tokenizer.Advance();
        
        _symbolTable.Define(name, type, SymbolKind.Var);

        while (tokenizer.CurrentToken.Value == ",")
        {
            _ = tokenizer.Eat(",");
            
            name = tokenizer.Identifier;
            tokenizer.Advance();
            _symbolTable.Define(name, type, SymbolKind.Var);
        }
        
        _ = tokenizer.Eat(";");
    }
    
    public string CompileStatements()
    {
        var builder = new VmWriter();
        string[] acceptedStatements = ["let", "do", "if", "else", "while", "return"];
        while (acceptedStatements.Contains(tokenizer.CurrentToken.Value))
        {
            var statement = tokenizer.KeyWord switch
            {
                "let" => CompileLet(),
                "do" => CompileDo(),
                "if" => CompileIf(),
                "while" => CompileWhile(),
                "return" => CompileReturn(),
                _ => throw new Exception($"Unexpected token {tokenizer.KeyWord}")
            };
            
            builder.AppendLines(statement);
        }
        
        return builder.ToString();
    }
    public string CompileDo()
    {
        var builder = new VmWriter();
        builder.Indent();
        _ = tokenizer.Eat("do");
        var exp = CompileExpression();
        _ = tokenizer.Eat(";");
        builder.AppendLines(exp);
        builder.AppendLine("pop temp 0");
        builder.Unindent();
        return builder.ToString();
    }
    public string CompileLet()
    {
        _ = tokenizer.Eat("let");
        var builder = new VmWriter();
        var isArr = false;
        
        var identifier = tokenizer.Identifier;
        tokenizer.Advance();
        if (tokenizer is { TokenType: TokenType.Symbol, Symbol: '[' })
        {
            isArr = true;
            tokenizer.Advance();
            builder.AppendLine(Push(identifier));
            builder.AppendLine(CompileExpression());
            builder.AppendLine("add");
            _ = tokenizer.Eat("]");
        }
        
        _ = tokenizer.Eat("=");
        
        builder.AppendLines(CompileExpression());
        if (isArr)
        {
            builder.AppendLine("pop temp 0");
            builder.AppendLine("pop pointer 1");
            builder.AppendLine("push temp 0");
            builder.AppendLine("pop that 0");
        }
        else
        {
            
            var kind = _symbolTable.KindOf(identifier);
            builder.AppendLine(Pop(kind, _symbolTable.IndexOf(identifier)));
        }
        _ = tokenizer.Eat(";");
        
        return builder.ToString();
    }
    public string CompileWhile()
    {
        var builder = new VmWriter();
        var scopeName = GetScope();
        var loopStartLabel = $"while.{scopeName}.{tokenizer.Index}";
        var loopEndLabel = $"end.while.{scopeName}.{tokenizer.Index}";
        
        
        _ = tokenizer.Eat("while");
        builder.AppendLine($"label {loopStartLabel}");
        
        _ = tokenizer.Eat("(");
        builder.AppendLines(CompileExpression());
        _ = tokenizer.Eat(")");
        builder.AppendLine(Not());
        builder.AppendLine($"if-goto {loopEndLabel}");
        
        _ = tokenizer.Eat("{");
        
        builder.AppendLines(CompileStatements());
        
        _ = tokenizer.Eat("}");
        builder.AppendLine($"goto {loopStartLabel}");
        builder.AppendLine($"label {loopEndLabel}");
        
        return builder.ToString();
    }
    public string CompileReturn()
    {
        _ = tokenizer.Eat("return");
        var builder = new VmWriter();
        builder.Indent();
        var exp = CompileExpression();
        builder.AppendLine(!string.IsNullOrEmpty(exp) ? exp : Push(0));
        _ = tokenizer.Eat(";");
        builder.AppendLine("return");
        builder.Unindent();
        return builder.ToString();
    }
    public string CompileIf()
    {
        var builder = new VmWriter();
        var scopeName = GetScope();
        var ifTrueLabel = $"if.{scopeName}_{tokenizer.Index}";
        var ifFalseLabel = $"else.{scopeName}_{tokenizer.Index}";
        var ifEndLabel = $"else.end.{scopeName}_{tokenizer.Index}";
        
        
        _ = tokenizer.Eat("if");
        _ = tokenizer.Eat("(");
        builder.AppendLines(CompileExpression());
        _ = tokenizer.Eat(")");
        builder.AppendLine($"if-goto {ifTrueLabel}");
        builder.AppendLine($"goto {ifFalseLabel}");
        
        _ = tokenizer.Eat("{");
        builder.Indent();
        builder.AppendLine($"label {ifTrueLabel}");
        builder.AppendLines(CompileStatements());
        _ = tokenizer.Eat("}");
        builder.Unindent();
        
        if (tokenizer.CurrentToken.Value == "else")
        {
            builder.AppendLine($"goto {ifEndLabel}");
        }
        builder.AppendLines($"label {ifFalseLabel}");

        if (tokenizer.CurrentToken.Value != "else")
            return builder.ToString();
        _ = tokenizer.Eat("else");
        builder.AppendLines(CompileElse());
        builder.AppendLines($"label {ifEndLabel}");

        return builder.ToString();
    }
    private string GetScope()
    {

        string[] scopeParts = [_currentClassName, _currentSubroutine];
        var scopeName = string.Join(".", scopeParts.Where(s => !string.IsNullOrWhiteSpace(s)));
        return scopeName;
    }
    private string CompileElse()
    {
        var builder = new VmWriter();
        _ = tokenizer.Eat("{");
        builder.Indent();
        builder.AppendLines(CompileStatements());
        builder.Unindent();
        _ = tokenizer.Eat("}");
        
        return builder.ToString();
    }
    public string CompileExpression()
    {
        var builder = new VmWriter();
        builder.Indent();
        var term = string.Empty;
        do
        {
            var isNotNeg = false;
            if (!string.IsNullOrWhiteSpace(term) && tokenizer is { TokenType: TokenType.Symbol, Symbol: '-' })
            {
                isNotNeg = true;
                _ = tokenizer.Eat("-");
            }
            term = CompileTerm();
            if (term != string.Empty)
            {
                builder.AppendLine(term);
            }
            if (isNotNeg)
            {
                builder.AppendLine(Sub());
            }
        } while (term != string.Empty);
        builder.Unindent();
        return builder.ToString();
    }
    private static string Not() => "not";
    private static string Div() => "call Math.divide 2";
    private static string Mult() => "call Math.multiply 2";
    private static string Add() => "add";
    private static string Neg() => "neg";
    private string Push(string variable)
    {
        var kind = _symbolTable.KindOf(variable);
        var index = _symbolTable.IndexOf(variable);
        var term = kind switch
        {
            SymbolKind.Var => "local",
            SymbolKind.Argument => "argument",
            SymbolKind.Static => "static",
            SymbolKind.Field => "this",
            _ => throw new ArgumentOutOfRangeException()
        };
        return $"push {term} {index}";
    }
    public string CompileTerm()
    {
        if (tokenizer.CurrentToken.Value == ";")
        {
            return string.Empty;
        }
        
        var builder = new VmWriter();
        builder.Indent();
        
        string[] acceptedKeywordConst = ["true", "false", "null", "this"];
        char[] acceptedSymbol = ['-', '~', '+', '*', '/', '<', '>', '&', '=', '|'];
        
        switch (tokenizer.TokenType)
        {
            case TokenType.Keyword when acceptedKeywordConst.Contains(tokenizer.KeyWord):
            {
                var query = tokenizer.KeyWord switch
                {
                    "true" => "push constant 0\nnot",
                    "false" or "null" => "push constant 0",
                    "this" => "push pointer 0",
                    _ => throw new ArgumentOutOfRangeException()
                };
                builder.AppendLine(query);
                tokenizer.Advance();
                break;
            }
            case TokenType.IntegerConstant:
                builder.AppendLine(Push(tokenizer.IntValue));
                tokenizer.Advance();
                break;
            case TokenType.Symbol when acceptedSymbol.Contains(tokenizer.Symbol):
            {
                var query = tokenizer.Symbol switch
                {
                    '~' => Not(),
                    '-' => Neg(),
                    '+' => Add(),
                    '*' => Mult(),
                    '/' => Div(),
                    '<' => Lt(),
                    '>' => Gt(),
                    '&' => And(),
                    '=' => Equal(),
                    '|' => Or(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                tokenizer.Advance();
                builder.AppendLines(CompileExpression());
                builder.AppendLine(query);
                break;
            }
            case TokenType.StringConstant:
            case TokenType.Identifier:
            default:
            {
                if (tokenizer is { TokenType: TokenType.StringConstant})
                {
                    var str =  tokenizer.StringValue.Select(c => (int)c).ToArray();
                    tokenizer.Advance();
                    builder.AppendLine(Push(str.Length));
                    builder.AppendLine("call String.new 1");
                    foreach (var c in str)
                    {
                        builder.AppendLine(Push(c));
                        builder.AppendLine("call String.appendChar 2");
                    }
                }
                else if (tokenizer.TokenType == TokenType.Identifier)
                {
                    var identifier = tokenizer.Identifier;
                    tokenizer.Advance();
                    var subIdentifier = string.Empty;

                    if (tokenizer is not { TokenType: TokenType.Symbol, Symbol: '.' or '(' or '[' })
                    {
                        builder.AppendLine(Push(identifier));
                    }

                    while (tokenizer is { TokenType: TokenType.Symbol, Symbol: '.' or '(' or '[' })
                    {
                        var symbol = tokenizer.Symbol;
                        tokenizer.Advance();
                        switch (symbol)
                        {
                            case '.':
                                subIdentifier = tokenizer.Identifier;
                                tokenizer.Advance();
                                break;
                            case '(':
                                string[] callNames = [identifier, subIdentifier];
                                var b = new StringBuilder();
                                var type = _symbolTable.TypeOf(identifier);
                                b.Append("call ");
                                var exp = CompileExpressionList();
                                if (callNames.Count(x => !string.IsNullOrWhiteSpace(x)) == 1)
                                {
                                    builder.AppendLine("push pointer 0");
                                    b.Append($"{_currentClassName}.{identifier}");
                                    b.AppendLine($" {_expCount + 1}");
                                }
                                else
                                {
                                    var val = !string.IsNullOrWhiteSpace(type) ? 1 : 0;
                                    if (!string.IsNullOrWhiteSpace(type))
                                    {
                                        builder.AppendLine("push pointer 0");
                                    }
                                    b.Append($"{type ?? identifier}.{subIdentifier}");
                            
                                    b.Append($" {_expCount + val}");
                                }
                                builder.AppendLines(exp);
                                _ = tokenizer.Eat(")");
                                builder.AppendLine(b.ToString());
                                _expCount = 0;
                                break;
                            case  '[':
                                var arrayExp =  CompileExpressionList();
                                _ = tokenizer.Eat("]");
                                builder.AppendLine(Push(identifier));
                                builder.AppendLines(arrayExp);
                                builder.AppendLine(Add());
                                builder.AppendLine("pop pointer 1\npush that 0");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                else if (tokenizer is { TokenType: TokenType.Symbol, Symbol: '(' })
                {
                    _ = tokenizer.Eat("(");
                    var expression = CompileExpression();
                    builder.AppendLine(expression);
                    _ = tokenizer.Eat(")");
                }
                break;
            }
        }
        
        builder.Unindent();
        return builder.ToString();
    }
    private static string Or() => "or";
    private static string Equal() => "eq";
    private static string And() => "and";
    private static string Gt()
    {
        return "gt";
    }
    private static string Lt()
    {
        return "lt";
    }
    private static string Sub()
    {
        return "sub";
    }
    private static string PushThis()
    {
        return "push pointer 0";
    }
    private static string Push(int variable)
    {
        return $"push constant {variable}";
    }
    public string CompileExpressionList()
    {
        var builder = new VmWriter();
        builder.Indent();
        
        var exp = CompileExpression();
        builder.AppendLines(exp);
        _expCount = 1;
        
        while (tokenizer is { TokenType: TokenType.Symbol, Symbol: ',' })
        {
            _expCount++;
            tokenizer.Advance();
            
            exp =  CompileExpression();
            if (string.IsNullOrEmpty(exp))
                continue;
            builder.AppendLines(exp);
        }
        
        builder.Unindent();
        return builder.ToString();
    }
    
    private static string Pop(SymbolKind? argument, int? index)
    {
        var term = argument switch
        {
            SymbolKind.Var => "local",
            SymbolKind.Argument => "argument",
            SymbolKind.Static => "static",
            SymbolKind.Field => "this",
            _ => throw new ArgumentOutOfRangeException()
        };
        return $"pop {term} {index}";
    }
    public string CompileSubroutineBody()
    {
        while (tokenizer.CurrentToken.Value == "var")
        {
            CompileVarDec();
        }
        
        var builder = new VmWriter();
        builder.Indent();
        builder.AppendLines(CompileStatements());
        builder.Unindent();
        return builder.ToString();
    }

    private string TryCompileType(bool allowVoid = false)
    {
        if (!tokenizer.HasMoreTokens)
        {
            throw new InvalidOperationException($"Unexpected end of tokens.");
        }

        if (tokenizer.TokenType == TokenType.Identifier)
        {
            return tokenizer.CurrentToken.Value;
        }

        string[] acceptedKeyword = allowVoid
            ? ["int", "boolean", "char", "void"]
            : ["int", "boolean", "char"];

        return tokenizer.TokenType == TokenType.Keyword && acceptedKeyword.Contains(tokenizer.CurrentToken.Value)
            ? tokenizer.CurrentToken.Value
            : throw new InvalidOperationException($"Invalid token '{tokenizer.CurrentToken.Value}'");
    }
    
    private SubroutineType TryCompileSubroutineType()
    {
        if (!tokenizer.HasMoreTokens)
        {
            throw new InvalidOperationException($"Unexpected end of tokens.");
        }
        var value = tokenizer.KeyWord;
        return value switch
        {
            "function" => SubroutineType.Function,
            "constructor" => SubroutineType.Constructor,
            "method" => SubroutineType.Method,
            _ => throw new InvalidOperationException($"Invalid subroutine '{tokenizer.CurrentToken.Value}'")
        };
    }
    

    private static SymbolKind ParseSymbolKind(ReadOnlySpan<char> span)
    {
        return span switch
        {
            "field" => SymbolKind.Field,
            "static" => SymbolKind.Static,
            "var" => SymbolKind.Var,
            _ => throw new InvalidOperationException($"Invalid symbol kind {span}")
        };
    }
}

internal enum SubroutineType
{
    Function,
    Constructor,
    Method
}
