﻿using System.Diagnostics;

namespace VNet;

[DebuggerDisplay("{Id} {Type}")]
public class Neuron
{
    public long Id { get; } = Random.Shared.NextInt64();

    public NeuronType Type { get; }

    public double Value { get; private set; }

    public double Bias { get; set; }

    public IReadOnlyList<Neuron> Outputs => _outputs.AsReadOnly();

    public IReadOnlyList<Neuron> Inputs => _inputs.AsReadOnly();

    public IReadOnlyList<double> InputWeights => _input_weights.AsReadOnly();

    public bool HasInputs => Type == NeuronType.Input || _inputs.Count > 0;

    private readonly List<Neuron> _outputs = new();
    private readonly HashSet<long> _output_ids = new();
    private readonly List<Neuron> _inputs = new();
    private readonly List<double> _input_values = new();
    private readonly List<double> _input_weights = new();

    public Neuron(NeuronType type) {
        Type = type;
    }

    public Neuron(Neuron source) {
        if (source is null) throw new ArgumentNullException(nameof(source));

        Type = source.Type;
        Id = source.Id;
        Bias = source.Bias;
    }

    private static double Sig(double a) {
        var k = Math.Exp(a);

        var sig = k / (1.0d + k);

        return sig;
    }

    public void Push(double v) {
        switch (Type) {
            case NeuronType.Input: {
                    Value = v;

                    foreach (var output in _outputs) {
                        output.Push(v);
                    }

                    break;
                }
            case NeuronType.Output: {
                    _input_values.Add(v);

                    if (_input_values.Count != _inputs.Count) break;

                    var input_value = 0d;
                    for (var i = 0; i < _input_values.Count; i++) {
                        var iv = _input_values[i];
                        var iw = _input_weights[i];
                        input_value += (iv * iw);
                    }
                    _input_values.Clear();

                    input_value += Bias;

                    input_value = Sig(input_value);

                    Value = input_value;

                    break;
                }
            case NeuronType.Hidden: {
                    _input_values.Add(v);

                    if (_input_values.Count != _inputs.Count) break;

                    var input_value = 0d;
                    for (var i = 0; i < _input_values.Count; i++) {
                        var iv = _input_values[i];
                        var iw = _input_weights[i];
                        input_value += (iv * iw);
                    }
                    _input_values.Clear();

                    input_value += Bias;

                    input_value = Sig(input_value);

                    Value = input_value;

                    for (var i = 0; i < _outputs.Count; i++) {
                        var o = _outputs[i];

                        o.Push(Value);
                    }

                    break;
                }
            default: throw new InvalidOperationException($"Unexpected {nameof(NeuronType)} {Type}");
        }
    }

    public void AddOutput(Neuron output) {
        if (!_output_ids.Add(output.Id)) return;

        _outputs.Add(output);
        output._inputs.Add(this);
        output._input_weights.Add(Random.Shared.NextDouble());
    }

    public void AddOutput(Neuron output, double weight) {
        if (!_output_ids.Add(output.Id)) return;

        _outputs.Add(output);
        output._inputs.Add(this);
        output._input_weights.Add(weight);
    }

    public void RemoveOutput(int output_index) {
        if (output_index < 0 || output_index >= _outputs.Count) throw new ArgumentOutOfRangeException(nameof(output_index), $"{nameof(output_index)} out of bounds");

        var n = _outputs[output_index];
        _outputs.RemoveAt(output_index);
        _output_ids.Remove(n.Id);

        var ix = n._inputs.IndexOf(this);
        n._inputs.RemoveAt(ix);
        n._input_weights.RemoveAt(ix);
    }

    public void RetargetOutput(int output_index, Neuron new_target) {
        if (output_index < 0 || output_index >= _outputs.Count) throw new ArgumentOutOfRangeException(nameof(output_index), $"{nameof(output_index)} out of bounds");

        if (!_output_ids.Add(new_target.Id)) return; // We already have an output to new_target

        var old_target = _outputs[output_index];
        var ix = old_target._inputs.IndexOf(this);

        var old_weight = old_target._input_weights[ix];

        old_target._inputs.RemoveAt(ix);
        old_target._input_weights.RemoveAt(ix);

        _output_ids.Remove(old_target.Id);

        _outputs[output_index] = new_target;

        new_target._inputs.Add(this);
        new_target._input_weights.Add(old_weight);
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

    public void AdjustInputWeight(int input_index, double weight) {
        if (input_index < 0 || input_index >= _input_weights.Count) throw new ArgumentOutOfRangeException(nameof(input_index), $"{nameof(input_index)} out of bounds");
        if (weight is < 0 or > 1) throw new ArgumentOutOfRangeException("weight must be between 0 and 1");

        _input_weights[input_index] = weight;
    }

    internal void ResetValue() {
        Value = double.NaN;
        _input_values.Clear();
    }
}
