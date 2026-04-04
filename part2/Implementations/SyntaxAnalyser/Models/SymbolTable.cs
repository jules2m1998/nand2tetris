using System.Collections;
using System.Runtime.CompilerServices;
using SyntaxAnalyser.Abstractions;

namespace SyntaxAnalyser.Models;

public enum SymbolKind
{
    Field,
    Static,
    Argument,
    Var
}

public record Symbol(string Name, string Type, SymbolKind Kind, int Index);

[CollectionBuilder(typeof(SymbolTableFactory), nameof(SymbolTableFactory.Create))]
public class SymbolTable : ISymbolTable
{
    private readonly List<Symbol> _classScope = [];
    private readonly List<Symbol> _subroutineScope = [];

    public int Count => _classScope.Count + _subroutineScope.Count;

    public bool IsReadOnly => false;

    public void StartSubroutine()
    {
        _subroutineScope.Clear();
    }

    public void Define(string name, string type, SymbolKind kind)
    {
        var scope = GetScopeForKind(kind);

        if (scope.Any(symbol => symbol.Name == name))
            throw new InvalidOperationException($"Duplicate symbol '{name}' in the current scope.");

        var index = scope.Count(symbol => symbol.Kind == kind);
        scope.Add(new Symbol(name, type, kind, index));
    }

    public int VarCount(SymbolKind kind)
    {
        return GetScopeForKind(kind).Count(symbol => symbol.Kind == kind);
    }

    public SymbolKind? KindOf(string name)
    {
        return Resolve(name)?.Kind;
    }

    public string? TypeOf(string name)
    {
        return Resolve(name)?.Type;
    }

    public int? IndexOf(string name)
    {
        return Resolve(name)?.Index;
    }

    public void Add(Symbol item)
    {
        GetScopeForKind(item.Kind).Add(item);
    }

    public void Clear()
    {
        _classScope.Clear();
        _subroutineScope.Clear();
    }

    public bool Contains(Symbol item)
    {
        return _classScope.Contains(item) || _subroutineScope.Contains(item);
    }

    public void CopyTo(Symbol[] array, int arrayIndex)
    {
        foreach (var symbol in this)
        {
            array[arrayIndex++] = symbol;
        }
    }

    public bool Remove(Symbol item)
    {
        return _subroutineScope.Remove(item) || _classScope.Remove(item);
    }

    public IEnumerator<Symbol> GetEnumerator()
    {
        foreach (var symbol in _classScope)
            yield return symbol;

        foreach (var symbol in _subroutineScope)
            yield return symbol;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private List<Symbol> GetScopeForKind(SymbolKind kind)
    {
        return kind switch
        {
            SymbolKind.Static or SymbolKind.Field => _classScope,
            SymbolKind.Argument or SymbolKind.Var => _subroutineScope,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private Symbol? Resolve(string name)
    {
        return _subroutineScope.FirstOrDefault(symbol => symbol.Name == name)
               ?? _classScope.FirstOrDefault(symbol => symbol.Name == name);
    }
}

public static class SymbolTableFactory
{
    public static SymbolTable Create(ReadOnlySpan<Symbol> values)
    {
        var list = new SymbolTable();
        foreach (var item in values)
            list.Add(item);
        return list;
    }
}
