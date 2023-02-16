namespace Ptt;

public static class FieldSymbols
{
    public static readonly Symbol SymFieldProduct
        = new Symbol("⋅");

    public static readonly Symbol SymFieldSum
        = new Symbol("+");

    public static readonly Symbol SymFieldMinus
        = new Symbol("-");

    public static readonly Symbol SymFieldDivision
        = new Symbol("/");
}

public static class RelationSymbols
{
    public static readonly Symbol SymLt = new Symbol("<", ">", "≥", "≤");

    public static readonly Symbol SymEq = new Symbol("=", "=", "≠", "≠");
}
