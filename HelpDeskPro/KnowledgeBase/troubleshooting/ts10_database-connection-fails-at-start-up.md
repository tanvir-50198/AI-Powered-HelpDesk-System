---
title: Database connection fails at start-up
category: Troubleshooting
audience: Agent
status: Approved
version: 1
---
# Symptoms
The application stops with a database error.

# Fix
Start PostgreSQL, check the connection string (host, port, database, user, password) and make sure the database user may create tables.
