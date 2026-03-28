using SyntaxAnalyser.Abstractions;
using SyntaxAnalyser.Models;
using System.Text;
namespace SyntaxAnalyser.Services;

public class CompilerEngine(ITokenizer tokenizer) : ICompilerEngine
{
    private static string GetOpenTag(TokenType type)
    {
        return type switch
        {
            TokenType.Keyword => "<keyword>",
            TokenType.Identifier => "<identifier>",
            TokenType.Symbol => "<symbol>",
            TokenType.IntegerConstant => "<integerConstant>",
            TokenType.StringConstant => "<stringConstant>",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    private static string GetCloseTag(TokenType type)
    {
        return type switch
        {
            TokenType.Keyword => "</keyword>",
            TokenType.Identifier => "</identifier>",
            TokenType.Symbol => "</symbol>",
            TokenType.IntegerConstant => "</integerConstant>",
            TokenType.StringConstant => "</stringConstant>",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public string CompileClass()
    {
        var builder = OpenXmlWriter("<class>");
        
        var currentToken = tokenizer.Eat("class");
        builder.AppendLine(ApplyCurrent(currentToken.Value, currentToken.Type));
        
        var identifier = tokenizer.Identifier;
        builder.AppendLine(ApplyCurrent(identifier));
        tokenizer.Advance();
        
        currentToken = tokenizer.Eat("{");
        builder.AppendLine(ApplyCurrent(currentToken.Value, currentToken.Type));
        
        // CompileClassVarDec
        while (tokenizer is { HasMoreTokens: true, CurrentToken.Value: "field" or "static" })
        {
            builder.AppendLines(CompileClassVarDec());
        }
        
        // CompileSubroutines
        while (tokenizer is { HasMoreTokens: true, CurrentToken.Value: "method" or "function" or "constructor" })
        {
            builder.AppendLines(CompileSubroutine());
        }
        
        currentToken = tokenizer.Eat("}");
        builder.AppendLine(ApplyCurrent(currentToken.Value, currentToken.Type));
        
        CloseXmlWriter(builder, "</class>");
        return builder.ToString();
    }
    
    private string ApplyCurrent(string value, TokenType? currentTokenType = null)
    {
        var builder = new StringBuilder();
        builder.Append(GetOpenTag(currentTokenType ?? tokenizer.TokenType));
        builder.Append($" {value} ");
        builder.Append(GetCloseTag(currentTokenType ?? tokenizer.TokenType));
        return builder.ToString();
    }
    
    public string CompileClassVarDec()
    {
        var builder = OpenXmlWriter("<classVarDec>");
        builder.Indent();
        var keyword = tokenizer.KeyWord;
        builder.AppendLine(ApplyCurrent(keyword));
        tokenizer.Advance();

        if (tokenizer.HasMoreTokens && (tokenizer.CurrentToken.Type == TokenType.Identifier || tokenizer.CurrentToken.Type == TokenType.Keyword && tokenizer.KeyWord is "int" or "boolean" or "char"))
        {
            builder.AppendLine(ApplyCurrent(tokenizer.CurrentToken.Value));
            tokenizer.Advance();
        }
        else
        {
            throw  new Exception("Expected identifier or a token");
        }
        while (tokenizer is { HasMoreTokens: true, CurrentToken.Type: TokenType.Identifier})
        {
            var identifier = tokenizer.Identifier;
            builder.AppendLine(ApplyCurrent(identifier));
            tokenizer.Advance();

            if (tokenizer is { HasMoreTokens: true, Symbol: ',' })
            {
                builder.AppendLine(ApplyCurrent(tokenizer.Symbol.ToString()));
                tokenizer.Advance();
            }
            else
            {
                break;
            }
        }
        var end = tokenizer.Eat(";");
        builder.AppendLine(ApplyCurrent(end.Value, end.Type));
        
        CloseXmlWriter(builder, "</classVarDec>");
        return  builder.ToString();
    }
    
    public string CompileSubroutine()
    {
        var builder = OpenXmlWriter("<subroutineDec>");
        
        var keyword = tokenizer.KeyWord;
        string[] subroutines = ["function", "constructor", "method", "function"];
        
        builder.AppendLine(ApplyCurrent(keyword));
        tokenizer.Advance();
            
        var type = EatType();
        if (type == null)
        {
            throw  new Exception("Expected identifier or a token");
        }
        builder.AppendLine(type);
        tokenizer.Advance();
            
        var methodName = tokenizer.Identifier;
        builder.AppendLine(ApplyCurrent(methodName));
        tokenizer.Advance();
    
        var bracket = tokenizer.Eat("(");
        builder.AppendLine(ApplyCurrent(bracket.Value, bracket.Type));
        builder.Append(CompileParameterList());
        var closeBracket = tokenizer.Eat(")");
        builder.AppendLine(ApplyCurrent(closeBracket.Value, bracket.Type));
        
        builder.Append(CompileSubroutineBody());
        
        CloseXmlWriter(builder, "</subroutineDec>");
        
        return builder.ToString();
    }
    
    private string CompileSubroutineBody()
    {
        var builder = OpenXmlWriter("<subroutineBody>");
        var curlyBrace = tokenizer.Eat("{");
        builder.AppendLine(ApplyCurrent(curlyBrace.Value, curlyBrace.Type));
        builder.Indent();

        while (tokenizer.CurrentToken.Value == "var")
        {
            builder.AppendLines(CompileVarDec());
        }
        builder.AppendLines(CompileStatements());
        
        
        builder.Unindent();
        var closeBrace = tokenizer.Eat("}");
        builder.AppendLine(ApplyCurrent(closeBrace.Value, closeBrace.Type));

        
        CloseXmlWriter(builder, "</subroutineBody>");
        return builder.ToString();
    }

    private string? EatType()
    {
        
        string[] acceptedKeyword = ["int", "boolean", "char", "void"];
        if (
            tokenizer is { HasMoreTokens: true, CurrentToken.Type: TokenType.Identifier or TokenType.Keyword } || 
            (tokenizer.TokenType == TokenType.Identifier && acceptedKeyword.Contains(tokenizer.CurrentToken.Value))
        )
        {
            return ApplyCurrent(tokenizer.CurrentToken.Value);
        }
        return null;
    }

    public string CompileParameterList()
    {
        var builder = new XmlWriter();
        builder.AppendLine("<parameterList>");
        builder.Indent();
        while (EatType() != null)
        {
            builder.Append(CompileParameter());
        }
        builder.Unindent();
        builder.AppendLine("</parameterList>");
        return builder.ToString();
    }
    
    private string CompileParameter()
    {
        var builder = new XmlWriter();
        
        // write the type
        var type = EatType();
        if (type == null)
        {
            throw  new Exception("Expected identifier or a token");
        }
        builder.AppendLine(type);
        tokenizer.Advance();
        
        
        // write the param's name
        var identifier = tokenizer.Identifier;
        builder.AppendLine(ApplyCurrent(identifier));
        tokenizer.Advance();
        
        // write the ',' if there is one
        if (tokenizer.CurrentToken.Value != ",")
            return builder.ToString();
        
        var token = tokenizer.Eat(",");
        builder.AppendLine(ApplyCurrent(token.Value, token.Type));

        return builder.ToString();
    }
    
    public string CompileVarDec()
    {
        var builder = OpenXmlWriter("<varDec>");

        var vaL = tokenizer.Eat("var");
        builder.AppendLine(ApplyCurrent(vaL.Value, vaL.Type));
        
        
        var type = EatType();
        if (type == null)
            throw  new Exception("Expected identifier or a token");
        builder.AppendLine(type);
        tokenizer.Advance();
        
        var identifier = tokenizer.Identifier;
        builder.AppendLine(ApplyCurrent(identifier));
        tokenizer.Advance();

        while (tokenizer.CurrentToken.Value == ",")
        {
            builder.AppendLine(ApplyCurrent(tokenizer.Symbol.ToString()));
            tokenizer.Advance();
            
            identifier = tokenizer.Identifier;
            builder.AppendLine(ApplyCurrent(identifier));
            tokenizer.Advance();
        }

        var semicolon = tokenizer.Eat(";");
        builder.AppendLine(ApplyCurrent(semicolon.Value, semicolon.Type));
        
        CloseXmlWriter(builder, "</varDec>");
        return builder.ToString();
    }
    private static void CloseXmlWriter(XmlWriter builder, string tag)
    {

        builder.Unindent();
        builder.AppendLine(tag);
    }
    private static XmlWriter OpenXmlWriter(string tag)
    {

        var builder = new XmlWriter();
        builder.AppendLine(tag);
        builder.Indent();
        return builder;
    }
    public string CompileStatements()
    {
        var builder = OpenXmlWriter("<statements>");
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
        
        
        CloseXmlWriter(builder, "</statements>");
        return builder.ToString();
    }
    
    public string CompileDo()
    {
        var builder = OpenXmlWriter("<doStatement>");
        
        
        var token = tokenizer.Eat("do");
        builder.AppendLine(ApplyCurrent(token.Value, token.Type));
        
        var identifier = tokenizer.Identifier;
        builder.AppendLine(ApplyCurrent(identifier));
        tokenizer.Advance();
        
        while (tokenizer.CurrentToken.Value == ".")
        {
            builder.AppendLine(ApplyCurrent(tokenizer.Symbol.ToString()));
            tokenizer.Advance();
            
            identifier = tokenizer.Identifier;
            builder.AppendLine(ApplyCurrent(identifier));
            tokenizer.Advance();
        }
        
        var openBracket = tokenizer.Eat("(");
        builder.AppendLine(ApplyCurrent(openBracket.Value, openBracket.Type));
        
        builder.AppendLines(CompileExpressionList());
        
        var closeBracket = tokenizer.Eat(")");
        builder.AppendLine(ApplyCurrent(closeBracket.Value, closeBracket.Type));
        EatSemicolon(builder);


        CloseXmlWriter(builder, "</doStatement>");
        return builder.ToString();
    }
    private void EatSemicolon(XmlWriter builder)
    {
        var semicolon = tokenizer.Eat(";");
        builder.AppendLine(ApplyCurrent(semicolon.Value, semicolon.Type));
    }
    public string CompileLet()
    {
        var builder = OpenXmlWriter("<letStatement>");
        
        
        var firstToken = tokenizer.Eat("let");
        builder.AppendLine(ApplyCurrent(firstToken.Value, firstToken.Type));
        
        var identifier = tokenizer.Identifier;
        builder.AppendLine(ApplyCurrent(identifier));
        tokenizer.Advance();

        if (tokenizer.Symbol == '[')
        {
            builder.AppendLine(ApplyCurrent(tokenizer.Symbol.ToString()));
            tokenizer.Advance();
            
            builder.AppendLines(CompileExpression());
            
            builder.AppendLine(ApplyCurrent(tokenizer.Symbol.ToString()));
            tokenizer.Advance();
        }
        var eq =  tokenizer.Eat("=");
        builder.AppendLine(ApplyCurrent(eq.Value, eq.Type));
            
        builder.AppendLines(CompileExpression());
        
        EatSemicolon(builder);
        
        CloseXmlWriter(builder, "</letStatement>");
        return builder.ToString();
    }
    public string CompileWhile()
    {
        
        var builder = OpenXmlWriter("<whileStatement>");
        
        
        var firstToken = tokenizer.Eat("while");
        builder.AppendLine(ApplyCurrent(firstToken.Value, firstToken.Type));
        
        var bracket = tokenizer.Eat("(");
        builder.AppendLine(ApplyCurrent(bracket.Value, bracket.Type));
        
            
        builder.AppendLines(CompileExpression());
        
        var closeBracket = tokenizer.Eat(")");
        builder.AppendLine(ApplyCurrent(closeBracket.Value, bracket.Type));
        
        var openCurlyBrace = tokenizer.Eat("{");
        builder.AppendLine(ApplyCurrent(openCurlyBrace.Value, openCurlyBrace.Type));
        
        builder.AppendLines(CompileStatements());
        
        var closeCurlyBrace = tokenizer.Eat("}");
        builder.AppendLine(ApplyCurrent(closeCurlyBrace.Value, closeCurlyBrace.Type));
        
        CloseXmlWriter(builder, "</whileStatement>");
        return builder.ToString();
    }
    
    public string CompileReturn()
    {
        var builder = OpenXmlWriter("<returnStatement>");
        
        
        var firstToken = tokenizer.Eat("return");
        builder.AppendLine(ApplyCurrent(firstToken.Value, firstToken.Type));
        
        builder.AppendLines(CompileExpression());
        
        EatSemicolon(builder);
        CloseXmlWriter(builder, "</returnStatement>");
        return builder.ToString();
    }
    
    public string CompileIf()
    {
        var builder = OpenXmlWriter("<ifStatement>");
        
        
        var firstToken = tokenizer.Eat("if");
        builder.AppendLine(ApplyCurrent(firstToken.Value, firstToken.Type));
        
        var bracket = tokenizer.Eat("(");
        builder.AppendLine(ApplyCurrent(bracket.Value, bracket.Type));
        
            
        builder.AppendLines(CompileExpression());
        
        var closeBracket = tokenizer.Eat(")");
        builder.AppendLine(ApplyCurrent(closeBracket.Value, bracket.Type));
        
        var openCurlyBrace = tokenizer.Eat("{");
        builder.AppendLine(ApplyCurrent(openCurlyBrace.Value, openCurlyBrace.Type));
        
        builder.AppendLines(CompileStatements());
        
        var closeCurlyBrace = tokenizer.Eat("}");
        builder.AppendLine(ApplyCurrent(closeCurlyBrace.Value, closeCurlyBrace.Type));

        if (tokenizer.CurrentToken.Value == "else")
        {
            builder.AppendLines(CompileElse());
        }
        
        CloseXmlWriter(builder, "</ifStatement>");
        return builder.ToString();
    }

    private string CompileElse()
    {
        var builder = new XmlWriter();
        
        var keyword = tokenizer.Eat("else");
        builder.AppendLine(ApplyCurrent(keyword.Value, keyword.Type));
        
        var openCurlyBrace = tokenizer.Eat("{");
        builder.AppendLine(ApplyCurrent(openCurlyBrace.Value, openCurlyBrace.Type));
        
        builder.AppendLines(CompileStatements());
        
        var closeCurlyBrace = tokenizer.Eat("}");
        builder.AppendLine(ApplyCurrent(closeCurlyBrace.Value, closeCurlyBrace.Type));
        
        return builder.ToString();
    }
    
    public string CompileExpression()
    {
        var builder = OpenXmlWriter("<expression>");
        var term = CompileTerm();
        if (string.IsNullOrEmpty(term) && tokenizer.CurrentToken.Value != "-")
        {
            return string.Empty;
        }
        else if (string.IsNullOrEmpty(term) && tokenizer.CurrentToken.Value == "-")
        {
            builder.AppendLine("<term>");
            
            var symbol = tokenizer.Symbol;
            builder.AppendLine(ApplyCurrent(symbol.ToString()));
            tokenizer.Advance();
            
            builder.AppendLines(CompileTerm());
            
            builder.AppendLine("</term>");
        }
        builder.AppendLines(term);
        string[] ops = ["+", "-", "*", "/", "&", "|", "<", ">", "=", "~"];
        var opMap = new Dictionary<char, string>
        {
            ['<'] = "&lt;",
            ['>'] = "&gt;",
            ['&'] = "&amp;",
        };
        
        while (tokenizer.TokenType == TokenType.Symbol && ops.Contains(tokenizer.CurrentToken.Value))
        {
            var symbol = tokenizer.Symbol;
            builder.AppendLine(opMap.TryGetValue(symbol, out var value) ? ApplyCurrent(value) : ApplyCurrent(symbol.ToString()));
            tokenizer.Advance();

            builder.AppendLines(CompileTerm());
        }
        
        CloseXmlWriter(builder, "</expression>");
        return builder.ToString();
    }
    
    public string CompileTerm()
    {
        if (tokenizer.CurrentToken.Value == ";")
        {
            return string.Empty;
        }
        
        var builder = OpenXmlWriter("<term>");
        
        string[] acceptedKeywordConst = ["true", "false", "null", "this"];
        
        
        if (tokenizer.TokenType == TokenType.StringConstant)
        {
            builder.AppendLine(ApplyCurrent(tokenizer.StringValue));
            tokenizer.Advance();
        } 
        else if (tokenizer.TokenType is TokenType.IntegerConstant ||
            tokenizer.TokenType == TokenType.Keyword && acceptedKeywordConst.Contains(tokenizer.CurrentToken.Value) || 
            tokenizer is { TokenType: TokenType.Symbol, CurrentToken.Value: "~" })
        {
            builder.AppendLine(ApplyCurrent(tokenizer.CurrentToken.Value));
            tokenizer.Advance();
        } 
        else switch (tokenizer)
        {
            case { TokenType: TokenType.Identifier }:
            {
                builder.AppendLine(ApplyCurrent(tokenizer.CurrentToken.Value));
                tokenizer.Advance();
                
                while (tokenizer is { TokenType: TokenType.Symbol, CurrentToken.Value: "." or "(" or "[" })
                {
                    switch (tokenizer.Symbol)
                    {
                        case '.':
                        {
                            builder.AppendLine(ApplyCurrent(tokenizer.Symbol.ToString()));
                            tokenizer.Advance();
        
                            var identifier = tokenizer.Identifier;
                            builder.AppendLine(ApplyCurrent(identifier));
                            tokenizer.Advance();
                            continue;
                        }
                        case '(':
                        {
                            var openBracket = tokenizer.Eat("(");
                            builder.AppendLine(ApplyCurrent(openBracket.Value, openBracket.Type));
                    
                            builder.AppendLines(CompileExpressionList());
    
                            var closeBracket = tokenizer.Eat(")");
                            builder.AppendLine(ApplyCurrent(closeBracket.Value, closeBracket.Type));
                            continue;
                        }
                    }

                    var openArray = tokenizer.Eat("[");
                    builder.AppendLine(ApplyCurrent(openArray.Value, openArray.Type));
            
                    builder.AppendLines(CompileExpression());
            
                    var closeArray = tokenizer.Eat("]");
                    builder.AppendLine(ApplyCurrent(closeArray.Value, closeArray.Type));
                    
                }
                break;
            }
            case { TokenType: TokenType.Symbol, CurrentToken.Value: "(" }:
            {
                var openArray = tokenizer.Eat("(");
                builder.AppendLine(ApplyCurrent(openArray.Value, openArray.Type));
            
                builder.AppendLines(CompileExpression());
            
                var closeArray = tokenizer.Eat(")");
                builder.AppendLine(ApplyCurrent(closeArray.Value, closeArray.Type));
                break;
            }
            default:
            {
                return string.Empty;
            }
        }
        var nextTerm = CompileTerm();
        
        if (nextTerm != string.Empty)
        {
            builder.AppendLines(nextTerm);
        }
        
        CloseXmlWriter(builder, "</term>");
        return builder.ToString();
    }

    public string CompileExpressionList()
    {
        
        var builder = OpenXmlWriter("<expressionList>");
        
        var exp =  CompileExpression();
        if (!string.IsNullOrEmpty(exp))
        {
            builder.AppendLines(exp);
        }
        
        while (tokenizer.CurrentToken.Value == ",")
        {
            builder.AppendLine(ApplyCurrent(tokenizer.CurrentToken.Value));
            tokenizer.Advance();
            
            exp =  CompileExpression();
            if (!string.IsNullOrEmpty(exp))
            {
                builder.AppendLines(exp);
            }
        }
        
        CloseXmlWriter(builder, "</expressionList>");
        return builder.ToString();
    }
}
