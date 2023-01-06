namespace RottrModManager.Shared
{
    public interface ITaskProgress
    {
        void Begin(string statusText);
        void Report(float progress);
        void End();
    }
}
