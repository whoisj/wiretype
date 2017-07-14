using System.Collections.Generic;
using System.IO;

namespace Wyman.WireType.Emitter
{
    interface Emitter
    {
        IReadOnlyDictionary<string, TypeList> GetHeaders();

        bool WriteHeader(grammar.NamespaceType namespaceType, TypeList types, SymbolTable symbolTable, TextWriter writer);

        bool WriteType(grammar.EnumType enumType, SymbolTable symbolTable, TextWriter writer);

        bool WriteType(grammar.StructType structType, SymbolTable symbolTable, TextWriter writer);
    }
}
