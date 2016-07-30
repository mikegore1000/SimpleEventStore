namespace SimpleEventStore.Tests.Events
{
    public class OrderDispatched
    {
        public string OrderId { get; private set; }

        public OrderDispatched(string orderId)
        {
            OrderId = orderId;
        }
    }
}