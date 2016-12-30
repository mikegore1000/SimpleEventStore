function appendToStream(documents) {
    var context = getContext();
    var collection = context.getCollection();
    var collectionLink = collection.getSelfLink();

    var index = 0;

    createDocument(documents[index]);

    // NOTE: isAccepted states if the write is going to be processed
    function createDocument(document) {
        var accepted = collection.createDocument(collectionLink, document, onDocumentCreated);

        if (!accepted) throw new Error("Document not accepted for creation.");
    }

    function onDocumentCreated(err, doc, options) {
        if (err) throw err;

        index++;

        if (index < documents.length) {
            createDocument(documents[index]);
        }
        else {
            context.getResponse().setBody("Events written successfully");
        }
    }
}