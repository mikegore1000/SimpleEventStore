namespace SimpleEventStore.Tests.Events
{
    public class OrderCreated
    {
        public string OrderId { get; private set; }

        public OrderCreated(string orderId)
        {
            OrderId = orderId;
        }
    }
}