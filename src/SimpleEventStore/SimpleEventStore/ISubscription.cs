namespace SimpleEventStore
{
    public interface ISubscription
    {
        void Start();

        void Stop();
    }
}
