// Initialize replica set if not already initialized
try {
  rs.status();
} catch (e) {
  rs.initiate();
}
db = db.getSiblingDB('outbox');
// Payments Collection
db.createCollection("Payments", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["Id", "Amount", "Currency", "PaymentMethod", "CustomerId", "CreatedAt", "Status"],
      properties: {
        Id: { bsonType: "binData", description: "must be a UUID" },
        Amount: { bsonType: "decimal", description: "must be a decimal" },
        Currency: { bsonType: "string", minLength: 3, maxLength: 3 },
        PaymentMethod: { bsonType: "string", maxLength: 50 },
        CustomerId: { bsonType: "binData", description: "must be a UUID" },
        CreatedAt: { bsonType: "date" },
        Status: {
          enum: ["Pending", "Processing", "Completed", "Failed", "Cancelled"]
        }
      }
    }
  }
});
db.Payments.createIndex({ Status: 1 });
db.Payments.createIndex({ CustomerId: 1 });
db.Payments.createIndex({ CreatedAt: 1 });
db.Payments.createIndex({ Id: 1 }, { unique: true });

// Users Collection
db.createCollection("Users", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["Id", "Name"],
      properties: {
        Id: { bsonType: "binData", description: "must be a UUID" },
        Name: { bsonType: "string", maxLength: 80 }
      }
    }
  }
});
db.Users.createIndex({ Id: 1 }, { unique: true });

// Outbox Collection
db.createCollection("Outbox", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["Id", "SpecVersion", "Type", "Source", "Time", "CreateDateUtc", "PartitionKey", "AutoSequence"],
      properties: {
        Id: { bsonType: "string", maxLength: 450 },
        SpecVersion: { bsonType: "string", minLength: 1, maxLength: 10 },
        Type: { bsonType: "string", minLength: 1, maxLength: 255 },
        Source: { bsonType: "string", minLength: 1, maxLength: 255 },
        Time: { bsonType: "date" },
        CreateDateUtc: { bsonType: "date" },
        DataContentType: { bsonType: ["string", "null"], maxLength: 100 },
        DataSchema: { bsonType: ["string", "null"], maxLength: 255 },
        Subject: { bsonType: ["string", "null"], maxLength: 255 },
        Data: { bsonType: ["binData", "null"] },
        DataRef: { bsonType: ["string", "null"], maxLength: 500 },
        TraceParent: { bsonType: ["string", "null"], maxLength: 55 },
        Sequence: { bsonType: ["long", "null"] },
        AutoSequence: { bsonType: "long" },
        PartitionKey: { bsonType: "string", maxLength: 400 }
      }
    }
  }
});
db.Outbox.createIndex({ Type: 1 });
db.Outbox.createIndex({ Source: 1 });
db.Outbox.createIndex({ Time: 1 });
db.Outbox.createIndex({ Id: 1, Source: 1 }, { unique: true });

// HighRiskOutbox Collection
db.createCollection("HighRiskOutbox", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["Id", "SpecVersion", "Type", "Source", "Time", "CreateDateUtc", "PartitionKey", "AutoSequence"],
      properties: {
        Id: { bsonType: "string", maxLength: 450 },
        SpecVersion: { bsonType: "string", minLength: 1, maxLength: 10 },
        Type: { bsonType: "string", minLength: 1, maxLength: 255 },
        Source: { bsonType: "string", minLength: 1, maxLength: 255 },
        Time: { bsonType: "date" },
        CreateDateUtc: { bsonType: "date" },
        DataContentType: { bsonType: ["string", "null"], maxLength: 100 },
        DataSchema: { bsonType: ["string", "null"], maxLength: 255 },
        Subject: { bsonType: ["string", "null"], maxLength: 255 },
        Data: { bsonType: ["binData", "null"] },
        DataRef: { bsonType: ["string", "null"], maxLength: 500 },
        TraceParent: { bsonType: ["string", "null"], maxLength: 55 },
        Sequence: { bsonType: ["long", "null"] },
        AutoSequence: { bsonType: "long" },
        PartitionKey: { bsonType: "string", maxLength: 400 }
      }
    }
  }
});
db.HighRiskOutbox.createIndex({ Type: 1 });
db.HighRiskOutbox.createIndex({ Source: 1 });
db.HighRiskOutbox.createIndex({ Time: 1 });
db.HighRiskOutbox.createIndex({ Id: 1, Source: 1 }, { unique: true });