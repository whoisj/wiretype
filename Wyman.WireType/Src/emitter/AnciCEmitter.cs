using System;
using System.Collections.Generic;
using System.IO;
using static System.StringComparer;

namespace Wyman.WireType.Emitter
{
    class AnciCEmitter : Emitter
    {
        public AnciCEmitter()
        {
            _headers = new Dictionary<string, TypeList>(Ordinal);
            _syncpoint = new object();
        }

        private readonly Dictionary<string, TypeList> _headers;
        private readonly object _syncpoint;

        public IReadOnlyDictionary<string, TypeList> GetHeaders()
        {
            return _headers;
        }

        public bool WriteHeader(grammar.NamespaceType namespaceType, TypeList types, SymbolTable symbolTable, TextWriter writer)
        {
            var namespaceName = namespaceType.FullName();
            var header_name = GetCTypeName(namespaceName);
            var namespaces = new HashSet<string>(Ordinal);
            grammar.BaseType parent;

            namespaces.Add(namespaceName);

            foreach (var type in types)
            {
                if (type.Parent(out parent)
                    && parent is grammar.NamespaceType ns)
                {
                    var fullName = ns.FullName();

                    if (!namespaces.Contains(fullName))
                    {
                        namespaces.Add(fullName);
                    }
                }
            }

            writer.WriteLine("#include \"wire_type.h\"");

            var namespaceTypes = symbolTable.GetAll<grammar.NamespaceType>();
            foreach (var type in namespaceTypes)
            {
                if (namespaces.Contains(type.FullName()))
                    continue;

                var nsName = type.FullName();

                if (namespaceName.StartsWith(nsName, StringComparison.Ordinal))
                {
                    var file_name = GetCTypeName(nsName);

                    writer.WriteLine($"#include \"{file_name.TrimEnd()}.h\"");
                }
            }

            writer.WriteLine();

            writer.WriteLine($"#ifndef __{header_name?.Trim()?.ToUpper()}__");
            writer.WriteLine($"#define __{header_name?.Trim()?.ToUpper()}__");

            foreach (var type in types)
            {
                if (type is grammar.StructType struct_type)
                {
                    var fullName = struct_type.FullName();
                    var typeName = GetCTypeName(fullName);
                    var indent = string.Empty;

                    writer.WriteLine();
                    writer.WriteLine($"typedef struct {typeName?.Trim()} {typeName?.Trim()};");
                    writer.WriteLine();
                    writer.WriteLine($"{indent}int {typeName?.Trim()}__initialize(struct {typeName}*object);");
                    writer.WriteLine($"{indent}int {typeName?.Trim()}__deserialize(struct {typeName}*object, FILE *stream);");
                    writer.WriteLine($"{indent}int {typeName?.Trim()}__serialize(struct {typeName}*object, FILE *stream);");
                }
            }

            foreach (var type in types)
            {
                if (type is grammar.EnumType enum_type)
                {
                    write_header(enum_type, symbolTable, writer);
                }
                else if (type is grammar.StructType struct_type)
                {
                    write_header(struct_type, symbolTable, writer);
                }
            }

            writer.WriteLine();
            writer.WriteLine("#endif");

            return true;
        }

        void write_header(grammar.EnumType value, SymbolTable symbolTable, TextWriter writer)
        {
            var fullName = value.FullName();
            var typeName = GetCTypeName(fullName)?.Trim();
            var indent = string.Empty;

            writer.WriteLine();

            writer.WriteLine($"{indent}typedef enum");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 4);

            foreach (var member in value.members())
            {
                var memberName = GetCTypeName(member.name)?.Trim();

                writer.WriteLine($"{indent}{typeName}_{memberName} = {member.ordinal},");
            }

            indent = string.Empty;

            writer.WriteLine($"{indent}}} {typeName};");
        }

