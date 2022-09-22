using System.Diagnostics;

namespace VNet;

[DebuggerDisplay("{Id} {Type}")]
public class Neuron
{
    public long Id { get; } = Random.Shared.NextInt64();

    public NeuronType Type { get; }

    public double Value { get; private set; }

    public IReadOnlyList<Neuron> Outputs => _outputs.AsReadOnly();

    public IReadOnlyList<double> OutputWeights => _output_weights.AsReadOnly();

    public bool HasInputs => Type == NeuronType.Input || _inputs.Count > 0;

    private readonly List<Neuron> _outputs = new();
    private readonly List<double> _output_weights = new();
    private readonly HashSet<long> _output_ids = new();
    private readonly List<Neuron> _inputs = new();
    private readonly List<double> _input_values = new();

    public Neuron(NeuronType type) {
        Type = type;
    }

    public Neuron(Neuron source) {
        if (source is null) throw new ArgumentNullException(nameof(source));

        Type = source.Type;
        Id = source.Id;
    }

    public void Push(double v) {
        switch (Type) {
            case NeuronType.Input:
                Value = v;

                foreach (var output in _outputs) {
                    output.Push(v);
                }

                break;
            case NeuronType.Output:
                var k = Math.Exp(v);

                Value = k / (1.0d + k);

                break;
            case NeuronType.Hidden:
                _input_values.Add(v);

                if (_input_values.Count == _inputs.Count) {
                    var sum = 0d;
                    for (var i = 0; i < _input_values.Count; i++) {
                        sum += _input_values[i];
                    }

                    Value = sum;

                    for (var i = 0; i < _outputs.Count; i++) {
                        var o = _outputs[i];
                        var w = _output_weights[i];

                        var co = sum * w;

                        o.Push(co);
                    }

                    _input_values.Clear();
                }

                break;
            default: throw new InvalidOperationException($"Unexpected {nameof(NeuronType)} {Type}");
        }
    }

    public void AddOutput(Neuron output, double weight) {
        if (!_output_ids.Add(output.Id)) return;

        _outputs.Add(output);
        output._inputs.Add(this);

        _output_weights.Add(weight);
    }

    public void RemoveOutput(int output_index) {
        if (output_index < 0 || output_index >= _outputs.Count) throw new ArgumentOutOfRangeException(nameof(output_index), $"{nameof(output_index)} out of bounds");

        var n = _outputs[output_index];
        _outputs.RemoveAt(output_index);
        _output_weights.RemoveAt(output_index);
        _output_ids.Remove(n.Id);

        n._inputs.Remove(this);
    }

    public void RetargetOutput(int output_index, Neuron new_target) {
        if (output_index < 0 || output_index >= _outputs.Count) throw new ArgumentOutOfRangeException(nameof(output_index), $"{nameof(output_index)} out of bounds");

        if (!_output_ids.Add(new_target.Id)) return; // We already have an output to new_target

        var old_target = _outputs[output_index];
        old_target._inputs.Remove(this);

        _output_ids.Remove(old_target.Id);

        _outputs[output_index] = new_target;

        new_target._inputs.Add(this);
    }

    public void RerouteOutput(int output_index, Neuron new_neuron) {
        if (output_index < 0 || output_index >= _outputs.Count) throw new ArgumentOutOfRangeException(nameof(output_index), $"{nameof(output_index)} out of bounds");

        if (!_output_ids.Add(new_neuron.Id)) return; // We already have an output to new_target

        var old_target = _outputs[output_index];
        old_target._inputs.Remove(this);

        _output_ids.Remove(old_target.Id);

        _outputs[output_index] = new_neuron;

        new_neuron._inputs.Add(this);
    }

    public void Disconnect() {
        while (_outputs.Count > 0) {
            RemoveOutput(0);
        }

        foreach (var input in _inputs.ToArray()) {
            for (var i = 0; i < input.Outputs.Count; i++) {
                if (ReferenceEquals(input.Outputs[i], this)) {
                    input.RemoveOutput(i);

                    break;
                }
            }
        }
    }

    public void AdjustOutput(int output_index, double weight) {
        if (output_index < 0 || output_index >= _outputs.Count) throw new ArgumentOutOfRangeException(nameof(output_index), $"{nameof(output_index)} out of bounds");
        if (weight < 0 || weight > 1) throw new ArgumentOutOfRangeException("weight must be between 0 and 1");

        _output_weights[output_index] = weight;
    }

    internal void Reset() {
        Value = 0;
        _input_values.Clear();
    }
}
