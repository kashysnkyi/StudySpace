using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace StudySpace.Web.Services;

public sealed record SearchDocument(Guid Id, string Title, string Summary, string Workspace);
public sealed record SearchHit(Guid Id, string Title, string Workspace, float Score);

public sealed class DocumentSearchService
{
    private static readonly LuceneVersion Version = LuceneVersion.LUCENE_48;

    public IReadOnlyList<SearchHit> Search(IReadOnlyCollection<SearchDocument> source, string term, int limit = 20)
    {
        using var directory = new RAMDirectory();
        using var analyzer = new StandardAnalyzer(Version);
        var config = new IndexWriterConfig(Version, analyzer);
        using (var writer = new IndexWriter(directory, config))
        {
            foreach (var item in source)
            {
                writer.AddDocument(new Lucene.Net.Documents.Document
                {
                    new StringField("id", item.Id.ToString(), Field.Store.YES),
                    new TextField("title", item.Title, Field.Store.YES),
                    new TextField("summary", item.Summary, Field.Store.NO),
                    new TextField("workspace", item.Workspace, Field.Store.YES)
                });
            }
            writer.Commit();
        }

        using var reader = DirectoryReader.Open(directory);
        var searcher = new IndexSearcher(reader);
        var parser = new MultiFieldQueryParser(Version, ["title", "summary", "workspace"], analyzer);
        var query = parser.Parse(QueryParserBase.Escape(term.Trim()) + "*");
        return searcher.Search(query, Math.Clamp(limit, 1, 100)).ScoreDocs.Select(hit =>
        {
            var document = searcher.Doc(hit.Doc);
            return new SearchHit(Guid.Parse(document.Get("id")), document.Get("title"), document.Get("workspace"), hit.Score);
        }).ToList();
    }
}
