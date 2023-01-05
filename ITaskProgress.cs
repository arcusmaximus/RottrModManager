namespace RottrModManager
{
    internal interface ITaskProgress
    {
        void Begin(string statusText);
        void Report(float progress);
        void End();
    }
}
