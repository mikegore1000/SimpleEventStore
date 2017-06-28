# Simple Event Store

Simple Event Store (SES) provides a lightweight event sourcing abstraction.

## Key Features
- Full support for async/await for all persistence engines
- Optimistic concurrency for append operations
- Reading streams forward
- Catch up subscriptions

## Persistence Engines
SES supports the following
- Azure DocumentDB
- In Memory

All persistence engines run the same test scenarios to ensure feature parity.  At present only the DocumentDB persistence engine should be considered for production usage.

## Usage

### Creation
```csharp
var storageEngine = new InMemoryStorageEngine();
var eventStore = new EventStore(storageEngine);
```
Do not use storage engines directly, only interact with the event store using the EventStore class.  There should only be a single instance of the EventStore per process.  Creating transient instances will lead to a decrease in performance.

### Appending Events
```csharp
var expectedRevision = 0;

await eventStore.AppendToStream(streamId, expectedRevision, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));
```
The expected stream revision would either be set to 0 for a new stream, or to the expected event number.  If the latest event number in the database differs then a concurrency exception will be thrown.

### Reading Events
```csharp
var events = await eventStore.ReadStreamForwards(streamId, startPosition: 2, numberOfEventsToRead: 1);
// or
var events = await subject.ReadStreamForwards(streamId);
```
You can either read all events in a stream, or a subset of events.  Only read all events if you know the maximum size of a stream is going to be low and that you always need to read all events as part of your workload e.g. replaying events to project current state for a DDD aggregate.

### Catch up subscriptions
```csharp
eventStore.SubscribeToAll(
    (events, checkpoint) =>
    {
        // Process events...
        // Then store the checkpoint...
    },
    "<PREVIOUS CHECKPOINT>");
```
Catch up subscriptions allow you to subscribe to events after they have been written to the event store.  If no previous checkpoint is supplied then the subscription will process all events in the event store.  

Storing the checkpoint allows you to resume a subscription in the case of process failure and should only be done once all events supplied in the callback have been processed.  This ensures that events are not missed.

It is possible to have multiple catch up subscriptions running, for example to
- Publish messages onto a service bus
- Populate a read model

Each subscription **must** perform an independant task.  It is not possible to run multiple instances of the same catch up subscription as it would mean high amounts of duplicate processing as each process instance would read the same checkpoint.

The number of events received will vary depending on the storage engine configuration.

## DocumentDB
```csharp
DocumentClient client; // Set this up as required
var databaseName = "MyEventStore";

var storageEngine = new AzureDocumentDbStorageEngine(
	client, 
	databaseName, 
	new DatabaseOptions(ConsistencyLevel.BoundedStaleness, 400), 
	new SubscriptionOptions(
		maxItemCount: 1, 
		pollEvery: TimeSpan.FromSeconds(0.5)));

await storageEngine.Initialise();
```
The DatabaseOptions allow you to specify the consistency level of the database along with the default number of RUs.  

Only use one of the following consistency levels
- Strong
- Bounded Staleness - use this if you need to geo-replicate the database

The SubscriptionOptions allow you to configure how catch up subscriptions work
- Max Item Count - the number of events to pull back in a polling operation
- Poll Every - how long to wait after a polling operation has completed

Calling the Initialise operation creates the underlying collection based on the DatabaseOptions.  This ensures the required stored procedure is present too.  It is safe to call this multiple times.