namespace SyntaxAnalyser.Abstractions;

public interface ICompilerEngine
{
    string CompileClass();
    string CompileClassVarDec();
    string CompileSubroutine();
    string CompileParameterList();
    string CompileVarDec();
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
