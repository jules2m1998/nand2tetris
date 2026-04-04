import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public final class JackAnalyzer {
    private JackAnalyzer() {
    }

    public static void main(String[] args) {
        System.exit(run(args));
    }

    static int run(String[] arguments) {
        if (arguments.length != 1 || "--help".equals(arguments[0]) || "-h".equals(arguments[0])) {
            printUsage();
            return arguments.length == 1 ? 0 : 1;
        }

        Path inputPath = Paths.get(arguments[0]).toAbsolutePath().normalize();
        List<Path> writtenFiles = new ArrayList<Path>();

        try {
            List<Path> jackFiles = resolveInputFiles(inputPath);

            for (Path jackFile : jackFiles) {
                Path destinationPath = changeExtension(jackFile, ".xml");

                System.out.println("Analyzing " + jackFile.getFileName());

                String source = new String(Files.readAllBytes(jackFile), StandardCharsets.UTF_8);
                CompilerEngine engine = new CompilerEngine(new Tokenizer(source));
                String xml = engine.compileClass();

                Files.write(destinationPath, xml.getBytes(StandardCharsets.UTF_8));
                writtenFiles.add(destinationPath);

                System.out.println("Created " + destinationPath.getFileName());
            }

            System.out.println("Analysis completed: " + jackFiles.size() + " file(s).");
            return 0;
        } catch (Exception exception) {
            for (Path writtenFile : writtenFiles) {
                try {
                    Files.deleteIfExists(writtenFile);
                } catch (IOException ignored) {
                }
            }

            System.err.println("Analysis failed: " + exception.getMessage());
            return 1;
        }
    }

    private static List<Path> resolveInputFiles(Path inputPath) throws IOException {
        if (Files.isRegularFile(inputPath)) {
            if (!inputPath.getFileName().toString().toLowerCase().endsWith(".jack")) {
                throw new IllegalStateException("Input file must have a .jack extension.");
            }

            List<Path> jackFiles = new ArrayList<Path>();
            jackFiles.add(inputPath);
            return jackFiles;
        }

        if (!Files.isDirectory(inputPath)) {
            throw new IllegalStateException("Input path not found: " + inputPath);
        }

        try (Stream<Path> stream = Files.list(inputPath)) {
            List<Path> jackFiles = stream
                .filter(path -> Files.isRegularFile(path) && path.getFileName().toString().toLowerCase().endsWith(".jack"))
                .sorted(Comparator.comparing(path -> path.getFileName().toString()))
                .collect(Collectors.toList());

            if (jackFiles.isEmpty()) {
                throw new IllegalStateException("Input directory does not contain any .jack files.");
            }

            return jackFiles;
        }
    }

    private static Path changeExtension(Path filePath, String newExtension) {
        String fileName = filePath.getFileName().toString();
        int lastDot = fileName.lastIndexOf('.');
        String outputName = lastDot >= 0
            ? fileName.substring(0, lastDot) + newExtension
            : fileName + newExtension;

        return filePath.resolveSibling(outputName);
    }

    private static void printUsage() {
        System.out.println("JackAnalyzer");
        System.out.println("Usage:");
        System.out.println("  JackAnalyzer <path-to-jack-file-or-directory>");
        System.out.println("Analyze Jack source and generate sibling XML parse files.");
    }
}
