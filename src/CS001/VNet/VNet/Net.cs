namespace VNet;

public class Net
{
    public int Generation { get; private init; } = 1;

    public int Score { get; set; }

    public Neuron[] InputNeurons { get; }

    public Neuron[] OutputNeurons { get; }

    public Neuron[] HiddenNeurons { get; }

    public Neuron[] Neurons { get; init; }

    public Net() {
        InputNeurons = Neurons.Where(n => n.Type == NeuronType.Input).ToArray();
        OutputNeurons = Neurons.Where(n => n.Type == NeuronType.Output).ToArray();
        HiddenNeurons = Neurons.Where(n => n.Type == NeuronType.Hidden).ToArray();
    }

    public Net(Neuron[] neurons) {
        if (neurons is null) throw new ArgumentNullException(nameof(neurons));

        Neurons = neurons;
        InputNeurons = Neurons.Where(n => n.Type == NeuronType.Input).ToArray();
        OutputNeurons = Neurons.Where(n => n.Type == NeuronType.Output).ToArray();
        HiddenNeurons = Neurons.Where(n => n.Type == NeuronType.Hidden).ToArray();
    }

    public void Reset() {
        foreach (var n in Neurons) {
            n.ResetValue();
        }
    }

    public double[] Predict(double[] inputs) {
        if (inputs is null) throw new ArgumentNullException(nameof(inputs));
        if (inputs.Length > InputNeurons.Length) throw new ArgumentOutOfRangeException($"Net only has {InputNeurons.Length} input neurons, can't input {inputs.Length} values.");

        for (var i = 0; i < InputNeurons.Length; i++) {
            if (i < inputs.Length) {
                InputNeurons[i].Push(inputs[i]);
            } else {
                InputNeurons[i].Push(0);
            }
        }

        return OutputNeurons.Select(o => o.Value).ToArray();
    }

    public static Net Random(int input_count, int output_count, int max_hidden_count) {
        var hidden_count = System.Random.Shared.Next(2, max_hidden_count);

        var inputs = Enumerable.Range(0, input_count).Select(_ => new Neuron(NeuronType.Input)).ToArray();
        var outputs = Enumerable.Range(0, output_count).Select(_ => new Neuron(NeuronType.Output)).ToArray();
        var hiddens = Enumerable.Range(0, hidden_count).Select(_ => new Neuron(NeuronType.Hidden)).ToArray();

        var rando_count = System.Random.Shared.Next(hidden_count * 2, hidden_count * 100);
        //var rando_count = System.Random.Shared.Next((hidden_count * hidden_count) / 4, (hidden_count * hidden_count));

        var dests = hiddens.Concat(outputs).ToArray();

        // Make sure each neuron has at least 1 output
        foreach (var input in inputs) {
            var dest = Random(hiddens);
            input.AddOutput(dest);
        }
        foreach (var hidden in hiddens) {
            var dest = Random(dests);
            hidden.AddOutput(dest);
        }

        // Randomly fill connections
        var sources = inputs.Concat(hiddens).OrderBy(_ => System.Random.Shared.NextDouble()).ToArray();
        var random_sources = Enumerable
            .Range(0, rando_count)
            .Select(_ => Random(sources))
            .ToArray()
        ;

        foreach (var source in random_sources) {
            var dest = Random(dests);

            source.AddOutput(dest);
        }

        var neurons = inputs.Concat(hiddens).Concat(outputs).ToArray();

        return new Net(neurons);
    }

    private static T Random<T>(IList<T> col) {
        var num = System.Random.Shared.Next(0, col.Count);
        return col[num];
    }

    private static T Random<T>(params IList<T>[] cols) {
        if (cols.Length == 0) {
            var o = System.Random.Shared.Next(0, cols[0].Count);

            return cols[0][o];
        }

        var c = System.Random.Shared.Next(0, cols.Length);

        var i = System.Random.Shared.Next(0, cols[c].Count);

        return cols[c][i];
    }

