using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;

namespace SyntaxAnalyser.Services;

public class CodeGenerator(ITokenizer tokenizer) : ICodeGenerator
{
    private readonly ISymbolTable _symbolTable = new SymbolTable();
    private string _currentClassName = string.Empty;
    private string _currentSubroutine = string.Empty;
    private int _ifCounter;
    private int _whileCounter;

    public string CompileClass()
    {
        var writer = new VmWriter();

        _symbolTable.Clear();

        _ = tokenizer.Eat("class");
        _currentClassName = tokenizer.Identifier;
        tokenizer.Advance();

        _ = tokenizer.Eat("{");

        while (tokenizer is { HasMoreTokens: true, CurrentToken.Value: "static" or "field" })
        {
            CompileClassVarDec();
        }

        while (tokenizer is { HasMoreTokens: true, CurrentToken.Value: "constructor" or "function" or "method" })
        {
            writer.AppendLines(CompileSubroutine());
        }

        _ = tokenizer.Eat("}");
        return writer.ToString();
    }

    public void CompileClassVarDec()
    {
        var kind = ParseClassScopeKind(tokenizer.CurrentToken.Value);
        tokenizer.Advance();

        var type = TryCompileType();
        tokenizer.Advance();

        DefineNamedSymbols(type, kind);
        _ = tokenizer.Eat(";");
    }

    public string CompileSubroutine()
    {
        _symbolTable.StartSubroutine();
        _ifCounter = 0;
        _whileCounter = 0;

        var kind = ParseSubroutineType(tokenizer.CurrentToken.Value);
        tokenizer.Advance();

        _ = TryCompileType(allowVoid: true);
        tokenizer.Advance();

        _currentSubroutine = tokenizer.Identifier;
        tokenizer.Advance();

        if (kind == SubroutineType.Method)
        {
            _symbolTable.Define("this", _currentClassName, SymbolKind.Argument);
        }

        _ = tokenizer.Eat("(");
        _ = CompileParameterList();
        _ = tokenizer.Eat(")");

        _ = tokenizer.Eat("{");
        var body = CompileSubroutineBody();
        _ = tokenizer.Eat("}");

        var writer = new VmWriter();
        writer.WriteFunction($"{_currentClassName}.{_currentSubroutine}", _symbolTable.VarCount(SymbolKind.Var));

        switch (kind)
        {
            case SubroutineType.Constructor:
                writer.WritePush("constant", _symbolTable.VarCount(SymbolKind.Field));
                writer.WriteCall("Memory.alloc", 1);
                writer.WritePop("pointer", 0);
                break;
            case SubroutineType.Method:
                writer.WritePush("argument", 0);
                writer.WritePop("pointer", 0);
                break;
        }

        writer.AppendLines(body);
        return writer.ToString();
    }

    public string CompileParameterList()
    {
        if (tokenizer is { TokenType: TokenType.Symbol, Symbol: ')' })
            return string.Empty;

        DefineParameter();

        while (tokenizer is { TokenType: TokenType.Symbol, Symbol: ',' })
        {
            tokenizer.Advance();
            DefineParameter();
        }

        return string.Empty;
    }

    public void CompileVarDec()
    {
        _ = tokenizer.Eat("var");
        var type = TryCompileType();
        tokenizer.Advance();

        DefineNamedSymbols(type, SymbolKind.Var);
        _ = tokenizer.Eat(";");
    }

    public string CompileStatements()
    {
        var writer = new VmWriter();

        while (tokenizer is { TokenType: TokenType.Keyword, CurrentToken.Value: "let" or "if" or "while" or "do" or "return" })
        {
            writer.AppendLines(tokenizer.KeyWord switch
            {
                "let" => CompileLet(),
                "if" => CompileIf(),
                "while" => CompileWhile(),
                "do" => CompileDo(),
                "return" => CompileReturn(),
                _ => throw new InvalidOperationException($"Unexpected statement token '{tokenizer.CurrentToken.Value}'.")
            });
        }

        return writer.ToString();
    }

    public string CompileDo()
    {
        _ = tokenizer.Eat("do");
        var call = CompileSubroutineCall();
        _ = tokenizer.Eat(";");

        var writer = new VmWriter();
        writer.AppendLines(call.Vm);
        writer.WritePop("temp", 0);
        return writer.ToString();
    }

