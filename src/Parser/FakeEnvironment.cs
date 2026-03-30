using Execution;

using Runtime;

namespace Parser;

public class FakeEnvironment(List<RuntimeValue> inputValues) : IEnvironment
{
    private readonly List<RuntimeValue> results = [];
    private List<RuntimeValue> inputs = [..inputValues];

    public IReadOnlyList<RuntimeValue> Results => results;

    public void PrintValue(string value)
    {
        results.Add(new RuntimeValue(value));
    }

    public RuntimeValue ReadValue(RuntimeValueType type)
    {
        if (inputs.Count == 0)
        {
            return new RuntimeValue(0);
        }

        RuntimeValue value = inputs[0];
        inputs.RemoveAt(0);
        return value;
    }
}