    public static Net Mutate(Net parent) {
        var inputs = parent.InputNeurons.Select(n => new Neuron(n)).ToArray();
        var outputs = parent.OutputNeurons.Select(n => new Neuron(n)).ToArray();
        var hiddens = parent.HiddenNeurons.Select(n => new Neuron(n)).ToList();
        var all = inputs.Concat(hiddens).Concat(outputs);
        var lookup = all.ToDictionary(a => a.Id, a => a);

        // Reconnect neurons
        {
            for (var i = 0; i < inputs.Length; i++) {
                var input_neuron = inputs[i];
                var original_input_neuron = parent.InputNeurons[i];

                for (var o = 0; o < original_input_neuron.Outputs.Count; o++) {
                    var original_output_neuron = original_input_neuron.Outputs[o];

                    var ix = -1;
                    for (var yy = 0; yy < original_output_neuron.Inputs.Count; yy++) {
                        if (original_output_neuron.Inputs[yy].Id == original_input_neuron.Id) {
                            ix = yy;
                            break;
                        }
                    }

                    var dest_id = original_output_neuron.Id;
                    var weight = original_output_neuron.InputWeights[ix];

                    var dest = lookup[dest_id];

                    input_neuron.AddOutput(dest, weight);
                }
            }

            for (var i = 0; i < hiddens.Count; i++) {
                var input_neuron = hiddens[i];
                var original_input_neuron = parent.HiddenNeurons[i];

                for (var o = 0; o < original_input_neuron.Outputs.Count; o++) {
                    var original_output_neuron = original_input_neuron.Outputs[o];

                    var ix = -1;
                    for (var yy = 0; yy < original_output_neuron.Inputs.Count; yy++) {
                        if (original_output_neuron.Inputs[yy].Id == original_input_neuron.Id) {
                            ix = yy;
                            break;
                        }
                    }

                    var dest_id = original_output_neuron.Id;
                    var weight = original_output_neuron.InputWeights[ix];

                    var dest = lookup[dest_id];

                    input_neuron.AddOutput(dest, weight);
                }
            }
        }


        var mutation_count = System.Random.Shared.Next(0, lookup.Count + all.Sum(a => a.Outputs.Count)) * 2;

        for (var i = 0; i < mutation_count; i++) {
            var op = System.Random.Shared.NextDouble();


            if (op < 0.01) { // Remove existing hidden neuron
                if (hiddens.Count <= 1) { // min hidden neuron count I guess
                    i--;
                    continue;
                }

                var n = Random(hiddens);

                n.Disconnect();
                hiddens.Remove(n);
            } else if (op < 0.1) { // Create new hidden neuron
                var source = Random(inputs, hiddens);

                if (source.Outputs.Count == 0) {
                    i--;
                    continue;
                }

                // Grab a random output
                var output = System.Random.Shared.Next(0, source.Outputs.Count);

                // Store the destination of the output
                var original_output_destination = source.Outputs[output];

                // Create new hidden neuron
                var new_neuron = new Neuron(NeuronType.Hidden);
                hiddens.Add(new_neuron);

                // Route new hidden neuron to the original output destination
                new_neuron.AddOutput(original_output_destination);

                // Reroute the source output to the new neuron
                source.RetargetOutput(output, new_neuron);
            } else if (op < 0.2) { // Create new connection
                var source = Random(inputs, hiddens);
                var dest = Random(hiddens, outputs);

                if (source.Outputs.Contains(dest)) {
                    i--;
                    continue;
                }

                source.AddOutput(dest);
            } else if (op < 0.3) { // Remove existing connection
                var n = Random(inputs, hiddens);
                if (n.Outputs.Count == 0) {
                    i--;
                    continue;
                }

                var o = System.Random.Shared.Next(0, n.Outputs.Count);

                n.RemoveOutput(o);
            } else if (op < 0.4) { // Retarget existing connection
                var n = Random(inputs, hiddens);
                if (n.Outputs.Count == 0) {
                    i--;
                    continue;
                }

                var o = System.Random.Shared.Next(0, n.Outputs.Count);

                var no = Random(hiddens, outputs);

                n.RetargetOutput(o, no);
            } else { // Modify existing connection weight
                var n = Random(hiddens, outputs);
                if (n.Inputs.Count == 0) {
                    i--;
                    continue;
                }

                var input_index = System.Random.Shared.Next(0, n.Inputs.Count);
                var original_input_weight = n.InputWeights[input_index];

                // random number between -0.1 and 0.1
                var mod = ((System.Random.Shared.NextDouble() * 2) - 1) / 4;

                var new_input_weight = original_input_weight + mod;

                if (new_input_weight < 0) new_input_weight = 1 + new_input_weight;
                if (new_input_weight > 1) new_input_weight--;

                n.AdjustInputWeight(input_index, new_input_weight);
            }
        }


        // Return new net

        var neurons = inputs.Concat(hiddens).Concat(outputs).ToArray();

        return new Net(neurons) {
            Generation = parent.Generation + 1
        };
    }
}