        void write_header(grammar.StructType value, SymbolTable symbolTable, TextWriter writer)
        {
            var full_name = value.FullName();
            var typeName = GetCTypeName(full_name);
            var indent = string.Empty;

            foreach (var member in value.members())
            {
                var memberName = member.definition.Name();
                memberName = GetCTypeName(memberName)?.Trim();

                var memberType = GetStorageType(member.definition, symbolTable);
                memberType = GetCTypeName(memberType);

                writer.WriteLine();
                writer.WriteLine($"{indent}int {typeName?.Trim()}__{memberName}_get(struct {typeName}*object, {memberType}*value_out);");
                writer.WriteLine($"{indent}int {typeName?.Trim()}__{memberName}_set(struct {typeName}*object, {memberType}*value_new);");
            }
        }

        public bool WriteType(grammar.EnumType value, SymbolTable symbolTable, TextWriter writer)
        {
            var fullName = value.FullName();
            grammar.NamespaceType namespace_type = null;

            if (value.Parent(out var parent))
            {
                namespace_type = parent as grammar.NamespaceType;

                var namespace_header = namespace_type.FullName();

                lock (_syncpoint)
                {
                    if (!_headers.ContainsKey(namespace_header))
                    {
                        _headers.Add(namespace_header, new TypeList());
                    }

                    _headers[namespace_header].Add(value);
                }
            }

            return false;
        }

        public bool WriteType(grammar.StructType value, SymbolTable symbolTable, TextWriter writer)
        {
            var fullName = value.FullName();
            var typeName = GetCTypeName(fullName);
            int index = fullName.LastIndexOf('.');
            grammar.NamespaceType namespaceType = null;
            var indent = string.Empty;

            if (value.Parent(out var parent))
            {
                namespaceType = parent as grammar.NamespaceType;

                var namespaceHeader = namespaceType.FullName();
                var headerFileName = GetCTypeName(namespaceHeader);

                writer.WriteLine($"#include \"{headerFileName.TrimEnd()}.h\"");
                writer.WriteLine();

                lock (_syncpoint)
                {
                    if (!_headers.ContainsKey(namespaceHeader))
                    {
                        _headers.Add(namespaceHeader, new TypeList());
                    }

                    _headers[namespaceHeader].Add(value);
                }
            }

            writer.WriteLine($"{indent}struct {typeName?.Trim()}");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 4);

            var members = value.members();

            foreach (var member in members)
            {
                var memberName = member.definition.Name();
                memberName = GetCTypeName(memberName);

                var memberType = GetStorageType(member.definition, symbolTable);
                memberType = GetCTypeName(memberType);

                writer.WriteLine($"{indent}int _{memberName?.Trim()}_exists;");
                writer.WriteLine($"{indent}unsigned long _{memberName?.Trim()}_oridinal;");

                switch(member.definition.Kind())
                {
                    default:
                        {
                            writer.WriteLine($"{indent}{memberType}_{memberName?.Trim()}_value;");
                        }
                        break;
                }

                writer.WriteLine();
            }

            indent = string.Empty;

            writer.WriteLine($"{indent}}};");
            writer.WriteLine();

