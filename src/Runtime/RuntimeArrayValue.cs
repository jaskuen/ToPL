using Ast.Declarations;

namespace Runtime;

public class RuntimeArrayValue(TypeReference elementType, IReadOnlyList<RuntimeValue> values)
{
    private readonly List<RuntimeValue> values = values.ToList();

    public TypeReference ElementType { get; } = elementType;

    public IReadOnlyList<RuntimeValue> Values => values;

    public RuntimeValue Get(int index)
    {
        EnsureIndexInBounds(index);
        return values[index];
    }

    public void Set(int index, RuntimeValue value)
    {
        EnsureIndexInBounds(index);
        values[index] = value;
    }

    private void EnsureIndexInBounds(int index)
    {
        if (index < 0 || index >= values.Count)
        {
            throw new IndexOutOfRangeException(
                $"Array index {index} is out of bounds for length {values.Count}.");
        }
    }
}
