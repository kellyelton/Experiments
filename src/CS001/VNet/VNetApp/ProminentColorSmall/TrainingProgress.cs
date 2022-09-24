namespace VNetApp.ProminentColorSmall;

public struct TrainingProgress
{
    public double Progress = double.NaN;

    public int MaxProgress = -1;

    public TrainingProgress(int maxProgress) {
        Progress = 0;
        MaxProgress = maxProgress;
    }
}
