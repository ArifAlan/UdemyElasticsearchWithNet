using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ElasticSearch.WEB.Models;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;

namespace ElasticSearch.WEB.Repository
{
	public class BlogRepository
	{
		private readonly ElasticsearchClient _client;
		private const string indexName = "blog";
		public BlogRepository(ElasticsearchClient client)
		{
			_client = client;
		}

		public async Task<Blog?> SaveAsync(Blog newBlog)
		{
			newBlog.Created = DateTime.Now;

			var response = await _client.IndexAsync(newBlog, x => x.Index(indexName));


			if (!response.IsValidResponse) return null;

			newBlog.Id = response.Id;

			return newBlog;


		}
		public async Task<List<Blog>> SearchAsync(string searchText)
		{
			//	Action<QueryDescriptor<Blog>>, Elasticsearch sorgularını oluşturmak için kullanılan bir delegedir.Elasticsearch.NET kütüphanesinde sorgu oluşturma işlemi için QueryDescriptor<T> sınıfı kullanılır.Bu sınıf, Elasticsearch sorgularını temsil eden nesnelerin oluşturulmasına olanak tanır.

			//Action<QueryDescriptor<Blog>> delegesi, QueryDescriptor< Blog > türünden bir nesne alıp işlem yapabilen bir metodu temsil eder.Blog yerine Elasticsearch'te indekslenen belirli bir belge türü (document type) yer alır.
			List<Action<QueryDescriptor<Blog>>> ListQuery = new();


			Action<QueryDescriptor<Blog>> matchAll = (q) => q.MatchAll();

			Action<QueryDescriptor<Blog>> matchContent = (q) => q.Match(m => m
				.Field(f => f.Content)
				.Query(searchText));


			Action<QueryDescriptor<Blog>> titleMatchBoolPrefix = (q) => q.MatchBoolPrefix(m => m
				.Field(f => f.Content)
				.Query(searchText));


			Action<QueryDescriptor<Blog>> tagTerm = (q) => q.Term(t => t.Field(f => f.Tags).Value(searchText));


			if (string.IsNullOrEmpty(searchText))
			{
				ListQuery.Add(matchAll);
			}

			else
			{

				ListQuery.Add(matchContent);
				ListQuery.Add(titleMatchBoolPrefix);
				ListQuery.Add(tagTerm);
			}



			//var result = await _client.SearchAsync<Blog>(s => s.Index(indexName)
			//             .Size(1000).Query(q => q
			//                 .Bool(b => b
			//                     .Should(s => s
			//                         .Match(m => m
			//                             .Field(f => f.Content)
			//                             .Query(searchText)), //should içinde virgül ile yazınca or oluyor
			//                         s=>s.MatchBoolPrefix(p => p
			//                             .Field(f => f.Title)
			//                             .Query(searchText))))));

			var result = await _client.SearchAsync<Blog>(s => s.Index(indexName)
				.Size(1000).Query(q => q
					.Bool(b => b
						.Should(ListQuery.ToArray()))));





			foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
			return result.Documents.ToList();





		}
	}
}
