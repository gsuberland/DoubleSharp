using System.Collections;
using System.Reflection;

namespace DoubleSharp.Pretty;

public static class PrettyExtensions {
	static readonly IReadOnlyDictionary<Type, MethodInfo> Printers;
	static PrettyExtensions() {
		var printers = new Dictionary<Type, MethodInfo>();
		foreach(var asm in AppDomain.CurrentDomain.GetAssemblies()) {
			foreach(var type in asm.GetTypes()) {
				if(type.GetInterfaces().Contains(typeof(IPrettyPrintable)))
					printers[type] = type.GetMethod("ToPrettyString")!;
				else
					foreach(var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)) {
						if(method.GetCustomAttribute<PrettyPrinterAttribute>() != null)
							printers[method.GetParameters()[0].ParameterType] = method;
					}
			}
		}
		Printers = printers;
	}
		
	public static void Print<T>(this T obj) => Console.WriteLine(obj.ToPrettyString());
	public static void PrettyPrint<T>(this T obj) => Console.WriteLine(obj.ToPrettyString());
	public static string ToPrettyString<T>(this T obj) => obj == null
		? $"({typeof(T).ToPrettyString()}) null"
		: ToPrettyString((object) obj);

	static string Indentify(string value) => 
		value.Contains('\n')
			? string.Join('\n', value.Trim().Split('\n').Select(x => "\t" + x))
			: "\t" + value;

	static IEnumerable<object?> Enumeratorable(IEnumerator e) {
		while(e.MoveNext())
			yield return e.Current;
	}

	static string ToPrettyString(object? obj) {
		if(obj == null) return "null";
		var type = obj.GetType();
		if(obj is Type) type = typeof(Type);
		if(!Printers.ContainsKey(type)) {
			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				dynamic dobj = obj;
				return $"[{((object) dobj.Key).ToPrettyString()}] = {((object) dobj.Value).ToPrettyString()}";
			}
			if(!type.GetInterfaces().Contains(typeof(IEnumerable))) return GenericPretty(obj);
			var temp = Enumeratorable(((IEnumerable) obj).GetEnumerator()).ToList();
			var prefix = $"{ToPrettyString(type.IsArray ? type.GetElementType() : type)}[{temp.Count}]";
			switch(temp.Count) {
				case 0: return prefix;
				case 1: return prefix + $" {{ {ToPrettyString(temp[0])} }}";
				default:
					return prefix + " {\n" + string.Join(", \n", temp.Select(x => Indentify(ToPrettyString(x)))) + "\n}";
			}
		}

		var printer = Printers[type];
		return printer.IsStatic
			? (string) printer.Invoke(null, new[] { obj })!
			: (string) printer.Invoke(obj, null)!;
	}

	static string GenericPretty(object obj) {
		var type = obj.GetType();
		if(type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Count(x => x.Name == "ToString" && x.DeclaringType != typeof(object)) != 0) return obj.ToString()!;

		var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		var prefix = type.FullName + " {";
		switch(fields.Length) {
			case 0: return prefix;
			case 1: return prefix + $" {fields[0].Name} = {fields[0].GetValue(obj).ToPrettyString()} }}";
			default:
				return prefix + "\n" + string.Join(", \n", fields.Select(field => Indentify(field.Name + " = " + field.GetValue(obj).ToPrettyString()))) + "\n}";
		}
	}
		
	[PrettyPrinter]
	// ReSharper disable once UnusedMember.Local
	static string PrettyPrintThis(string value) {
		var ret = value.Aggregate("\"", (current, c) => current + c switch {
			'\t' => "\\t",
			'\n' => "\\n",
			'\r' => "\\r",
			'\\' => "\\\\",
			'"' => "\\\"",
			{ } when char.IsControl(c) || c < ' ' || c >= 0x7f && c <= 0xff => $"\\x{(int) c:x02}",
			{ } when c > 0xff => "\\u...",
			_ => c
		});
		return ret + "\"";
	}

	static readonly IReadOnlyDictionary<string, string> TypeMap = new Dictionary<string, string> {
		["System.String"] = "string",
		["System.Collections.Generic.List"] = "List",
		["System.Collections.Generic.IEnumerable"] = "IEnumerable",
		["System.Collections.Generic.Dictionary"] = "Dictionary",
		["System.Collections.Generic.ICollection"] = "ICollection",
		["System.Collections.Generic.IList"] = "IList",
		["System.Collections.Generic.IReadOnlyDictionary"] = "IReadOnlyDictionary",
		["System.Collections.Generic.IReadOnlyList"] = "IReadOnlyList",
		["System.Int32"] = "int",
		["System.Int64"] = "long",
		["System.UInt32"] = "uint",
		["System.UInt64"] = "ulong",
		["System.IntPtr"] = "nint",
		["System.UIntPtr"] = "nuint",
	};

	static string RenameType(string name) =>
		TypeMap.TryGetValue(name, out var mapped)
			? mapped
			: name;

	[PrettyPrinter]
	// ReSharper disable once UnusedMember.Local
	static string PrettyPrintThis(Type type) => 
		type.IsGenericType
			? $"{RenameType(type.FullName!.Split('`')[0])}<{string.Join(", ", type.GetGenericArguments().Select(PrettyPrintThis))}>"
			: type.IsArray
				? $"{PrettyPrintThis(type.GetElementType()!)}[{new(',', type.GetArrayRank() - 1)}]"
				: RenameType(type.ToString());
}