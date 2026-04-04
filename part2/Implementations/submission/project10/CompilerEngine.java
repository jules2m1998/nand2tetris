import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;

final class CompilerEngine {
    private static final Set<String> CLASS_VAR_KEYWORDS = new HashSet<String>(
        Arrays.asList("field", "static"));
    private static final Set<String> SUBROUTINE_KEYWORDS = new HashSet<String>(
        Arrays.asList("method", "function", "constructor"));
    private static final Set<String> STATEMENT_KEYWORDS = new HashSet<String>(
        Arrays.asList("let", "do", "if", "while", "return"));
    private static final Set<String> KEYWORD_CONSTANTS = new HashSet<String>(
        Arrays.asList("true", "false", "null", "this"));
    private static final Set<String> OPERATORS = new HashSet<String>(
        Arrays.asList("+", "-", "*", "/", "&", "|", "<", ">", "="));

    private final Tokenizer tokenizer;

    CompilerEngine(Tokenizer tokenizer) {
        this.tokenizer = tokenizer;
    }

    String compileClass() {
        XmlWriter builder = openXmlWriter("class");

        Token currentToken = tokenizer.eat("class");
        builder.appendLine(applyCurrent(currentToken.getValue(), currentToken.getType()));

        String identifier = tokenizer.getIdentifier();
        builder.appendLine(applyCurrent(identifier));
        tokenizer.advance();

        currentToken = tokenizer.eat("{");
        builder.appendLine(applyCurrent(currentToken.getValue(), currentToken.getType()));

        while (isCurrentKeyword(CLASS_VAR_KEYWORDS)) {
            builder.appendLines(compileClassVarDec());
        }

        while (isCurrentKeyword(SUBROUTINE_KEYWORDS)) {
            builder.appendLines(compileSubroutine());
        }

        currentToken = tokenizer.eat("}");
        builder.appendLine(applyCurrent(currentToken.getValue(), currentToken.getType()));

        closeXmlWriter(builder, "class");
        return builder.toString();
    }

    String compileClassVarDec() {
        XmlWriter builder = openXmlWriter("classVarDec");

        String keyword = tokenizer.getKeyWord();
        builder.appendLine(applyCurrent(keyword));
        tokenizer.advance();

        String type = tryCompileType(false);
        if (type == null) {
            throw new IllegalStateException("Expected identifier or a token");
        }

        builder.appendLine(type);
        tokenizer.advance();

        appendIdentifier(builder);

        while (isCurrentValue(",")) {
            Token comma = tokenizer.eat(",");
            builder.appendLine(applyCurrent(comma.getValue(), comma.getType()));
            appendIdentifier(builder);
        }

        Token end = tokenizer.eat(";");
        builder.appendLine(applyCurrent(end.getValue(), end.getType()));

        closeXmlWriter(builder, "classVarDec");
        return builder.toString();
    }

