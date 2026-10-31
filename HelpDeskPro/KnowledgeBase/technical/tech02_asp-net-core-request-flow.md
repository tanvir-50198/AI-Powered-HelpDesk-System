---
title: ASP.NET Core request flow
category: Technical
audience: Agent
status: Approved
version: 1
---
# Flow
Browser sends a request, middleware runs, the controller checks login and role, calls a service, the service applies business rules and uses Entity Framework Core to read PostgreSQL.
