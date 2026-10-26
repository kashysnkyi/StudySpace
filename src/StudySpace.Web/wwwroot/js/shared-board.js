(() => {
  const shell = document.querySelector('.board-shell');
  const canvas = document.getElementById('sharedCanvas');
  if (!shell || !canvas || !window.signalR) return;

  const boardId = shell.dataset.boardId;
  const context = canvas.getContext('2d');
  const status = document.getElementById('boardStatus');
  const color = document.getElementById('strokeColor');
  const width = document.getElementById('strokeWidth');
  let currentStroke = null;

  const drawStroke = stroke => {
    if (!stroke.points || stroke.points.length < 2) return;
    context.beginPath();
    context.strokeStyle = stroke.color;
    context.lineWidth = stroke.width;
    context.lineCap = 'round';
    context.lineJoin = 'round';
    context.moveTo(stroke.points[0].x * canvas.width, stroke.points[0].y * canvas.height);
    for (const point of stroke.points.slice(1)) context.lineTo(point.x * canvas.width, point.y * canvas.height);
    context.stroke();
  };

  const pointFromEvent = event => {
    const rectangle = canvas.getBoundingClientRect();
    return {
      x: (event.clientX - rectangle.left) / rectangle.width,
      y: (event.clientY - rectangle.top) / rectangle.height
    };
  };

  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/collaboration')
    .withAutomaticReconnect()
    .build();

  connection.on('strokeDrawn', drawStroke);
  connection.on('boardCleared', () => context.clearRect(0, 0, canvas.width, canvas.height));
  connection.onreconnecting(() => { status.textContent = 'Повторне підключення…'; });
  connection.onreconnected(async () => {
    status.textContent = 'Realtime підключено';
    const strokes = await connection.invoke('JoinBoard', boardId);
    context.clearRect(0, 0, canvas.width, canvas.height);
    strokes.forEach(drawStroke);
  });

  connection.start()
    .then(() => connection.invoke('JoinBoard', boardId))
    .then(strokes => {
      strokes.forEach(drawStroke);
      status.textContent = 'Realtime підключено';
    })
    .catch(() => { status.textContent = 'Realtime недоступний'; });

  canvas.addEventListener('pointerdown', event => {
    canvas.setPointerCapture(event.pointerId);
    currentStroke = { color: color.value, width: Number(width.value), points: [pointFromEvent(event)] };
  });
  canvas.addEventListener('pointermove', event => {
    if (!currentStroke) return;
    const point = pointFromEvent(event);
    const previous = currentStroke.points[currentStroke.points.length - 1];
    currentStroke.points.push(point);
    drawStroke({ ...currentStroke, points: [previous, point] });
  });
  const finishStroke = async event => {
    if (!currentStroke) return;
    if (canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
    const completed = currentStroke;
    currentStroke = null;
    if (completed.points.length > 1 && connection.state === signalR.HubConnectionState.Connected) {
      await connection.invoke('DrawStroke', boardId, completed);
    }
  };
  canvas.addEventListener('pointerup', finishStroke);
  canvas.addEventListener('pointercancel', finishStroke);
  document.getElementById('clearBoard').addEventListener('click', async () => {
    context.clearRect(0, 0, canvas.width, canvas.height);
    if (connection.state === signalR.HubConnectionState.Connected) await connection.invoke('ClearBoard', boardId);
  });
})();

