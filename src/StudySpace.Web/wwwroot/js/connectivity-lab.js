(() => {
  const lab = document.getElementById('connectivityLab');
  if (!lab) return;

  const metrics = new Map();
  const setup = (name) => {
    const element = lab.querySelector(`[data-transport="${name}"]`);
    const state = { element, requests: 0, messages: 0, latencyTotal: 0 };
    metrics.set(name, state);
    return state;
  };

  const update = (state, payload, startedAt) => {
    state.messages += 1;
    state.latencyTotal += Date.now() - startedAt;
    state.element.querySelector('[data-value]').textContent = payload.activeEditors;
    state.element.querySelector('[data-requests]').textContent = state.requests;
    state.element.querySelector('[data-messages]').textContent = state.messages;
    state.element.querySelector('[data-latency]').textContent = Math.round(state.latencyTotal / state.messages);
    state.element.querySelector('[data-status]').textContent = `Версія ${payload.version} · ${new Date(payload.timestamp).toLocaleTimeString('uk-UA')}`;
    state.element.classList.add('pulse-received');
    window.setTimeout(() => state.element.classList.remove('pulse-received'), 400);
  };

  const polling = setup('polling');
  const poll = async () => {
    const startedAt = Date.now();
    polling.requests += 1;
    try {
      const response = await fetch('/api/connectivity/state', { cache: 'no-store' });
      update(polling, await response.json(), startedAt);
    } finally {
      window.setTimeout(poll, 2000);
    }
  };

  const longPolling = setup('long-poll');
  let longPollVersion = -1;
  const longPoll = async () => {
    const startedAt = Date.now();
    longPolling.requests += 1;
    try {
      const response = await fetch(`/api/connectivity/long-poll?version=${longPollVersion}`, { cache: 'no-store' });
      const payload = await response.json();
      longPollVersion = payload.version;
      update(longPolling, payload, startedAt);
      longPoll();
    } catch {
      longPolling.element.querySelector('[data-status]').textContent = 'Повторна спроба…';
      window.setTimeout(longPoll, 1500);
    }
  };

  const webSocket = setup('websocket');
  const protocol = location.protocol === 'https:' ? 'wss' : 'ws';
  const connectWebSocket = () => {
    const socket = new WebSocket(`${protocol}://${location.host}/api/connectivity/websocket`);
    webSocket.requests += 1;
    webSocket.element.querySelector('[data-requests]').textContent = webSocket.requests;
    socket.addEventListener('open', () => { webSocket.element.querySelector('[data-status]').textContent = 'Підключено'; });
    socket.addEventListener('message', event => update(webSocket, JSON.parse(event.data), Date.now()));
    socket.addEventListener('close', () => {
      webSocket.element.querySelector('[data-status]').textContent = 'Повторне підключення…';
      window.setTimeout(connectWebSocket, 1500);
    });
  };

  poll();
  longPoll();
  connectWebSocket();
})();

