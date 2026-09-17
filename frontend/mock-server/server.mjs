// Small stand-in for the real Postgres-backed API, used only for local frontend development
// before the backend is deployed. Not part of the production build.
import { createServer } from 'node:http';
import { randomUUID } from 'node:crypto';

const PORT = process.env.MOCK_PORT ?? 4000;

function outcomes(pattern) {
  return pattern.split('').map((c) => (c === 'P' ? 'Passed' : 'Failed'));
}

const repositories = [
  { id: 'repo-widgets', owner: 'acme', name: 'widgets', totalTests: 4, quarantinedCount: 1 },
  { id: 'repo-payments', owner: 'acme', name: 'payments-service', totalTests: 3, quarantinedCount: 0 },
];

const testsByRepo = {
  'repo-widgets': [
    {
      id: randomUUID(),
      suiteName: 'Checkout',
      testName: 'completes_purchase_with_saved_card',
      status: 'Quarantined',
      flipRate: 0.83,
      failureRate: 0.5,
      lastUpdatedAt: new Date().toISOString(),
      quarantinedAt: new Date().toISOString(),
      recentOutcomes: outcomes('PFPFPF'),
    },
    {
      id: randomUUID(),
      suiteName: 'Checkout',
      testName: 'applies_discount_code',
      status: 'Suspicious',
      flipRate: 0.21,
      failureRate: 0.15,
      lastUpdatedAt: new Date().toISOString(),
      quarantinedAt: null,
      recentOutcomes: outcomes('PPPFPPPPPPPPFPPPPPPP'),
    },
    {
      id: randomUUID(),
      suiteName: 'Inventory',
      testName: 'reserves_stock_on_hold',
      status: 'Stable',
      flipRate: 0,
      failureRate: 0,
      lastUpdatedAt: new Date().toISOString(),
      quarantinedAt: null,
      recentOutcomes: outcomes('PPPPPPPPPPPP'),
    },
    {
      id: randomUUID(),
      suiteName: 'Inventory',
      testName: 'rejects_negative_adjustment',
      status: 'Stable',
      flipRate: 1,
      failureRate: 1,
      lastUpdatedAt: new Date().toISOString(),
      quarantinedAt: null,
      recentOutcomes: outcomes('FFFFFFFFFF'),
    },
  ],
  'repo-payments': [
    {
      id: randomUUID(),
      suiteName: 'Ledger',
      testName: 'settles_batch_within_window',
      status: 'Stable',
      flipRate: 0,
      failureRate: 0,
      lastUpdatedAt: new Date().toISOString(),
      quarantinedAt: null,
      recentOutcomes: outcomes('PPPPPPPPPP'),
    },
    {
      id: randomUUID(),
      suiteName: 'Ledger',
      testName: 'reconciles_partial_refund',
      status: 'Stable',
      flipRate: 0,
      failureRate: 0,
      lastUpdatedAt: new Date().toISOString(),
      quarantinedAt: null,
      recentOutcomes: outcomes('PPPPPPPP'),
    },
    {
      id: randomUUID(),
      suiteName: 'Webhooks',
      testName: 'retries_on_5xx',
      status: 'Stable',
      flipRate: 0,
      failureRate: 0,
      lastUpdatedAt: new Date().toISOString(),
      quarantinedAt: null,
      recentOutcomes: outcomes('PPPPP'),
    },
  ],
};

function withCors(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
}

function sendJson(res, status, body) {
  withCors(res);
  res.writeHead(status, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(body));
}

const server = createServer(async (req, res) => {
  if (req.method === 'OPTIONS') {
    withCors(res);
    res.writeHead(204);
    res.end();
    return;
  }

  const url = new URL(req.url, `http://localhost:${PORT}`);

  if (req.method === 'GET' && url.pathname === '/api/repositories') {
    sendJson(res, 200, repositories);
    return;
  }

  if (req.method === 'POST' && url.pathname === '/api/repositories') {
    let body = '';
    for await (const chunk of req) body += chunk;
    const { owner, name } = JSON.parse(body || '{}');
    const id = `repo-${randomUUID()}`;
    repositories.push({ id, owner, name, totalTests: 0, quarantinedCount: 0 });
    testsByRepo[id] = [];
    sendJson(res, 201, { id, owner, name, webhookSecret: randomUUID().replace(/-/g, '') });
    return;
  }

  const testsMatch = url.pathname.match(/^\/api\/repositories\/([^/]+)\/tests$/);
  if (req.method === 'GET' && testsMatch) {
    sendJson(res, 200, testsByRepo[testsMatch[1]] ?? []);
    return;
  }

  sendJson(res, 404, { error: 'Not found' });
});

server.listen(PORT, () => {
  console.log(`FlakeGuard mock API listening on http://localhost:${PORT}`);
});
