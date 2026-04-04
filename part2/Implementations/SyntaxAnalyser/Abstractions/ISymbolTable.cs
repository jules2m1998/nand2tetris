using SyntaxAnalyser.Models;
namespace SyntaxAnalyser.Abstractions;

/// <summary>
/// Defines the symbol table contract used during Jack compilation.
/// The table tracks identifiers declared at class scope and subroutine scope,
/// along with their type, kind, and running index.
/// </summary>
public interface ISymbolTable : ICollection<Symbol>
{
    /// <summary>
    /// Starts a new subroutine scope.
    /// Implementations should clear subroutine-level symbols such as
    /// <see cref="SymbolKind.Argument"/> and <see cref="SymbolKind.Var"/>,
    /// while preserving class-level symbols such as
    /// <see cref="SymbolKind.Static"/> and <see cref="SymbolKind.Field"/>.
    /// </summary>
    void StartSubroutine();
    
    /// <summary>
    /// Defines a new identifier and assigns the next running index for its kind.
    /// </summary>
    /// <param name="name">The declared identifier name.</param>
    /// <param name="type">The Jack type associated with the identifier.</param>
    /// <param name="kind">The storage kind assigned to the identifier.</param>
    void Define(string name, string type, SymbolKind kind);

    /// <summary>
    /// Returns how many identifiers of the specified kind exist in the current table state.
    /// </summary>
    /// <param name="kind">The symbol kind to count.</param>
    /// <returns>The number of identifiers currently defined for the given kind.</returns>
    int VarCount(SymbolKind kind);

    /// <summary>
    /// Returns the kind of a previously defined identifier.
    /// </summary>
    /// <param name="name">The identifier name to resolve.</param>
    /// <returns>
    /// The identifier kind if it exists; otherwise <see langword="null"/>.
    /// </returns>
    SymbolKind? KindOf(string name);

    /// <summary>
    /// Returns the declared type of a previously defined identifier.
    /// </summary>
    /// <param name="name">The identifier name to resolve.</param>
    /// <returns>
    /// The declared type if the identifier exists; otherwise <see langword="null"/>.
    /// </returns>
    string? TypeOf(string name);

    /// <summary>
    /// Returns the running index assigned to a previously defined identifier.
    /// </summary>
    /// <param name="name">The identifier name to resolve.</param>
    /// <returns>
    /// The assigned index if the identifier exists; otherwise <see langword="null"/>.
    /// </returns>
    int? IndexOf(string name);
}
