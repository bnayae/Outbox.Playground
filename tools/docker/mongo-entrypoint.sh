#!/bin/bash
set -e

# Start MongoDB in the background
mongod --replSet rs0 --bind_ip_all &
MONGO_PID=$!

echo "Waiting for MongoDB to start..."
until mongosh --quiet --eval "db.adminCommand('ping')" > /dev/null 2>&1; do
  sleep 1
done

echo "MongoDB started successfully"

# Initialize replica set if not already initialized
echo "Checking replica set status..."
if ! mongosh --quiet --eval "rs.status()" > /dev/null 2>&1; then
  echo "Initializing replica set..."
  mongosh --quiet --eval "rs.initiate()" > /dev/null 2>&1
  echo "Replica set initialized"
  sleep 2
else
  echo "Replica set already initialized"
fi

# Check if database exists
DB_EXISTS=$(mongosh --quiet --eval "db.getMongo().getDBNames().includes('outbox')" | tail -1)

if [ "$DB_EXISTS" = "false" ]; then
  echo "Database 'outbox' does not exist. Running initialization script..."
  mongosh < /docker-entrypoint-initdb.d/init-db-mongo.js
  echo "Database and collections created successfully"
else
  echo "Database 'outbox' already exists"
fi

echo "MongoDB is ready for connections"

# Keep MongoDB running in foreground
wait $MONGO_PID