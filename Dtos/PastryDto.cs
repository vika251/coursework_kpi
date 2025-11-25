namespace ConfectioneryApi.Dtos
{
    /// DTO для представлення кондитерського виробу у відповідях API.
    public class PastryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}