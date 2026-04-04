import java.util.regex.Matcher;
import java.util.regex.Pattern;

final class Tokenizer {
    private static final Pattern INTEGER_CONSTANT_PATTERN = Pattern.compile("^\\d+\\b");
    private static final Pattern IDENTIFIER_PATTERN = Pattern.compile("^[A-Za-z_][A-Za-z0-9_]*\\b");
    private static final Pattern STRING_CONSTANT_PATTERN = Pattern.compile("^\"[^\"\\r\\n]*\"");
    private static final Pattern SYMBOL_PATTERN = Pattern.compile("^(?:[{}()\\[\\].,;+\\-*&|<>=~]|/(?![/*]))");
    private static final Pattern KEYWORD_PATTERN = Pattern.compile(
        "^(class|constructor|function|method|field|static|var|int|char|boolean|void|true|false|null|this|let|do|if|else|while|return)\\b");
    private static final Pattern ONE_LINE_COMMENT_PATTERN = Pattern.compile("^//[^\\r\\n]*");
    private static final Pattern MULTI_LINE_COMMENT_PATTERN = Pattern.compile("^/\\*[\\s\\S]*?\\*/\\s*");
    private static final Pattern WHITE_SPACE_PATTERN = Pattern.compile("^\\s+");

    private static final Pattern[] IGNORED_RULES = new Pattern[] {
        ONE_LINE_COMMENT_PATTERN,
        MULTI_LINE_COMMENT_PATTERN,
        WHITE_SPACE_PATTERN
    };

    private static final Rule[] TOKEN_RULES = new Rule[] {
        new Rule(KEYWORD_PATTERN, TokenType.KEYWORD),
        new Rule(SYMBOL_PATTERN, TokenType.SYMBOL),
        new Rule(INTEGER_CONSTANT_PATTERN, TokenType.INTEGER_CONSTANT),
        new Rule(STRING_CONSTANT_PATTERN, TokenType.STRING_CONSTANT),
        new Rule(IDENTIFIER_PATTERN, TokenType.IDENTIFIER)
    };

    private final String code;
    private int index;

    Tokenizer(String code) {
        this.code = code;
    }

    Token getCurrentToken() {
        index = skipIgnored(index);

        for (Rule rule : TOKEN_RULES) {
            String current = code.substring(index);
            Matcher matcher = rule.pattern.matcher(current);
            if (!matcher.find()) {
                continue;
            }

            String token = matcher.group();
            validateToken(rule.type, token);
            return new Token(rule.type, token);
        }

        String remaining = code.substring(index);
        String preview = remaining.length() > 20 ? remaining.substring(0, 20) : remaining;
        throw new IllegalStateException("Invalid token near '" + preview + "' at index " + index);
    }

    boolean hasMoreTokens() {
        return skipIgnored(index) < code.length();
    }

    Token advance() {
        Token token = getCurrentToken();
        index += token.getValue().length();
        return token;
    }

    Token eat(String token) {
        if (hasMoreTokens() && getCurrentToken().getValue().equals(token)) {
            return advance();
        }

        throw new IllegalStateException("Invalid token near '" + token + "' at index " + index);
    }

    TokenType getTokenType() {
        return getCurrentToken().getType();
    }

    String getStringValue() {
        Token token = getCurrentToken();
        if (token.getType() != TokenType.STRING_CONSTANT) {
            throw new IllegalStateException("You can't perform this action on non string value");
        }

        return token.getValue().substring(1, token.getValue().length() - 1);
    }

    int getIntValue() {
        Token token = getCurrentToken();
        if (token.getType() != TokenType.INTEGER_CONSTANT) {
            throw new IllegalStateException("You can't perform this action on non int value");
        }

        return Integer.parseInt(token.getValue());
    }

    String getKeyWord() {
        Token token = getCurrentToken();
        if (token.getType() != TokenType.KEYWORD) {
            throw new IllegalStateException("You can't perform this action on non keyword value");
        }

        return token.getValue();
    }

    char getSymbol() {
        Token token = getCurrentToken();
        if (token.getType() != TokenType.SYMBOL) {
            throw new IllegalStateException("You can't perform this action on symbol value");
        }

        return token.getValue().charAt(0);
    }

    String getIdentifier() {
        Token token = getCurrentToken();
        if (token.getType() != TokenType.IDENTIFIER) {
            throw new IllegalStateException("You can't perform this action on non identifier");
        }

        return token.getValue();
    }

    private int skipIgnored(int currentIndex) {
        while (currentIndex < code.length()) {
            String current = code.substring(currentIndex);
            boolean matchedIgnoredToken = false;

            for (Pattern ignoredRule : IGNORED_RULES) {
                Matcher matcher = ignoredRule.matcher(current);
                if (!matcher.find()) {
                    continue;
                }

                currentIndex += matcher.group().length();
                matchedIgnoredToken = true;
                break;
            }

            if (!matchedIgnoredToken) {
                break;
            }
        }

        return currentIndex;
    }

    private void validateToken(TokenType type, String token) {
        if (type != TokenType.INTEGER_CONSTANT) {
            return;
        }

        try {
            int number = Integer.parseInt(token);
            if (number > 32767) {
                throw new IllegalStateException(
                    "Integer constant out of range near '" + token + "' at index " + index);
            }
        } catch (NumberFormatException exception) {
            throw new IllegalStateException(
                "Integer constant out of range near '" + token + "' at index " + index);
        }
    }

    private static final class Rule {
        private final Pattern pattern;
        private final TokenType type;

        private Rule(Pattern pattern, TokenType type) {
            this.pattern = pattern;
            this.type = type;
        }
    }
}