    public string CompileLet()
    {
        _ = tokenizer.Eat("let");
        var variableName = tokenizer.Identifier;
        tokenizer.Advance();

        var writer = new VmWriter();

        if (tokenizer is { TokenType: TokenType.Symbol, Symbol: '[' })
        {
            tokenizer.Advance();
            var indexExpression = CompileExpression();
            _ = tokenizer.Eat("]");
            _ = tokenizer.Eat("=");
            var valueExpression = CompileExpression();
            _ = tokenizer.Eat(";");

            writer.AppendLines(indexExpression);
            writer.AppendLines(PushNamed(variableName));
            writer.WriteArithmetic("add");
            writer.AppendLines(valueExpression);
            writer.WritePop("temp", 0);
            writer.WritePop("pointer", 1);
            writer.WritePush("temp", 0);
            writer.WritePop("that", 0);
            return writer.ToString();
        }

        _ = tokenizer.Eat("=");
        writer.AppendLines(CompileExpression());
        _ = tokenizer.Eat(";");
        writer.AppendLines(PopNamed(variableName));
        return writer.ToString();
    }

    public string CompileWhile()
    {
        var labelId = _whileCounter++;
        var loopStartLabel = $"WHILE_EXP{labelId}";
        var loopEndLabel = $"WHILE_END{labelId}";

        _ = tokenizer.Eat("while");
        _ = tokenizer.Eat("(");
        var condition = CompileExpression();
        _ = tokenizer.Eat(")");
        _ = tokenizer.Eat("{");
        var body = CompileStatements();
        _ = tokenizer.Eat("}");

        var writer = new VmWriter();
        writer.WriteLabel(loopStartLabel);
        writer.AppendLines(condition);
        writer.WriteArithmetic("not");
        writer.WriteIf(loopEndLabel);
        writer.AppendLines(body);
        writer.WriteGoto(loopStartLabel);
        writer.WriteLabel(loopEndLabel);
        return writer.ToString();
    }

    public string CompileReturn()
    {
        _ = tokenizer.Eat("return");

        var writer = new VmWriter();
        if (tokenizer is { TokenType: TokenType.Symbol, Symbol: ';' })
        {
            writer.WritePush("constant", 0);
        }
        else
        {
            writer.AppendLines(CompileExpression());
        }

        _ = tokenizer.Eat(";");
        writer.WriteReturn();
        return writer.ToString();
    }

    public string CompileIf()
    {
        var labelId = _ifCounter++;
        var trueLabel = $"IF_TRUE{labelId}";
        var falseLabel = $"IF_FALSE{labelId}";
        var endLabel = $"IF_END{labelId}";

        _ = tokenizer.Eat("if");
        _ = tokenizer.Eat("(");
        var condition = CompileExpression();
        _ = tokenizer.Eat(")");

        _ = tokenizer.Eat("{");
        var ifBody = CompileStatements();
        _ = tokenizer.Eat("}");

        var writer = new VmWriter();
        writer.AppendLines(condition);
        writer.WriteIf(trueLabel);
        writer.WriteGoto(falseLabel);
        writer.WriteLabel(trueLabel);
        writer.AppendLines(ifBody);

        if (tokenizer is { TokenType: TokenType.Keyword, CurrentToken.Value: "else" })
        {
            writer.WriteGoto(endLabel);
            writer.WriteLabel(falseLabel);
            _ = tokenizer.Eat("else");
            _ = tokenizer.Eat("{");
            writer.AppendLines(CompileStatements());
            _ = tokenizer.Eat("}");
            writer.WriteLabel(endLabel);
            return writer.ToString();
        }

        writer.WriteLabel(falseLabel);
        return writer.ToString();
    }

    public string CompileExpression()
    {
        var writer = new VmWriter();
        writer.AppendLines(CompileTerm());

        while (TryReadBinaryOperator(out var vmOperation))
        {
            writer.AppendLines(CompileTerm());
            writer.WriteArithmetic(vmOperation);
        }

        return writer.ToString();
    }

