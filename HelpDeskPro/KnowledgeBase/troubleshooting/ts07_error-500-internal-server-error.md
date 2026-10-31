---
title: Error 500 Internal Server Error
category: Troubleshooting
audience: Agent
status: Approved
version: 1
---
# Symptoms
Users see an error page with a correlation code.

# Fix
Ask the user for the correlation code, search the application log for it and read the logged exception. The stack trace is only in the log, never in the browser.
