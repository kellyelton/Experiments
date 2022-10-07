namespace VNetApp.ProminentColorSmall;

public struct TrainingProgress
{
    public int Progress = -1;

    public int MaxProgress = -1;

    public int MutationPoolSize = -1;

    public double CreateRandomNetChance = -1;

    public TrainingProgress(int maxProgress) {
        Progress = 0;
        MutationPoolSize = 0;
        MaxProgress = maxProgress;
        CreateRandomNetChance = 0;
    }
}
