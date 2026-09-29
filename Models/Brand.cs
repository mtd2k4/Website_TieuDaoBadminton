namespace tieudaobadminton.Models
{
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; } = ""; // Yonex, Victor, Lining

        public List<Product> Products { get; set; } = new();
    }

}
