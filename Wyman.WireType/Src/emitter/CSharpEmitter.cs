using System;
using System.Collections.Generic;
using System.IO;

namespace Wyman.WireType.Emitter
{
    class CSharpEmitter : Emitter
    {
        public CSharpEmitter()
        { }

        public IReadOnlyDictionary<string, TypeList> GetHeaders() => null;

        public bool WriteHeader(grammar.NamespaceType namespaceType, TypeList types, SymbolTable symbolTable, TextWriter writer)
        {
            return false;
        }

        public bool WriteType(grammar.EnumType enumType, SymbolTable symbolTable, TextWriter writer)
        {
            var full_name = enumType.FullName();
            int index = full_name.LastIndexOf('.');
            grammar.NamespaceType namespace_type = null;
            string indent = string.Empty;

            if (enumType.Parent(out var parent))
            {
                namespace_type = parent as grammar.NamespaceType;

                writer.WriteLine($"namespace {namespace_type.FullName()}");
                writer.WriteLine("{");

                indent = new string(' ', 4);
            }

            writer.WriteLine($"{indent}enum {enumType.Name()}");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 8);

            foreach (var member in enumType.members())
            {
                writer.WriteLine($"{indent}{member.name} = {member.ordinal},");
            }

            indent = new string(' ', 4);

            writer.WriteLine($"{indent}}}");

            indent = string.Empty;

            if (namespace_type != null)
            {
                writer.WriteLine("}");
            }

            return true;
        }

        public bool WriteType(grammar.StructType structType, SymbolTable symbolTable, TextWriter writer)
        {
            var full_name = structType.FullName();
            grammar.NamespaceType namespace_type = null;
            string indent = string.Empty;

            var members = System.Linq.Enumerable.OrderBy(structType.members(), x => x.definition.Name());

            if (structType.Parent(out var parent))
            {
                namespace_type = parent as grammar.NamespaceType;

                writer.WriteLine($"namespace {namespace_type.FullName()}");
                writer.WriteLine("{");

                indent = new string(' ', 4);
            }

            writer.WriteLine($"{indent}partial class {structType.Name()} : WireType.IWireType");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 8);


            writer.WriteLine($"{indent}public {structType.Name()}()");
            writer.WriteLine($"{indent}{{ }}");
            writer.WriteLine();

            foreach (var member in members)
            {
                var storage_type = GetTypeString(member.definition, symbolTable, false);
                writer.WriteLine($"{indent}private WireType.StructMember<{storage_type}> _{member.definition.Name()} = new WireType.StructMember<{storage_type}>({member.ordinal});");
            }

            writer.WriteLine();

            foreach (var member in members)
            {
                var export_type = GetTypeString(member.definition, symbolTable, true);
                var actual_type = GetTypeString(member.definition, symbolTable, false);
                var member_kind = member.definition.Kind();

                // public void definition(kind value) { _exists = true; _value = value; }
                writer.WriteLine($"{indent}public void {member.definition.Name()}({export_type} value)");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 12);

                switch (member_kind)
                {
                    case grammar.MemberKind.blobList:
                    {
                        writer.WriteLine($"{indent}if (value is null)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = null;");
                        writer.WriteLine($"{indent}return;");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = (value.Count == 0)");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}? System.Array.Empty<byte[]>()");
                        writer.WriteLine($"{indent}: System.Linq.Enumerable.ToArray(value);");

                        indent = new string(' ', 12);
                    }
                    break;

                    case grammar.MemberKind.blobType:
                    {
                        writer.WriteLine($"{indent}if (value is null)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = null;");
                        writer.WriteLine($"{indent}return;");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = (value.Length == 0)");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}? System.Array.Empty<byte>()");
                        writer.WriteLine($"{indent}: value;");

