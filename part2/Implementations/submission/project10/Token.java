final class Token {
    private final TokenType type;
    private final String value;

    Token(TokenType type, String value) {
        this.type = type;
        this.value = value;
    }

    TokenType getType() {
        return type;
    }

    String getValue() {
        return value;
    }
}
