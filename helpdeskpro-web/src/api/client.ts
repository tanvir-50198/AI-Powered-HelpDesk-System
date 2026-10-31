/**
 * Thin fetch wrapper around the ASP.NET API.
 *  - cookie auth (Identity cookie) -> credentials are always sent
 *  - turns ProblemDetails / validation errors into a single readable message
 *  - tells the app when the session expired (401)
 */

export class ApiError extends Error {
  readonly status: number;
  readonly correlationId?: string;

  constructor(message: string, status: number, correlationId?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.correlationId = correlationId;
  }
}

type Query = Record<string, string | number | boolean | undefined | null>;

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  query?: Query;
  json?: unknown;
  form?: FormData;
  signal?: AbortSignal;
  /** Do not treat a 401 as "session expired" (used by login / me). */
  skipUnauthorizedHandler?: boolean;
}

const BASE = '/api';

let unauthorizedHandler: (() => void) | null = null;
export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

function buildUrl(path: string, query?: Query): string {
  const url = new URL(BASE + path, window.location.origin);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null || value === '') continue;
      url.searchParams.set(key, String(value));
    }
  }
  return url.pathname + url.search;
}

interface ProblemBody {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  correlationId?: string;
}

function messageFromProblem(status: number, body: ProblemBody | null): string {
  if (body?.errors) {
    const first = Object.values(body.errors).flat()[0];
    if (first) return first;
  }
  if (body?.detail) return body.detail;
  if (body?.title && status !== 500) return body.title;

  switch (status) {
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return "You don't have permission to do that.";
    case 404:
      return "We couldn't find what you were looking for.";
    case 429:
      return 'Too many requests. Please wait a moment and try again.';
    case 502:
    case 503:
      return 'The service is temporarily unavailable. Please try again shortly.';
    default:
      return status >= 500
        ? 'Something went wrong on our side. Please try again.'
        : 'The request could not be completed.';
  }
}

export async function request<T = void>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', query, json, form, signal, skipUnauthorizedHandler } = options;

  const headers: Record<string, string> = { Accept: 'application/json' };
  let body: BodyInit | undefined;
  if (form) {
    body = form; // the browser sets the multipart boundary
  } else if (json !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(json);
  }

  let response: Response;
  try {
    response = await fetch(buildUrl(path, query), {
      method,
      headers,
      body,
      signal,
      credentials: 'include',
    });
  } catch (error) {
    if ((error as Error).name === 'AbortError') throw error;
    throw new ApiError("Can't reach the server. Check your connection and try again.", 0);
  }

  const correlationId = response.headers.get('X-Correlation-Id') ?? undefined;

  if (!response.ok) {
    let problem: ProblemBody | null = null;
    try {
      problem = (await response.json()) as ProblemBody;
    } catch {
      /* empty body (401 / 403 / 429 have none) */
    }
    if (response.status === 401 && !skipUnauthorizedHandler) {
      unauthorizedHandler?.();
    }
    throw new ApiError(
      messageFromProblem(response.status, problem),
      response.status,
      problem?.correlationId ?? correlationId,
    );
  }

  if (response.status === 204) return undefined as T;
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}
