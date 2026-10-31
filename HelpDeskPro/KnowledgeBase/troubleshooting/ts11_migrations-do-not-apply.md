---
title: Migrations do not apply
category: Troubleshooting
audience: Agent
status: Approved
version: 1
---
# Symptoms
Tables are missing or the model does not match the database.

# Fix
Run 'dotnet ef database update'. On a fresh database delete the old database and start again. Check that Database:UseMigrations matches how the database was created.
