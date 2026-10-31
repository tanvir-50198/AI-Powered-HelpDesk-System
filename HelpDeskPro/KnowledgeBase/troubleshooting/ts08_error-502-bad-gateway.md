---
title: Error 502 Bad Gateway
category: Troubleshooting
audience: Agent
status: Approved
version: 1
---
# Symptoms
A reverse proxy returns 502.

# Fix
The application process is not running or crashed. Check the process, the logs and the health endpoint /health, then restart the service.
