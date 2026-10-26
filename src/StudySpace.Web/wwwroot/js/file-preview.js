(() => {
  const humanSize = bytes => bytes < 1024 * 1024
    ? `${Math.ceil(bytes / 1024)} KB`
    : `${(bytes / 1024 / 1024).toFixed(1)} MB`;

  const wireUpload = form => {
    const zone = form.querySelector('[data-file-drop-zone]');
    const input = form.querySelector('input[type="file"]');
    const preview = form.querySelector('[data-file-preview]');
    const submit = form.querySelector('.upload-submit');
    if (!zone || !input || !preview || !submit) return;

    const showFile = file => {
    preview.replaceChildren();
    preview.hidden = false;
    submit.disabled = false;

    const details = document.createElement('div');
    details.innerHTML = `<strong></strong><span></span>`;
    details.querySelector('strong').textContent = file.name;
    details.querySelector('span').textContent = `${file.type || 'Невідомий тип'} · ${humanSize(file.size)}`;
    preview.append(details);

    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.addEventListener('load', () => {
        const image = document.createElement('img');
        image.src = reader.result;
        image.alt = `Локальний попередній перегляд файлу ${file.name}`;
        preview.prepend(image);
      });
      reader.readAsDataURL(file);
    }
    };

    input.addEventListener('change', () => {
      if (input.files?.[0]) showFile(input.files[0]);
    });
    ['dragenter', 'dragover'].forEach(name => zone.addEventListener(name, event => {
      event.preventDefault();
      zone.classList.add('drag-over');
    }));
    ['dragleave', 'drop'].forEach(name => zone.addEventListener(name, event => {
      event.preventDefault();
      zone.classList.remove('drag-over');
    }));
    zone.addEventListener('drop', event => {
      const file = event.dataTransfer?.files?.[0];
      if (!file) return;
      const transfer = new DataTransfer();
      transfer.items.add(file);
      input.files = transfer.files;
      showFile(file);
    });
    form.addEventListener('submit', () => {
      submit.disabled = true;
      submit.textContent = 'Завантаження…';
    });
  };

  document.querySelectorAll('.file-drop-form').forEach(wireUpload);
})();

