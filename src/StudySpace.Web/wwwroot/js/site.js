(() => {
  const search = document.querySelector('.global-search');
  const input = document.getElementById('globalSearch');
  const results = document.getElementById('searchResults');

  if (!search || !input || !results || !window.jQuery) {
    return;
  }

  let request = null;
  let debounceTimer = null;

  const closeResults = () => {
    results.replaceChildren();
    results.hidden = true;
  };

  const renderResults = (items) => {
    results.replaceChildren();

    if (items.length === 0) {
      const empty = document.createElement('div');
      empty.className = 'search-empty';
      empty.textContent = 'Відповідних документів не знайдено';
      results.append(empty);
    } else {
      for (const item of items) {
        const link = document.createElement('a');
        link.href = item.url;
        link.className = 'search-result';
        link.setAttribute('role', 'option');

        const title = document.createElement('strong');
        title.textContent = item.title;
        const workspace = document.createElement('span');
        workspace.textContent = item.workspace;

        link.append(title, workspace);
        results.append(link);
      }
    }

    results.hidden = false;
  };

  input.addEventListener('input', () => {
    window.clearTimeout(debounceTimer);
    request?.abort();
    const term = input.value.trim();

    if (term.length < 3) {
      closeResults();
      return;
    }

    debounceTimer = window.setTimeout(() => {
      request = window.jQuery.ajax({
        url: search.dataset.searchEndpoint,
        data: { term },
        dataType: 'json'
      })
        .done(renderResults)
        .fail((xhr, status) => {
          if (status !== 'abort') {
            closeResults();
          }
        });
    }, 250);
  });

  input.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      closeResults();
      input.blur();
    }
  });

  document.addEventListener('click', (event) => {
    if (!search.contains(event.target)) {
      closeResults();
    }
  });
})();

