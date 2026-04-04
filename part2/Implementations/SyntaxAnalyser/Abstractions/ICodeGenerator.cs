namespace SyntaxAnalyser.Abstractions;

public interface ICodeGenerator
{
    string CompileClass();
    void CompileClassVarDec();
    string CompileSubroutine();
    string CompileParameterList();
    void CompileVarDec();
    string CompileStatements();
    string CompileDo();
    string CompileLet();
    string CompileWhile();
    string CompileReturn();
    string CompileIf();
    string CompileExpression();
    string CompileTerm();
    string CompileExpressionList();
    string CompileSubroutineBody();
}
