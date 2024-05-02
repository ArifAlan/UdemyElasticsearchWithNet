using Elasticsearch.API.Models;
using System.Drawing;

namespace Elasticsearch.API.DTOs
{
    public record ProductFeatureDto(int Width, int Height, EColor Color)
    {
    }
}