                        indent = new string(' ', 12);
                    }
                    break;

                    case grammar.MemberKind.enumList:
                    case grammar.MemberKind.f32List:
                    case grammar.MemberKind.f64List:
                    case grammar.MemberKind.i32List:
                    case grammar.MemberKind.i64List:
                    case grammar.MemberKind.structList:
                    case grammar.MemberKind.u32List:
                    case grammar.MemberKind.u64List:
                    {
                        writer.WriteLine($"{indent}if (value is null || value.Count == 0)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = System.Array.Empty<{actual_type.Substring(0, actual_type.Length - 2)}>();");
                        writer.WriteLine($"{indent}return;");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}// Public export is a read-only list, whereas an array was recorded.");
                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = System.Linq.Enumerable.ToArray(value);");
                    }
                    break;

                    case grammar.MemberKind.stringType:
                    {
                        writer.WriteLine($"{indent}if (value is null)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = null;");
                        writer.WriteLine($"{indent}return;");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}// NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.");
                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = System.Text.Encoding.UTF8.GetBytes(value);");
                    }
                    break;

                    case grammar.MemberKind.stringList:
                    {
                        writer.WriteLine($"{indent}if (value is null)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = null;");
                        writer.WriteLine($"{indent}return;");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}if (value.Count == 0)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = System.Array.Empty<byte[]>();");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine($"{indent}else");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}var values = new byte[value.Count][];");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}// NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.");
                        writer.WriteLine($"{indent}for (int i = 0; i < value.Count; i += 1)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 20);

                        writer.WriteLine($"{indent}values[i] = (value[i] is null || value[i].Length == 0)");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}? System.Array.Empty<byte>()");
                        writer.WriteLine($"{indent}: System.Text.Encoding.UTF8.GetBytes(value[i]);");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = values;");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;

                    case grammar.MemberKind.timespanType:
                    {
                        writer.WriteLine($"{indent}// NetFx System.TimeSpan has 100-nanosecond granularity, whereas nanosecond values were recorded.");
                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = value.Ticks * 100L;");
                    }
                    break;

                    case grammar.MemberKind.timestampType:
                    {
                        writer.WriteLine($"{indent}// NetFx System.DateTime has 100-nanosecond granularity, whereas millisecond values were recorded.");
                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = value.Ticks / 10000L;");
                    }
                    break;

                    default:
                    {
                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = value;");
                    }
                    break;
                }

                indent = new string(' ', 8);

                writer.WriteLine($"{indent}}}");
                writer.WriteLine();

                // public bool definition(out kind value) { if (!_exits) { value = default(kind); return false; } value = _value; return true; }
                writer.WriteLine($"{indent}public bool {member.definition.Name()}(out {export_type} value)");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 12);

                writer.WriteLine($"{indent}if (!_{member.definition.Name()}.Exists)");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 16);

                writer.WriteLine($"{indent}value = default({export_type});");
                writer.WriteLine($"{indent}return false;");

                indent = new string(' ', 12);

                writer.WriteLine($"{indent}}}");
                writer.WriteLine();

                switch (member.definition.Kind())
                {
                    case grammar.MemberKind.stringType:
                    {
                        writer.WriteLine($"{indent}// NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.");
                        writer.WriteLine($"{indent}value = System.Text.Encoding.UTF8.GetString(_{member.definition.Name()}.Value);");
                    }
                    break;

                    case grammar.MemberKind.stringList:
                    {
                        writer.WriteLine($"{indent}var values = new System.Collections.Generic.List<string>(_{member.definition.Name()}.Value.Length);");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}// NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.");
                        writer.WriteLine($"{indent}foreach (byte[] bytes in _{member.definition.Name()}.Value)");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}string strval = bytes.Length == 0");

                        indent = new string(' ', 20);

                        writer.WriteLine($"{indent}? string.Empty");
                        writer.WriteLine($"{indent}: System.Text.Encoding.UTF8.GetString(bytes);");
                        writer.WriteLine();

                        indent = new string(' ', 16);

                        writer.WriteLine($"{indent}values.Add(strval);");

                        indent = new string(' ', 12);

                        writer.WriteLine($"{indent}}}");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}value = values;");
                    }
                    break;

                    case grammar.MemberKind.timespanType:
                    {
                        writer.WriteLine($"{indent}// NetFx System.TimeSpan has 100-nanosecond granularity, whereas nanosecond values were recorded.");
                        writer.WriteLine($"{indent}value = new System.TimeSpan(_{member.definition.Name()}.Value / 100L);");
                    }
                    break;

                    case grammar.MemberKind.timestampType:
                    {
                        writer.WriteLine($"{indent}// NetFx System.DateTime has 100-nanosecond granularity, whereas millisecond values were recorded.");
                        writer.WriteLine($"{indent}value = new System.DateTime(10000L * _{member.definition.Name()}.Value);");
                    }
                    break;

                    default:
                    {
                        writer.WriteLine($"{indent}value = _{member.definition.Name()}.Value;");
                    }
                    break;
                }

                writer.WriteLine($"{indent}return true;");

                indent = new string(' ', 8);

                writer.WriteLine($"{indent}}}");
                writer.WriteLine();
            }

            writer.WriteLine($"{indent}public unsafe bool ReadFrom(WireType.WireTypeReader reader)");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 12);

            writer.WriteLine($"{indent}if (reader is null)");
            writer.WriteLine($"{new string(' ', 16)}throw new System.ArgumentNullException(nameof(reader));");
            writer.WriteLine();
            writer.WriteLine($"{indent}long size = reader.Current.Size;");
            writer.WriteLine($"{indent}long alreadyRead = reader.TotalRead;");
            writer.WriteLine();
            writer.WriteLine($"{indent}while (reader.TotalRead - alreadyRead < size)");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 16);

            writer.WriteLine($"{indent}if (!reader.Read(out WireType.TypeHeader header))");
            writer.WriteLine($"{new string(' ', 20)}return false;");
            writer.WriteLine();
            writer.WriteLine($"{indent}switch (header.Ordinal)");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 20);

            members = System.Linq.Enumerable.OrderBy(members, x => x.ordinal);

            foreach (var member in members)
            {
                var member_kind = member.definition.Kind();
                var type_kind = GetTypeKind(member_kind);
                var storage_type = GetStorageTypeName(member.definition, symbolTable);

                writer.WriteLine($"{indent}case {member.ordinal}: // {member.definition.Name()}");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 24);

                writer.WriteLine($"{indent}if (header.Kind != WireType.TypeKind.{type_kind})");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 28);

                writer.WriteLine($"{indent}reader.Skip();");

                indent = new string(' ', 24);

                writer.WriteLine($"{indent}}}");
                writer.WriteLine();

                switch (member_kind)
                {
                    case grammar.MemberKind.blobList:
                    case grammar.MemberKind.stringList:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out byte[][] values))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = values;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;

                    case grammar.MemberKind.blobType:
                    case grammar.MemberKind.stringType:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out byte[] value))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = value;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;

                    case grammar.MemberKind.enumList:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out int[] temp))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}var values = new {storage_type}[temp.Length];");
                        writer.WriteLine();
                        
                        writer.WriteLine($"{indent}System.Buffer.BlockCopy(temp, 0, values, 0, temp.Length * sizeof(int));");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = values;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;

                    case grammar.MemberKind.enumType:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out int value))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = ({storage_type})value;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;

                    case grammar.MemberKind.f32List:
                    case grammar.MemberKind.f64List:
                    case grammar.MemberKind.i32List:
                    case grammar.MemberKind.i64List:
                    case grammar.MemberKind.timespanList:
                    case grammar.MemberKind.timestampList:
                    case grammar.MemberKind.u32List:
                    case grammar.MemberKind.u64List:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out {storage_type}[] values))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = values;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;

                    case grammar.MemberKind.f64Type:
                    case grammar.MemberKind.f32Type:
                    case grammar.MemberKind.i32Type:
                    case grammar.MemberKind.i64Type:
                    case grammar.MemberKind.timespanType:
                    case grammar.MemberKind.timestampType:
                    case grammar.MemberKind.u32Type:
                    case grammar.MemberKind.u64Type:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out {storage_type} value))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = value;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;


                    case grammar.MemberKind.structList:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out {storage_type}[] values))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = values;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;

                    case grammar.MemberKind.structType:
                    {
                        writer.WriteLine($"{indent}if (reader.Read(out {storage_type} value))");
                        writer.WriteLine($"{indent}{{");

                        indent = new string(' ', 28);

                        writer.WriteLine($"{indent}_{member.definition.Name()}.Value = value;");

                        indent = new string(' ', 24);

                        writer.WriteLine($"{indent}}}");
                    }
                    break;
                }

                indent = new string(' ', 20);

                writer.WriteLine($"{indent}}}");
                writer.WriteLine($"{indent}break;");
                writer.WriteLine();
            }

            indent = new string(' ', 16);

            writer.WriteLine($"{indent}}}");

            indent = new string(' ', 12);

            writer.WriteLine($"{indent}}}");
            writer.WriteLine();
            writer.WriteLine($"{indent}return true;");

            indent = new string(' ', 8);

            writer.WriteLine($"{indent}}}");
            writer.WriteLine();

            writer.WriteLine($"{indent}public unsafe void WriteTo(WireType.WireTypeWriter writer)");
            writer.WriteLine($"{indent}{{");

            indent = new string(' ', 12);

            foreach (var member in members)
            {
                var member_kind = member.definition.Kind();
                var type_kind = GetTypeKind(member_kind);
                var storage_type = GetStorageTypeName(member.definition, symbolTable);

                writer.WriteLine($"{indent}if (_{member.definition.Name()}.Exists)");
                writer.WriteLine($"{indent}{{");

                indent = new string(' ', 16);

                switch (member_kind)
                {
                    case grammar.MemberKind.blobList:
                    case grammar.MemberKind.blobType:
                    case grammar.MemberKind.f32List:
                    case grammar.MemberKind.f32Type:
                    case grammar.MemberKind.f64List:
                    case grammar.MemberKind.f64Type:
                    case grammar.MemberKind.i32List:
                    case grammar.MemberKind.i32Type:
                    case grammar.MemberKind.i64List:
                    case grammar.MemberKind.i64Type:
                    case grammar.MemberKind.stringList:
                    case grammar.MemberKind.stringType:
                    case grammar.MemberKind.structList:
                    case grammar.MemberKind.structType:
                    case grammar.MemberKind.timespanList:
                    case grammar.MemberKind.timespanType:
                    case grammar.MemberKind.timestampList:
                    case grammar.MemberKind.timestampType:
                    case grammar.MemberKind.u32List:
                    case grammar.MemberKind.u32Type:
                    case grammar.MemberKind.u64List:
                    case grammar.MemberKind.u64Type:
                    {
                        writer.WriteLine($"{indent}writer.Write(_{member.definition.Name()}.Ordinal, _{member.definition.Name()}.Value);");
                    }
                    break;

                    case grammar.MemberKind.enumList:
                    {
                        writer.WriteLine($"{indent}var values = new int[_{member.definition.Name()}.Value.Length];");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}System.Buffer.BlockCopy(_{member.definition.Name()}.Value, 0, values, 0, values.Length * sizeof(int));");
                        writer.WriteLine();

                        writer.WriteLine($"{indent}writer.Write(_{member.definition.Name()}.Ordinal, values);");
                    }
                    break;

                    case grammar.MemberKind.enumType:
                    {
                        writer.WriteLine($"{indent}int value = (int)_{member.definition.Name()}.Value;");
                        writer.WriteLine($"{indent}writer.Write(_{member.definition.Name()}.Ordinal, value);");
                    }
                    break;
                }

                indent = new string(' ', 12);

                writer.WriteLine($"{indent}}}");
                writer.WriteLine();
            }

            indent = new string(' ', 8);

            writer.WriteLine($"{indent}}}");

            indent = new string(' ', 4);

            writer.WriteLine($"{indent}}}");

            indent = string.Empty;

            if (namespace_type != null)
            {
                writer.WriteLine("}");
            }

            return true;
        }

        public string GetTypeString(grammar.MemberType memberType, SymbolTable symbolTable, bool isExported)
        {
            const string export_list_format = "System.Collections.Generic.IReadOnlyList<{0}>";
            const string storage_list_format = "{0}[]";

            var type_name = (isExported)
                ? GetExportType(memberType, symbolTable)
                : GetStorageTypeName(memberType, symbolTable);

            var is_list = (memberType.Kind() & grammar.MemberKind.List) != 0;
            var format = (isExported)
                ? export_list_format
                : storage_list_format;

            return GetTypeString(format, type_name, is_list);
        }

        private string GetExportType(grammar.MemberType memberType, SymbolTable symbolTable)
        {
            var type = memberType.Kind() & ~grammar.MemberKind.List;

            switch (type)
            {
                case grammar.MemberKind.blobType:
                    return "byte[]";

                case grammar.MemberKind.enumType:
                    goto case grammar.MemberKind.structType;

                case grammar.MemberKind.f32Type:
                    return "float";

                case grammar.MemberKind.f64Type:
                    return "double";

                case grammar.MemberKind.i32Type:
                    return "int";

                case grammar.MemberKind.i64Type:
                    return "long";

                case grammar.MemberKind.stringType:
                    return "string";

                case grammar.MemberKind.structType:
                {
                    var full_name = memberType.TypeName();
                    var real_type = symbolTable.Get(full_name);

                    return real_type.FullName();
                }

                case grammar.MemberKind.timespanType:
                    return "System.TimeSpan";

                case grammar.MemberKind.timestampType:
                    return "System.DateTime";

                case grammar.MemberKind.u32Type:
                    return "uint";

                case grammar.MemberKind.u64Type:
                    return "ulong";
            }

            throw new Exception("NEED TYPED EXCEPTION HERE");
        }

        private string GetStorageTypeName(grammar.MemberType memberType, SymbolTable symbolTable)
        {
            var type = memberType.Kind() & ~grammar.MemberKind.List;

            switch (type)
            {
                case grammar.MemberKind.blobType:
                    return "byte[]";

                case grammar.MemberKind.enumType:
                    goto case grammar.MemberKind.structType;

                case grammar.MemberKind.f32Type:
                    return "float";

                case grammar.MemberKind.f64Type:
                    return "double";

                case grammar.MemberKind.i32Type:
                    return "int";

                case grammar.MemberKind.i64Type:
                    return "long";

                case grammar.MemberKind.stringType:
                    return "byte[]";

                case grammar.MemberKind.structType:
                {
                    var full_name = memberType.TypeName();
                    var real_type = symbolTable.Get(full_name);

                    return real_type.FullName();
                }

                case grammar.MemberKind.timespanType:
                case grammar.MemberKind.timestampType:
                    goto case grammar.MemberKind.i64Type;

                case grammar.MemberKind.u32Type:
                    return "uint";

                case grammar.MemberKind.u64Type:
                    return "ulong";
            }

            throw new Exception("NEED TYPED EXCEPTION HERE");
        }

        private static string GetTypeKindName(grammar.MemberKind memberKind)
        {
            var type_kind = GetTypeKind(memberKind);

            switch (type_kind)
            {
                case global::WireType.TypeKind.ExplicitSize:
                    return nameof(global::WireType.TypeKind.ExplicitSize);

                case global::WireType.TypeKind.ExplicitSizeList:
                    return nameof(global::WireType.TypeKind.ExplicitSizeList);

                case global::WireType.TypeKind.ImplicitSize:
                    return nameof(global::WireType.TypeKind.ImplicitSize);

                case global::WireType.TypeKind.ImplicitSizeList:
                    return nameof(global::WireType.TypeKind.ImplicitSizeList);
            }

            return "UNKNOWN";
        }

        private static global::WireType.TypeKind GetTypeKind(grammar.MemberKind memberKind)
        {
            switch (memberKind)
            {
                case grammar.MemberKind.blobType:
                case grammar.MemberKind.stringType:
                case grammar.MemberKind.structType:
                    return global::WireType.TypeKind.ExplicitSize;

                case grammar.MemberKind.blobList:
                case grammar.MemberKind.stringList:
                case grammar.MemberKind.structList:
                    return global::WireType.TypeKind.ExplicitSizeList;

                case grammar.MemberKind.enumType:
                case grammar.MemberKind.f32Type:
                case grammar.MemberKind.f64Type:
                case grammar.MemberKind.i32Type:
                case grammar.MemberKind.i64Type:
                case grammar.MemberKind.timespanType:
                case grammar.MemberKind.timestampType:
                case grammar.MemberKind.u32Type:
                case grammar.MemberKind.u64Type:
                    return global::WireType.TypeKind.ImplicitSize;

                case grammar.MemberKind.enumList:
                case grammar.MemberKind.f32List:
                case grammar.MemberKind.f64List:
                case grammar.MemberKind.i32List:
                case grammar.MemberKind.i64List:
                case grammar.MemberKind.timespanList:
                case grammar.MemberKind.timestampList:
                case grammar.MemberKind.u32List:
                case grammar.MemberKind.u64List:
                    return global::WireType.TypeKind.ImplicitSizeList;
            }

            return (global::WireType.TypeKind)7;
        }

        private string GetTypeString(string format, string type_name, bool is_list)
        {
            return is_list ? string.Format(System.Globalization.CultureInfo.InvariantCulture, format, type_name) : type_name;
        }
    }
}
