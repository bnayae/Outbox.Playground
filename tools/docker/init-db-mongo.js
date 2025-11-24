// Initialize replica set if not already initialized
try {
  rs.status();
} catch (e) {
  rs.initiate();
}
db = db.getSiblingDB('outbox');

db.createCollection("Payments");
db.createCollection("Users");
db.createCollection("Outbox");
db.createCollection("HighRiskOutbox");