    public string CompileTerm()
    {
        if (tokenizer.CurrentToken.Value == ";")
            return string.Empty;

        var writer = new VmWriter();

        switch (tokenizer.TokenType)
        {
            case TokenType.IntegerConstant:
                writer.WritePush("constant", tokenizer.IntValue);
                tokenizer.Advance();
                return writer.ToString();

            case TokenType.StringConstant:
                return CompileStringConstant();

            case TokenType.Keyword:
                return CompileKeywordConstant();

            case TokenType.Symbol when tokenizer.Symbol is '-' or '~':
            {
                var unaryCommand = tokenizer.Symbol == '-' ? "neg" : "not";
                tokenizer.Advance();
                writer.AppendLines(CompileTerm());
                writer.WriteArithmetic(unaryCommand);
                return writer.ToString();
            }

            case TokenType.Symbol when tokenizer.Symbol == '(':
                tokenizer.Advance();
                writer.AppendLines(CompileExpression());
                _ = tokenizer.Eat(")");
                return writer.ToString();

            case TokenType.Identifier:
            {
                var identifier = tokenizer.Identifier;
                tokenizer.Advance();

                if (tokenizer is { TokenType: TokenType.Symbol, Symbol: '[' })
                {
                    tokenizer.Advance();
                    writer.AppendLines(CompileExpression());
                    _ = tokenizer.Eat("]");
                    writer.AppendLines(PushNamed(identifier));
                    writer.WriteArithmetic("add");
                    writer.WritePop("pointer", 1);
                    writer.WritePush("that", 0);
                    return writer.ToString();
                }

                if (tokenizer is { TokenType: TokenType.Symbol, Symbol: '(' or '.' })
                {
                    var call = CompileSubroutineCall(identifier);
                    writer.AppendLines(call.Vm);
                    return writer.ToString();
                }

                writer.AppendLines(PushNamed(identifier));
                return writer.ToString();
            }

            default:
                throw new InvalidOperationException($"Unsupported term starting at token '{tokenizer.CurrentToken.Value}'.");
        }
    }

    public string CompileExpressionList()
    {
        return CompileExpressionListInternal().Vm;
    }

    public string CompileSubroutineBody()
    {
        while (tokenizer is { TokenType: TokenType.Keyword, CurrentToken.Value: "var" })
        {
            CompileVarDec();
        }

        return CompileStatements();
    }

    private string CompileStringConstant()
    {
        var writer = new VmWriter();
        var stringValue = tokenizer.StringValue;

        tokenizer.Advance();

        writer.WritePush("constant", stringValue.Length);
        writer.WriteCall("String.new", 1);

        foreach (var character in stringValue)
        {
            writer.WritePush("constant", character);
            writer.WriteCall("String.appendChar", 2);
        }

        return writer.ToString();
    }

    private string CompileKeywordConstant()
    {
        var writer = new VmWriter();

        switch (tokenizer.KeyWord)
        {
            case "true":
                writer.WritePush("constant", 0);
                writer.WriteArithmetic("not");
                break;
            case "false":
            case "null":
                writer.WritePush("constant", 0);
                break;
            case "this":
                writer.WritePush("pointer", 0);
                break;
            default:
                throw new InvalidOperationException($"Unsupported keyword constant '{tokenizer.KeyWord}'.");
        }

        tokenizer.Advance();
        return writer.ToString();
    }

    private (string Vm, int Count) CompileExpressionListInternal()
    {
        if (tokenizer is { TokenType: TokenType.Symbol, Symbol: ')' })
            return (string.Empty, 0);

        var writer = new VmWriter();
        var count = 0;

        do
        {
            writer.AppendLines(CompileExpression());
            count++;

            if (tokenizer is not { TokenType: TokenType.Symbol, Symbol: ',' })
                break;

            tokenizer.Advance();
        } while (true);

        return (writer.ToString(), count);
    }

    private (string Vm, string FullName, int ArgCount) CompileSubroutineCall()
    {
        var firstIdentifier = tokenizer.Identifier;
        tokenizer.Advance();
        return CompileSubroutineCall(firstIdentifier);
    }

