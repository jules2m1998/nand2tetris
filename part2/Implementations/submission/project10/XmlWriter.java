final class XmlWriter {
    private static final String INDENT_UNIT = "  ";

    private final StringBuilder builder = new StringBuilder();
    private int indentLevel;

    void indent() {
        indentLevel++;
    }

    void unindent() {
        if (indentLevel > 0) {
            indentLevel--;
        }
    }

    void appendLine(String text) {
        for (int i = 0; i < indentLevel; i++) {
            builder.append(INDENT_UNIT);
        }

        builder.append(text);
        builder.append('\n');
    }

    void append(String text) {
        builder.append(text);
    }

    void appendLines(String text) {
        if (text.isEmpty()) {
            return;
        }

        int start = 0;

        while (start < text.length()) {
            int end = text.indexOf('\n', start);
            if (end == -1) {
                appendLine(text.substring(start));
                return;
            }

            if (end > start) {
                appendLine(text.substring(start, end));
            }

            start = end + 1;
        }
    }

    @Override
    public String toString() {
        return builder.toString();
    }
}