            writer.WriteLine($"{indent}int {typeName?.Trim()}__initialize(struct {typeName}*object)");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 4);

            writer.WriteLine($"{indent}// Zero the struct (default state) but retain all ordinal data.");
            writer.WriteLine($"{indent}memset(object, 0, sizeof({typeName?.Trim()}));");
            writer.WriteLine();

            foreach (var member in members)
            {
                var memberName = member.definition.Name();
                memberName = GetCTypeName(memberName);

                writer.WriteLine($"{indent}object->_{memberName?.Trim()}_oridinal = {member.ordinal};");
            }

            writer.WriteLine($"{indent}return 0;");

            indent = string.Empty;

            writer.WriteLine($"{indent}}};");
            writer.WriteLine();

            writer.WriteLine($"{indent}int {typeName?.Trim()}__deserialize(struct {typeName}*object, FILE *stream)");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 4);

            writer.WriteLine($"{indent}// code goes here");
            writer.WriteLine($"{indent}return 0;");

            indent = string.Empty;

            writer.WriteLine($"{indent}}};");
            writer.WriteLine();

            writer.WriteLine($"{indent}int {typeName?.Trim()}__serialize(struct {typeName}*object, FILE *stream)");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 4);

            writer.WriteLine($"{indent}// code goes here");
            writer.WriteLine($"{indent}return 0;");

            indent = string.Empty;

            writer.WriteLine($"{indent}}};");
            writer.WriteLine();

            foreach (var member in members)
            {
                var memberName = member.definition.Name();
                memberName = GetCTypeName(memberName)?.Trim();

                var memberType = GetStorageType(member.definition, symbolTable);
                memberType = GetCTypeName(memberType);

                writer.WriteLine($"{indent}int {typeName?.Trim()}__{memberName}_get(struct {typeName}*object, {memberType}*value_out)");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 4);

                writer.WriteLine($"{indent}if (!object->_{memberName}_exists)");
                writer.WriteLine($"{new string(' ', 12)}return -1;");
                writer.WriteLine();
                writer.WriteLine($"{indent}*value_out = object->_{memberName}_value;");
                writer.WriteLine($"{indent}return 0;");

                indent = string.Empty;

                writer.WriteLine($"{indent}}}");
                writer.WriteLine();

                writer.WriteLine($"{indent}int {typeName?.Trim()}__{memberName}_set(struct {typeName}*object, {memberType}*value_new)");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 4);

                writer.WriteLine($"{indent}object->_{memberName}_value = *value_new;");
                writer.WriteLine($"{indent}object->_{memberName}_exists = 1;");
                writer.WriteLine($"{indent}return 0;");

                indent = string.Empty;

                writer.WriteLine($"{indent}}}");
                writer.WriteLine();
            }

            return true;
        }

        public string GetCTypeName(string fullName, bool toLower = true)
        {
            var name = fullName?.Replace('.', '_');

            if (toLower)
            {
                name = name?.ToLower();
            }

            if (!fullName.EndsWith("*", StringComparison.Ordinal))
            {
                name += " ";
            }

            return name;
        }

        public string get_type_string(grammar.MemberType memberType, SymbolTable symbolTable, bool isExpert)
        {
            const string StorageListFormat = "{0}*";

            var typeName = GetStorageType(memberType, symbolTable);

            var isList = (memberType.Kind() & grammar.MemberKind.List) != 0;
            var format = StorageListFormat;

            return GetTypeString(format, typeName, isList);
        }

        private string GetStorageType(grammar.MemberType memberType, SymbolTable symbolTable)
        {
            var type = memberType.Kind() & ~grammar.MemberKind.List;

            switch (type)
            {
                case grammar.MemberKind.blobType:
                    return "char *";

                case grammar.MemberKind.enumType:
                    goto case grammar.MemberKind.structType;

                case grammar.MemberKind.f32Type:
                    return "float ";

                case grammar.MemberKind.f64Type:
                    return "double ";

                case grammar.MemberKind.i32Type:
                    return "signed long ";

                case grammar.MemberKind.i64Type:
                    return "signed long long ";

                case grammar.MemberKind.stringType:
                    return "char *";

                case grammar.MemberKind.structType:
                    {
                        var fullName = memberType.TypeName();
                        var real_type = symbolTable.Get(fullName);

                        var type_name = real_type.FullName();
                        type_name += " *";

                        return type_name;
                    }

                case grammar.MemberKind.timespanType:
                case grammar.MemberKind.timestampType:
                    goto case grammar.MemberKind.u64Type;

                case grammar.MemberKind.u32Type:
                    return "unsigned long ";

                case grammar.MemberKind.u64Type:
                    return "unsigned long long ";
            }

            throw new Exception("NEED TYPED EXCEPTION HERE");
        }

        private string GetTypeString(string format, string typeName, bool isList)
        {
            return isList ? string.Format(format, typeName) : typeName;
        }
    }
}
