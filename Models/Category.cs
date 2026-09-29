namespace tieudaobadminton.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";   // Vợt, Giày, Quần áo
        public string Slug { get; set; } = "";   // vot, giay, quan-ao

        public List<Product> Products { get; set; } = new();
    }

}
