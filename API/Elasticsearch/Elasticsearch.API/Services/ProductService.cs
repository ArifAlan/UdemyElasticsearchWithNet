using Elasticsearch.API.DTOs;
using Elasticsearch.API.Repositories;
using System.Net;

namespace Elasticsearch.API.Services
{
    public class ProductService
    {
        private readonly ProductRepository _productRepository;
        private readonly ILogger<ProductService> _logger;
        public ProductService(ProductRepository productRepository, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<ResponseDto<ProductDto>> SaveAsync(ProductCreateDto request)
        {




            var responseProduct = await _productRepository.SaveAsync(request.CreateProduct());
            if (responseProduct == null)
            {
                return ResponseDto<ProductDto>.Fail(new List<string> { "kayıt esnasında bir hata meydana geldi." }, System.Net.HttpStatusCode.InternalServerError);
            }



            return ResponseDto<ProductDto>.Success(responseProduct.CreateDto(), HttpStatusCode.Created);




        }

    }
}
