namespace VMTranslator.Abstractions;

public interface IVmLineTranslator
{
    /// <summary>
    /// Translate a VM code to hack assembler
    /// </summary>
    /// <example>
    /// <code>
    /// var vmTranslator = new VmLineTranslatorImp();
    /// var result = vmTranslator.Translate("push argument 1"); // should generate the assembly code to push arg 1 in the stack
    /// </code>
    /// </example>
    /// <param name="line">The VM line of code to be translated</param>
    /// <param name="lineNumber">The current line</param>
    /// <param name="fileName">The current VM file basename used for static symbols</param>
    /// <returns>The hack assembler result</returns>
    string[]  Translate(string line, int lineNumber = 0, string fileName = "FileName");
}
