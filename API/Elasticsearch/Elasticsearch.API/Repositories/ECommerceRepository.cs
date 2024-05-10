using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elasticsearch.API.Models.ECommerceModel;
using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Elasticsearch.API.Repositories
{
    public class ECommerceRepository
    {
        private readonly ElasticsearchClient _client;
        private const string indexName = "kibana_sample_data_ecommerce";
        public ECommerceRepository(ElasticsearchClient client)
        {
            _client = client;
        }
        public async Task<ImmutableList<ECommerce>> TermQuery(string customerFirstName)
        {
            //1. way
            //var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName).Query(q => q.Term(t => t.Field("customer_first_name.keyword").Value(customerFirstName))));
            //foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            //return result.Documents.ToImmutableList();


            //2.way tip güvenli şekilde yazıldı
            //var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName).Query(q=>q.Term(t=>t.CustomerFirstName.Suffix("keyword"),customerFirstName)));

            //3.way
            var termQuery=new TermQuery("customer_first_name.keyword") { Value= customerFirstName, CaseInsensitive=true };

            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName).Query(q => q.Term(termQuery)));
            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();
        }
        //termquery de bir fieldda birden fazla değer verilebilir
        public async Task<ImmutableList<ECommerce>> TermsQuery(List<string> customerFirstNameList)
        {
            List<FieldValue> terms = new List<FieldValue>();
            customerFirstNameList.ForEach(x =>
            {
                terms.Add(x);
            });
            //1.way
            //var termsQuery = new TermsQuery()
            //{
            //    Field = "customer_first_name.keyword",
            //    Terms = new TermsQueryField(terms.AsReadOnly())
            //};

            //var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName).Query(termsQuery));

            // 2. way
            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
            .Size(100)
            .Query(q => q
            .Terms(t => t
            .Field(f => f.CustomerFirstName
            .Suffix("keyword"))
            .Terms(new TermsQueryField(terms.AsReadOnly())))));



            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        //Prefix sorgusu, belirli bir önek veya başlangıç metnine sahip olan belgeleri bulmak için kullanılan bir sorgu türüdür.
        //Suffix sorgusu, belirli bir sonek veya bitiş metnine sahip olan belgeleri bulmak için kullanılan bir sorgu türüdür.
        public async Task<ImmutableList<ECommerce>> PrefixQueryAsync(string CustomerFullName)
        {
            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Query(q => q
                    .Prefix(p => p
                        .Field(f => f.CustomerFullName
                            .Suffix("keyword"))
                                .Value(CustomerFullName))));



            return result.Documents.ToImmutableList();

        }

        // Range sorgusu, belirli bir değer aralığına sahip belgeleri bulmak için kullanılan bir sorgu türüdür.NumberRange sayısal değer aralıkta arama yaparken kullanırsın.DateRange tarihsel aralıkta arama yaparken kullanırsın.gte=> grater than or equals , lte=>less than or equals 
        public async Task<ImmutableList<ECommerce>> RangeQueryAsync(double FromPrice, double ToPrice)
        {
            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName).Size(20)
                .Query(q => q
                    .Range(r => r
                        .NumberRange(nr => nr
                            .Field(f => f.TaxfulTotalPrice)
                                .Gte(FromPrice).Lte(ToPrice)))));



            return result.Documents.ToImmutableList();

        }
      
        public async Task<ImmutableList<ECommerce>> MatchAllQueryAsync()
        {

            //var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
            //    .Size(100)
            //    .Query(q => q.MatchAll()));


            var result = await _client.SearchAsync<ECommerce>(s =>
                s.Index(indexName).Size(1000).Query(q => q.Match(m => m.Field(f => f.CustomerFullName).Query("shaw"))));



            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        public async Task<ImmutableList<ECommerce>> PaginationQueryAsync(int page, int pageSize)
        {

            // page=1, pageSize=10 =>  1-10
            // page=2 , pageSize=10=> 11-20
            // page=3, pageSize=10 => 21-30


            var pageFrom = (page - 1) * pageSize;


            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Size(pageSize).From(pageFrom)
                .Query(q => q.MatchAll()));


            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        // Wilcard sorgusu, joker karakterler kullanarak eşleşen belgeleri bulmak için kullanılan bir sorgu türüdür.
        public async Task<ImmutableList<ECommerce>> WildCardQueryAsync(string customerFullName)
        {



            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)     
            .Query(q => q.Wildcard(w =>
                    w.Field(f => f.CustomerFullName
                            .Suffix("keyword"))
                                .Wildcard(customerFullName))));


            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        //Fuzzy sorgusu, benzer ancak tam olarak eşleşmeyen terimleri içeren belgeleri bulmak için kullanılan bir sorgu türüdür.
        public async Task<ImmutableList<ECommerce>> FuzzyQueryAsync(string customerName)
        {

            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Query(q => q.Fuzzy(fu =>
                    fu.Field(f => f.CustomerFirstName.Suffix("keyword")).Value(customerName)
                        .Fuzziness(new Fuzziness(2))))
                            .Sort(sort => sort
                                .Field(f => f.TaxfulTotalPrice, new FieldSort() { Order = SortOrder.Desc })));

            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }

        //Match query full text , tam metin aramaları için kullanılan ve belirli bir terimin bir veya daha fazla alan içinde eşleşen belgeleri bulmak için kullanılan bir sorgu türüdür.
        public async Task<ImmutableList<ECommerce>> MatchQueryFullTextAsync(string categoryName)
        {


            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Size(1000).Query(q => q
                    .Match(m => m
                        .Field(f => f.Category)
                        .Query(categoryName).Operator(Operator.And))));

            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        //Match boolean prefix, bir dizi boolean değeri içeren bir alana eşleşen belgeleri bulmak için kullanılan ve belirli bir öneki içeren terimlerle eşleşen belgeleri döndüren bir Elasticsearch sorgu türüdür.

        public async Task<ImmutableList<ECommerce>> MatchBoolPrefixFullTextAsync(string customerFullName)
        {

            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Size(1000).Query(q => q
                    .MatchBoolPrefix(m => m
                        .Field(f => f.CustomerFullName)
                        .Query(customerFullName))));

            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        // MatchPhraseQueryFullText, tam metin aramalarında bir terimin tam bir önermedeki özel bir konumunu eşleştirmek için kullanılan bir Elasticsearch sorgu türüdür.
        public async Task<ImmutableList<ECommerce>> MatchPhraseFullTextAsync(string customerFullName)
        {

            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Size(1000).Query(q => q
                    .MatchPhrase(m => m
                        .Field(f => f.CustomerFullName)
                        .Query(customerFullName))));

            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        //Compound query, birden fazla sorgu türünü birleştirerek daha karmaşık sorgular oluşturmak için kullanılan bir Elasticsearch sorgu türüdür.
        public async Task<ImmutableList<ECommerce>> CompoundQueryExampleOneAsync(string cityName, double taxfulTotalPrice, string categoryName, string menufacturer)
        {

            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Size(1000).Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .Term(t => t
                                .Field("geoip.city_name")
                                .Value(cityName)))
                        .MustNot(mn => mn
                            .Range(r => r
                                .NumberRange(nr => nr
                                    .Field(f => f.TaxfulTotalPrice)
                                    .Lte(taxfulTotalPrice))))
                        .Should(s => s.Term(t => t
                            .Field(f => f.Category.Suffix("keyword"))
                            .Value(categoryName)))
                        .Filter(f => f
                            .Term(t => t
                                .Field("manufacturer.keyword")
                                .Value(menufacturer))))


                ));

            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        public async Task<ImmutableList<ECommerce>> CompoundQueryExampleTwoAsync(string customerFullName)
        {
            //var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName).Size(1000).Query(q => q.Bool(b => b.Must(m => m.Match(m => m.Field(f => f.CustomerFullName).Query(customerFullName))))));

            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Size(1000).Query(q => q.MatchPhrasePrefix(m => m.Field(f => f.CustomerFullName).Query(customerFullName))));


            //var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
            //    .Size(1000).Query(q => q
            //        .Bool(b => b
            //            .Should(m => m
            //                .Match(m => m
            //                    .Field(f => f.CustomerFullName)
            //                    .Query(customerFullName))
            //                .Prefix(p => p
            //                    .Field(f => f.CustomerFullName.Suffix("keyword"))
            //                    .Value(customerFullName))))));





            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }
        public async Task<ImmutableList<ECommerce>> MultiMatchQueryFullTextAsync(string name)
        {


            var result = await _client.SearchAsync<ECommerce>(s => s.Index(indexName)
                .Size(1000).Query(q => q
                    .MultiMatch(mm =>
                        mm.Fields(new Field("customer_first_name")
                            .And(new Field("customer_last_name"))
                            .And(new Field("customer_full_name")))
                            .Query(name))));

            foreach (var hit in result.Hits) hit.Source.Id = hit.Id;
            return result.Documents.ToImmutableList();

        }

    }
}