    String compileSubroutine() {
        XmlWriter builder = openXmlWriter("subroutineDec");

        String keyword = tokenizer.getKeyWord();
        builder.appendLine(applyCurrent(keyword));
        tokenizer.advance();

        String type = tryCompileType(true);
        if (type == null) {
            throw new IllegalStateException("Expected identifier or a token");
        }

        builder.appendLine(type);
        tokenizer.advance();

        appendIdentifier(builder);

        Token openBracket = tokenizer.eat("(");
        builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));

        builder.appendLines(compileParameterList());

        Token closeBracket = tokenizer.eat(")");
        builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));

        builder.appendLines(compileSubroutineBody());

        closeXmlWriter(builder, "subroutineDec");
        return builder.toString();
    }

    String compileParameterList() {
        XmlWriter builder = openXmlWriter("parameterList");

        if (!isCurrentValue(")")) {
            String type = tryCompileType(false);
            if (type == null) {
                throw new IllegalStateException("Expected identifier or a token");
            }

            while (tryCompileType(false) != null) {
                builder.appendLines(compileParameter());
            }
        }

        closeXmlWriter(builder, "parameterList");
        return builder.toString();
    }

    String compileVarDec() {
        XmlWriter builder = openXmlWriter("varDec");

        Token varToken = tokenizer.eat("var");
        builder.appendLine(applyCurrent(varToken.getValue(), varToken.getType()));

        String type = tryCompileType(false);
        if (type == null) {
            throw new IllegalStateException("Expected identifier or a token");
        }

        builder.appendLine(type);
        tokenizer.advance();

        appendIdentifier(builder);

        while (isCurrentValue(",")) {
            Token comma = tokenizer.eat(",");
            builder.appendLine(applyCurrent(comma.getValue(), comma.getType()));
            appendIdentifier(builder);
        }

        Token semicolon = tokenizer.eat(";");
        builder.appendLine(applyCurrent(semicolon.getValue(), semicolon.getType()));

        closeXmlWriter(builder, "varDec");
        return builder.toString();
    }

    String compileStatements() {
        XmlWriter builder = openXmlWriter("statements");

        while (isCurrentKeyword(STATEMENT_KEYWORDS)) {
            String statementKeyword = tokenizer.getCurrentToken().getValue();
            if ("let".equals(statementKeyword)) {
                builder.appendLines(compileLet());
            } else if ("do".equals(statementKeyword)) {
                builder.appendLines(compileDo());
            } else if ("if".equals(statementKeyword)) {
                builder.appendLines(compileIf());
            } else if ("while".equals(statementKeyword)) {
                builder.appendLines(compileWhile());
            } else if ("return".equals(statementKeyword)) {
                builder.appendLines(compileReturn());
            } else {
                throw new IllegalStateException("Unexpected token " + statementKeyword);
            }
        }

        closeXmlWriter(builder, "statements");
        return builder.toString();
    }

    String compileDo() {
        XmlWriter builder = openXmlWriter("doStatement");

        Token doToken = tokenizer.eat("do");
        builder.appendLine(applyCurrent(doToken.getValue(), doToken.getType()));

        appendSubroutineCall(builder);

        eatSemicolon(builder);

        closeXmlWriter(builder, "doStatement");
        return builder.toString();
    }

    String compileLet() {
        XmlWriter builder = openXmlWriter("letStatement");

        Token letToken = tokenizer.eat("let");
        builder.appendLine(applyCurrent(letToken.getValue(), letToken.getType()));

        appendIdentifier(builder);

        if (isCurrentValue("[")) {
            Token openBracket = tokenizer.eat("[");
            builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));
            builder.appendLines(compileExpression());
            Token closeBracket = tokenizer.eat("]");
            builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));
        }

        Token equals = tokenizer.eat("=");
        builder.appendLine(applyCurrent(equals.getValue(), equals.getType()));

        builder.appendLines(compileExpression());

        eatSemicolon(builder);

        closeXmlWriter(builder, "letStatement");
        return builder.toString();
    }

    String compileWhile() {
        XmlWriter builder = openXmlWriter("whileStatement");

        Token whileToken = tokenizer.eat("while");
        builder.appendLine(applyCurrent(whileToken.getValue(), whileToken.getType()));

        Token openBracket = tokenizer.eat("(");
        builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));

        builder.appendLines(compileExpression());

        Token closeBracket = tokenizer.eat(")");
        builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));

        Token openBrace = tokenizer.eat("{");
        builder.appendLine(applyCurrent(openBrace.getValue(), openBrace.getType()));

        builder.appendLines(compileStatements());

        Token closeBrace = tokenizer.eat("}");
        builder.appendLine(applyCurrent(closeBrace.getValue(), closeBrace.getType()));

        closeXmlWriter(builder, "whileStatement");
        return builder.toString();
    }

    String compileReturn() {
        XmlWriter builder = openXmlWriter("returnStatement");

        Token returnToken = tokenizer.eat("return");
        builder.appendLine(applyCurrent(returnToken.getValue(), returnToken.getType()));

        if (!isCurrentValue(";")) {
            builder.appendLines(compileExpression());
        }

        eatSemicolon(builder);

        closeXmlWriter(builder, "returnStatement");
        return builder.toString();
    }

    String compileIf() {
        XmlWriter builder = openXmlWriter("ifStatement");

        Token ifToken = tokenizer.eat("if");
        builder.appendLine(applyCurrent(ifToken.getValue(), ifToken.getType()));

        Token openBracket = tokenizer.eat("(");
        builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));

        builder.appendLines(compileExpression());

        Token closeBracket = tokenizer.eat(")");
        builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));

        Token openBrace = tokenizer.eat("{");
        builder.appendLine(applyCurrent(openBrace.getValue(), openBrace.getType()));

        builder.appendLines(compileStatements());

        Token closeBrace = tokenizer.eat("}");
        builder.appendLine(applyCurrent(closeBrace.getValue(), closeBrace.getType()));

        if (isCurrentValue("else")) {
            builder.appendLines(compileElse());
        }

        closeXmlWriter(builder, "ifStatement");
        return builder.toString();
    }

    String compileExpression() {
        if (!isTermStart()) {
            return "";
        }

        XmlWriter builder = openXmlWriter("expression");

        builder.appendLines(compileTerm());

        while (isCurrentOperator()) {
            Token operator = tokenizer.advance();
            builder.appendLine(applyCurrent(operator.getValue(), operator.getType()));
            builder.appendLines(compileTerm());
        }

        closeXmlWriter(builder, "expression");
        return builder.toString();
    }

    String compileTerm() {
        if (!isTermStart()) {
            return "";
        }

        XmlWriter builder = openXmlWriter("term");

        if (tokenizer.getTokenType() == TokenType.STRING_CONSTANT) {
            builder.appendLine(applyCurrent(tokenizer.getStringValue(), TokenType.STRING_CONSTANT));
            tokenizer.advance();
        } else if (tokenizer.getTokenType() == TokenType.INTEGER_CONSTANT || isCurrentKeyword(KEYWORD_CONSTANTS)) {
            Token token = tokenizer.advance();
            builder.appendLine(applyCurrent(token.getValue(), token.getType()));
        } else if (isCurrentValue("-") || isCurrentValue("~")) {
            Token unary = tokenizer.advance();
            builder.appendLine(applyCurrent(unary.getValue(), unary.getType()));
            builder.appendLines(compileTerm());
        } else if (tokenizer.getTokenType() == TokenType.IDENTIFIER) {
            String identifier = tokenizer.getIdentifier();
            builder.appendLine(applyCurrent(identifier));
            tokenizer.advance();

            if (isCurrentValue("[")) {
                Token openArray = tokenizer.eat("[");
                builder.appendLine(applyCurrent(openArray.getValue(), openArray.getType()));
                builder.appendLines(compileExpression());
                Token closeArray = tokenizer.eat("]");
                builder.appendLine(applyCurrent(closeArray.getValue(), closeArray.getType()));
            } else if (isCurrentValue("(")) {
                Token openBracket = tokenizer.eat("(");
                builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));
                builder.appendLines(compileExpressionList());
                Token closeBracket = tokenizer.eat(")");
                builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));
            } else if (isCurrentValue(".")) {
                Token dot = tokenizer.eat(".");
                builder.appendLine(applyCurrent(dot.getValue(), dot.getType()));
                appendIdentifier(builder);
                Token openBracket = tokenizer.eat("(");
                builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));
                builder.appendLines(compileExpressionList());
                Token closeBracket = tokenizer.eat(")");
                builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));
            }
        } else if (isCurrentValue("(")) {
            Token openBracket = tokenizer.eat("(");
            builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));
            builder.appendLines(compileExpression());
            Token closeBracket = tokenizer.eat(")");
            builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));
        }

        closeXmlWriter(builder, "term");
        return builder.toString();
    }

    String compileExpressionList() {
        XmlWriter builder = openXmlWriter("expressionList");

        if (!isCurrentValue(")")) {
            builder.appendLines(compileExpression());

            while (isCurrentValue(",")) {
                Token comma = tokenizer.eat(",");
                builder.appendLine(applyCurrent(comma.getValue(), comma.getType()));
                builder.appendLines(compileExpression());
            }
        }

        closeXmlWriter(builder, "expressionList");
        return builder.toString();
    }

    private String compileSubroutineBody() {
        XmlWriter builder = openXmlWriter("subroutineBody");

        Token openBrace = tokenizer.eat("{");
        builder.appendLine(applyCurrent(openBrace.getValue(), openBrace.getType()));

        while (isCurrentValue("var")) {
            builder.appendLines(compileVarDec());
        }

        builder.appendLines(compileStatements());

        Token closeBrace = tokenizer.eat("}");
        builder.appendLine(applyCurrent(closeBrace.getValue(), closeBrace.getType()));

        closeXmlWriter(builder, "subroutineBody");
        return builder.toString();
    }

    private String compileParameter() {
        XmlWriter builder = new XmlWriter();

        String type = tryCompileType(false);
        if (type == null) {
            throw new IllegalStateException("Expected identifier or a token");
        }

        builder.appendLine(type);
        tokenizer.advance();

        appendIdentifier(builder);

        if (isCurrentValue(",")) {
            Token comma = tokenizer.eat(",");
            builder.appendLine(applyCurrent(comma.getValue(), comma.getType()));
        }

        return builder.toString();
    }

    private String compileElse() {
        XmlWriter builder = new XmlWriter();

        Token elseToken = tokenizer.eat("else");
        builder.appendLine(applyCurrent(elseToken.getValue(), elseToken.getType()));

        Token openBrace = tokenizer.eat("{");
        builder.appendLine(applyCurrent(openBrace.getValue(), openBrace.getType()));

        builder.appendLines(compileStatements());

        Token closeBrace = tokenizer.eat("}");
        builder.appendLine(applyCurrent(closeBrace.getValue(), closeBrace.getType()));

        return builder.toString();
    }

    private void appendIdentifier(XmlWriter builder) {
        String identifier = tokenizer.getIdentifier();
        builder.appendLine(applyCurrent(identifier));
        tokenizer.advance();
    }

    private void appendSubroutineCall(XmlWriter builder) {
        appendIdentifier(builder);

        if (isCurrentValue(".")) {
            Token dot = tokenizer.eat(".");
            builder.appendLine(applyCurrent(dot.getValue(), dot.getType()));
            appendIdentifier(builder);
        }

        Token openBracket = tokenizer.eat("(");
        builder.appendLine(applyCurrent(openBracket.getValue(), openBracket.getType()));

        builder.appendLines(compileExpressionList());

        Token closeBracket = tokenizer.eat(")");
        builder.appendLine(applyCurrent(closeBracket.getValue(), closeBracket.getType()));
    }

    private void eatSemicolon(XmlWriter builder) {
        Token semicolon = tokenizer.eat(";");
        builder.appendLine(applyCurrent(semicolon.getValue(), semicolon.getType()));
    }

    private boolean isTermStart() {
        if (!tokenizer.hasMoreTokens()) {
            return false;
        }

        TokenType tokenType = tokenizer.getTokenType();
        if (tokenType == TokenType.INTEGER_CONSTANT
            || tokenType == TokenType.STRING_CONSTANT
            || tokenType == TokenType.IDENTIFIER) {
            return true;
        }

        if (tokenType == TokenType.KEYWORD && KEYWORD_CONSTANTS.contains(tokenizer.getCurrentToken().getValue())) {
            return true;
        }

        return tokenType == TokenType.SYMBOL && (isCurrentValue("(") || isCurrentValue("-") || isCurrentValue("~"));
    }

    private boolean isCurrentOperator() {
        return tokenizer.hasMoreTokens()
            && tokenizer.getTokenType() == TokenType.SYMBOL
            && OPERATORS.contains(tokenizer.getCurrentToken().getValue());
    }

    private boolean isCurrentValue(String value) {
        return tokenizer.hasMoreTokens() && tokenizer.getCurrentToken().getValue().equals(value);
    }

    private boolean isCurrentKeyword(Set<String> keywords) {
        return tokenizer.hasMoreTokens()
            && tokenizer.getTokenType() == TokenType.KEYWORD
            && keywords.contains(tokenizer.getCurrentToken().getValue());
    }

    private String tryCompileType(boolean allowVoid) {
        if (!tokenizer.hasMoreTokens()) {
            return null;
        }

        if (tokenizer.getTokenType() == TokenType.IDENTIFIER) {
            return applyCurrent(tokenizer.getCurrentToken().getValue());
        }

        if (tokenizer.getTokenType() == TokenType.KEYWORD) {
            String keyword = tokenizer.getCurrentToken().getValue();
            if ("int".equals(keyword) || "boolean".equals(keyword) || "char".equals(keyword)
                || (allowVoid && "void".equals(keyword))) {
                return applyCurrent(keyword);
            }
        }

        return null;
    }

    private String applyCurrent(String value) {
        return applyCurrent(value, tokenizer.getTokenType());
    }

    private String applyCurrent(String value, TokenType currentTokenType) {
        return getOpenTag(currentTokenType) + " " + escapeXml(value) + " " + getCloseTag(currentTokenType);
    }

    private static String getOpenTag(TokenType type) {
        if (type == TokenType.KEYWORD) {
            return "<keyword>";
        }
        if (type == TokenType.IDENTIFIER) {
            return "<identifier>";
        }
        if (type == TokenType.SYMBOL) {
            return "<symbol>";
        }
        if (type == TokenType.INTEGER_CONSTANT) {
            return "<integerConstant>";
        }
        if (type == TokenType.STRING_CONSTANT) {
            return "<stringConstant>";
        }

        throw new IllegalArgumentException("Unsupported token type: " + type);
    }

    private static String getCloseTag(TokenType type) {
        if (type == TokenType.KEYWORD) {
            return "</keyword>";
        }
        if (type == TokenType.IDENTIFIER) {
            return "</identifier>";
        }
        if (type == TokenType.SYMBOL) {
            return "</symbol>";
        }
        if (type == TokenType.INTEGER_CONSTANT) {
            return "</integerConstant>";
        }
        if (type == TokenType.STRING_CONSTANT) {
            return "</stringConstant>";
        }

        throw new IllegalArgumentException("Unsupported token type: " + type);
    }

    private static String escapeXml(String value) {
        return value
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;");
    }

    private static XmlWriter openXmlWriter(String tag) {
        XmlWriter builder = new XmlWriter();
        builder.appendLine("<" + tag + ">");
        builder.indent();
        return builder;
    }

    private static void closeXmlWriter(XmlWriter builder, String tag) {
        builder.unindent();
        builder.appendLine("</" + tag + ">");
    }
}
