using SyntaxAnalyser.Abstractions;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
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
public class SymbolTable : Collection<Symbol>, ISymbolTable
{
    public void StartSubroutine()
    {
        var clearedList = this.Where(x => x.Kind != SymbolKind.Argument && x.Kind != SymbolKind.Var).ToList();
        Clear();
        foreach (var symbol in clearedList)
        {
            Define(symbol.Name, symbol.Type, symbol.Kind);
        }
    }
    
    public void Define(string name, string type, SymbolKind kind)
    {
        var count = this.Count(s => s.Kind == kind);
        Add(new Symbol(name, type, kind, count));
    }
    
    public int VarCount(SymbolKind kind)
    {
        return this.Count(x => x.Kind == kind);
    }
    
    public SymbolKind? KindOf(string name)
    {
        return this.FirstOrDefault(x => x.Name == name)?.Kind;
    }
    
    public string? TypeOf(string name)
    {
        return this.FirstOrDefault(x => x.Name == name)?.Type;
    }
    
    public int? IndexOf(string name)
    {
        return this.FirstOrDefault(x => x.Name == name)?.Index;
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