    private (string Vm, string FullName, int ArgCount) CompileSubroutineCall(string firstIdentifier)
    {
        var writer = new VmWriter();
        string fullName;
        int argCount;

        if (tokenizer is { TokenType: TokenType.Symbol, Symbol: '(' })
        {
            tokenizer.Advance();
            writer.WritePush("pointer", 0);
            var (argumentsVm, argumentCount) = CompileExpressionListInternal();
            _ = tokenizer.Eat(")");
            writer.AppendLines(argumentsVm);
            fullName = $"{_currentClassName}.{firstIdentifier}";
            argCount = argumentCount + 1;
        }
        else
        {
            _ = tokenizer.Eat(".");
            var subroutineName = tokenizer.Identifier;
            tokenizer.Advance();
            _ = tokenizer.Eat("(");
            var (argumentsVm, argumentCount) = CompileExpressionListInternal();
            _ = tokenizer.Eat(")");

            var resolvedKind = _symbolTable.KindOf(firstIdentifier);
            if (resolvedKind is null)
            {
                fullName = $"{firstIdentifier}.{subroutineName}";
                argCount = argumentCount;
            }
            else
            {
                writer.AppendLines(PushNamed(firstIdentifier));
                fullName = $"{_symbolTable.TypeOf(firstIdentifier)}.{subroutineName}";
                argCount = argumentCount + 1;
            }

            writer.AppendLines(argumentsVm);
        }

        writer.WriteCall(fullName, argCount);
        return (writer.ToString(), fullName, argCount);
    }

    private bool TryReadBinaryOperator(out string vmOperation)
    {
        vmOperation = string.Empty;

        if (tokenizer.TokenType != TokenType.Symbol)
            return false;

        vmOperation = tokenizer.Symbol switch
        {
            '+' => "add",
            '-' => "sub",
            '*' => "call Math.multiply 2",
            '/' => "call Math.divide 2",
            '&' => "and",
            '|' => "or",
            '<' => "lt",
            '>' => "gt",
            '=' => "eq",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(vmOperation))
            return false;

        tokenizer.Advance();
        return true;
    }

    private void DefineParameter()
    {
        var type = TryCompileType();
        tokenizer.Advance();

        var name = tokenizer.Identifier;
        tokenizer.Advance();

        _symbolTable.Define(name, type, SymbolKind.Argument);
    }

    private void DefineNamedSymbols(string type, SymbolKind kind)
    {
        var name = tokenizer.Identifier;
        tokenizer.Advance();
        _symbolTable.Define(name, type, kind);

        while (tokenizer is { TokenType: TokenType.Symbol, Symbol: ',' })
        {
            tokenizer.Advance();
            name = tokenizer.Identifier;
            tokenizer.Advance();
            _symbolTable.Define(name, type, kind);
        }
    }

    private string PushNamed(string name)
    {
        var kind = _symbolTable.KindOf(name) ?? throw new InvalidOperationException($"Unknown symbol '{name}'.");
        var index = _symbolTable.IndexOf(name) ?? throw new InvalidOperationException($"Unknown symbol '{name}'.");
        return $"push {VmSegmentFor(kind)} {index}";
    }

    private string PopNamed(string name)
    {
        var kind = _symbolTable.KindOf(name) ?? throw new InvalidOperationException($"Unknown symbol '{name}'.");
        var index = _symbolTable.IndexOf(name) ?? throw new InvalidOperationException($"Unknown symbol '{name}'.");
        return $"pop {VmSegmentFor(kind)} {index}";
    }

    private static string VmSegmentFor(SymbolKind kind)
    {
        return kind switch
        {
            SymbolKind.Static => "static",
            SymbolKind.Field => "this",
            SymbolKind.Argument => "argument",
            SymbolKind.Var => "local",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private string TryCompileType(bool allowVoid = false)
    {
        if (!tokenizer.HasMoreTokens)
            throw new InvalidOperationException("Unexpected end of tokens.");

        if (tokenizer.TokenType == TokenType.Identifier)
            return tokenizer.CurrentToken.Value;

        string[] acceptedKeywords = allowVoid
            ? ["int", "boolean", "char", "void"]
            : ["int", "boolean", "char"];

        return tokenizer.TokenType == TokenType.Keyword && acceptedKeywords.Contains(tokenizer.CurrentToken.Value)
            ? tokenizer.CurrentToken.Value
            : throw new InvalidOperationException($"Invalid token '{tokenizer.CurrentToken.Value}'");
    }

    private static SymbolKind ParseClassScopeKind(string token)
    {
        return token switch
        {
            "field" => SymbolKind.Field,
            "static" => SymbolKind.Static,
            _ => throw new InvalidOperationException($"Invalid class variable kind '{token}'.")
        };
    }

    private static SubroutineType ParseSubroutineType(string token)
    {
        return token switch
        {
            "function" => SubroutineType.Function,
            "constructor" => SubroutineType.Constructor,
            "method" => SubroutineType.Method,
            _ => throw new InvalidOperationException($"Invalid subroutine '{token}'.")
        };
    }
}

internal enum SubroutineType
{
    Function,
    Constructor,
    Method
}
