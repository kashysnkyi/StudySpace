(() => {
  const shell = document.querySelector('.editor-shell');
  if (!shell) return;

  const documentId = shell.dataset.documentId;
  const container = document.getElementById('editorBlocks');
  const emptyState = document.getElementById('emptyEditor');
  const connectionStatus = document.getElementById('connectionStatus');
  const collaboratorNotice = document.getElementById('collaboratorNotice');
  const apiUrl = `/api/documents/${documentId}/blocks`;
  const saveTimers = new Map();
  let draggedBlock = null;

  const blockTypeNames = ['Абзац', 'Заголовок', 'Список', 'Код', 'Цитата'];
  const updateEmptyState = () => { emptyState.hidden = container.children.length > 0; };

  const request = async (url, options = {}) => {
    const response = await fetch(url, {
      ...options,
      headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }
    });
    if (!response.ok) throw new Error(`Request failed with ${response.status}`);
    return response.status === 204 ? null : response.json();
  };

  const createBlockElement = (block) => {
    const element = document.createElement('article');
    element.className = 'editor-block remote-change';
    element.dataset.blockId = block.id;
    element.draggable = true;

    const handle = document.createElement('button');
    handle.className = 'drag-handle';
    handle.type = 'button';
    handle.ariaLabel = 'Перемістити блок';
    handle.textContent = '⋮⋮';

    const fields = document.createElement('div');
    fields.className = 'block-fields';
    const select = document.createElement('select');
    select.className = 'form-select form-select-sm block-type';
    select.ariaLabel = 'Тип блоку';
    blockTypeNames.forEach((name, index) => select.add(new Option(name, index, index === block.type, index === block.type)));
    const textarea = document.createElement('textarea');
    textarea.className = 'form-control block-content';
    textarea.rows = 3;
    textarea.ariaLabel = 'Вміст блоку';
    textarea.value = block.content;
    const state = document.createElement('span');
    state.className = 'save-state';
    state.ariaLive = 'polite';
    state.textContent = 'Збережено';
    fields.append(select, textarea, state);

    const remove = document.createElement('button');
    remove.className = 'delete-block';
    remove.type = 'button';
    remove.ariaLabel = 'Видалити блок';
    remove.textContent = '×';
    element.append(handle, fields, remove);
    window.setTimeout(() => element.classList.remove('remote-change'), 1200);
    wireBlock(element);
    return element;
  };

  const saveBlock = async (block) => {
    const state = block.querySelector('.save-state');
    state.textContent = 'Збереження…';
    try {
      await request(`${apiUrl}/${block.dataset.blockId}`, {
        method: 'PUT',
        body: JSON.stringify({
          type: Number(block.querySelector('.block-type').value),
          content: block.querySelector('.block-content').value
        })
      });
      state.textContent = 'Збережено';
    } catch {
      state.textContent = 'Не вдалося зберегти';
      state.classList.add('text-danger');
    }
  };

  const scheduleSave = (block) => {
    const existing = saveTimers.get(block.dataset.blockId);
    window.clearTimeout(existing);
    block.querySelector('.save-state').textContent = 'Не збережено';
    saveTimers.set(block.dataset.blockId, window.setTimeout(() => saveBlock(block), 550));
  };

  const persistOrder = async () => {
    const blockIds = [...container.children].map(block => block.dataset.blockId);
    if (blockIds.length === 0) return;
    await request(`${apiUrl}/order`, { method: 'PUT', body: JSON.stringify({ blockIds }) });
  };

  const wireBlock = (block) => {
    block.querySelector('.block-content').addEventListener('input', () => scheduleSave(block));
    block.querySelector('.block-type').addEventListener('change', () => saveBlock(block));
    block.querySelector('.block-content').addEventListener('focus', () => {
      connection?.invoke('CursorChanged', documentId, block.dataset.blockId, 'Інший учасник').catch(() => {});
    });
    block.querySelector('.delete-block').addEventListener('click', async () => {
      await request(`${apiUrl}/${block.dataset.blockId}`, { method: 'DELETE' });
      block.remove();
      updateEmptyState();
    });
    block.addEventListener('dragstart', () => { draggedBlock = block; block.classList.add('dragging'); });
    block.addEventListener('dragend', () => { block.classList.remove('dragging'); draggedBlock = null; });
    block.addEventListener('dragover', event => {
      event.preventDefault();
      if (!draggedBlock || draggedBlock === block) return;
      const rectangle = block.getBoundingClientRect();
      container.insertBefore(draggedBlock, event.clientY < rectangle.top + rectangle.height / 2 ? block : block.nextSibling);
    });
    block.addEventListener('drop', async event => { event.preventDefault(); await persistOrder(); });
  };

  container.querySelectorAll('.editor-block').forEach(wireBlock);
  updateEmptyState();

  document.querySelectorAll('[data-add-block]').forEach(button => {
    button.addEventListener('click', async () => {
      const type = Number(button.dataset.addBlock);
      const created = await request(apiUrl, {
        method: 'POST',
        body: JSON.stringify({ type, content: type === 1 ? 'Новий заголовок' : 'Почніть писати…' })
      });
      if (!container.querySelector(`[data-block-id="${created.id}"]`)) {
        container.append(createBlockElement(created));
      }
      updateEmptyState();
    });
  });

  const connection = window.signalR ? new signalR.HubConnectionBuilder()
    .withUrl('/hubs/collaboration')
    .withAutomaticReconnect()
    .build() : null;

  if (!connection) {
    connectionStatus.textContent = 'Бібліотека realtime недоступна';
    return;
  }

  const flash = (block) => {
    block?.classList.add('remote-change');
    window.setTimeout(() => block?.classList.remove('remote-change'), 1200);
  };

  connection.on('blockCreated', block => {
    if (!container.querySelector(`[data-block-id="${block.id}"]`)) container.append(createBlockElement(block));
    updateEmptyState();
  });
  connection.on('blockUpdated', block => {
    const element = container.querySelector(`[data-block-id="${block.id}"]`);
    if (!element || element.contains(document.activeElement)) return;
    element.querySelector('.block-type').value = block.type;
    element.querySelector('.block-content').value = block.content;
    flash(element);
  });
  connection.on('blockDeleted', payload => {
    container.querySelector(`[data-block-id="${payload.blockId}"]`)?.remove();
    updateEmptyState();
  });
  connection.on('blocksReordered', ids => ids.forEach(id => {
    const element = container.querySelector(`[data-block-id="${id}"]`);
    if (element) container.append(element);
  }));
  connection.on('cursorChanged', payload => {
    const element = container.querySelector(`[data-block-id="${payload.blockId}"]`);
    flash(element);
    collaboratorNotice.textContent = `${payload.collaborator} редагує блок.`;
    collaboratorNotice.hidden = false;
    window.setTimeout(() => { collaboratorNotice.hidden = true; }, 2500);
  });
  connection.onreconnecting(() => { connectionStatus.textContent = 'Повторне підключення…'; });
  connection.onreconnected(() => { connectionStatus.textContent = 'Realtime підключено'; connection.invoke('JoinDocument', documentId); });
  connection.onclose(() => { connectionStatus.textContent = 'Realtime відключено'; });

  connection.start()
    .then(() => connection.invoke('JoinDocument', documentId))
    .then(() => { connectionStatus.textContent = 'Realtime підключено'; })
    .catch(() => { connectionStatus.textContent = 'Realtime недоступний'; });
})();

