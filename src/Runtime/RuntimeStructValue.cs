using Ast.Declarations;

namespace Runtime;

public class RuntimeStructValue(
    string typeName,
    IReadOnlyDictionary<string, TypeReference> fieldTypes,
    IReadOnlyDictionary<string, RuntimeValue> values)
{
    private readonly Dictionary<string, RuntimeValue> values = new(values);

    public string TypeName { get; } = typeName;

    public IReadOnlyDictionary<string, TypeReference> FieldTypes { get; } = fieldTypes;

    public RuntimeValue GetField(string name)
    {
        if (!values.TryGetValue(name, out RuntimeValue? value))
        {
            throw new ArgumentException($"Struct {TypeName} has no field {name}.");
        }

        return value;
    }

    public void SetField(string name, RuntimeValue value)
    {
        if (!values.ContainsKey(name))
        {
            throw new ArgumentException($"Struct {TypeName} has no field {name}.");
        }

        values[name] = value;
    }
}
