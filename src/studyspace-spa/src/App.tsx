import { FormEvent, useEffect, useState } from "react";

type Status = "Draft" | "Published" | "Archived";
type DocumentItem = { id: string; workspaceId: string; workspaceName: string; title: string; summary: string; status: Status; updatedAt: string };
type Page = { items: DocumentItem[]; total: number; nextLink?: string };

const api = import.meta.env.VITE_API_URL ?? "";

export default function App() {
  const [documents, setDocuments] = useState<DocumentItem[]>([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<DocumentItem | null>(null);

  async function load(search = query) {
    setLoading(true); setError("");
    try {
      const response = await fetch(`${api}/api/documents?limit=100&query=${encodeURIComponent(search)}`, { credentials: "include" });
      if (!response.ok) throw new Error(`API returned ${response.status}`);
      const page: Page = await response.json();
      setDocuments(page.items);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Не вдалося завантажити документи"); }
    finally { setLoading(false); }
  }

  useEffect(() => { void load(""); }, []);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editing) return;
    const response = await fetch(`${api}/api/documents/${editing.id}`, {
      method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ workspaceId: editing.workspaceId, title: editing.title, summary: editing.summary, status: editing.status })
    });
    if (!response.ok) { setError(`Не вдалося зберегти зміни (${response.status})`); return; }
    setEditing(null); await load();
  }

  return <div className="app-shell">
    <header><a className="brand" href="/">S <span>StudySpace</span></a><span className="chip">React SPA | REST API</span></header>
    <main>
      <section className="hero"><p className="eyebrow">ЦЕНТР РОБОТИ З ДОКУМЕНТАМИ</p><h1>Ваші знання,<br/><em>у русі.</em></h1><p>Шукайте, переглядайте та оновлюйте документи через той самий API, який використовує MVC-застосунок.</p></section>
      <form className="search" onSubmit={event => { event.preventDefault(); void load(); }}><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Пошук за назвою або описом" /><button>Шукати</button></form>
      <div className="section-title"><h2>Документи</h2><span>{documents.length} з {documents.length || 0}</span></div>
      {loading && <p className="notice">Завантаження простору…</p>}
      {error && <p className="notice error">{error}. Переконайтеся, що StudySpace.Web працює на порту 5036.</p>}
      <div className="grid">{documents.map(document => <article key={document.id}>
        <div><span className={`status ${document.status.toLowerCase()}`}>{document.status}</span><span className="workspace">{document.workspaceName}</span></div>
        <h3>{document.title}</h3><p>{document.summary}</p>
        <footer><time>{new Date(document.updatedAt).toLocaleDateString("uk-UA")}</time><button onClick={() => setEditing(document)}>Редагувати</button></footer>
      </article>)}</div>
    </main>
    {editing && <div className="modal-backdrop" onMouseDown={() => setEditing(null)}><form className="modal" onSubmit={save} onMouseDown={e => e.stopPropagation()}>
      <p className="eyebrow">РЕДАГУВАННЯ ДОКУМЕНТА</p><h2>{editing.title}</h2>
      <label>Назва<input value={editing.title} onChange={e => setEditing({ ...editing, title: e.target.value })} required /></label>
      <label>Короткий опис<textarea value={editing.summary} onChange={e => setEditing({ ...editing, summary: e.target.value })} required /></label>
      <label>Статус<select value={editing.status} onChange={e => setEditing({ ...editing, status: e.target.value as Status })}><option value="Draft">Чернетка</option><option value="Published">Опубліковано</option><option value="Archived">Архів</option></select></label>
      <div className="actions"><button type="button" className="secondary" onClick={() => setEditing(null)}>Скасувати</button><button>Зберегти зміни</button></div>
    </form></div>}
  </div>;
}
