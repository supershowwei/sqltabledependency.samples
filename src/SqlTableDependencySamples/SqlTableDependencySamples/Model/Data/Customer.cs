using System.ComponentModel.DataAnnotations.Schema;

namespace SqlTableDependencySamples.Model.Data
{
    [Table("MyDependencyTable")]
    public class Customer
    {
        public string Address { get; set; }

        [Column("Description")]
        public string Detail { get; set; }

        public string Name { get; set; }

        [Column("Id")]
        public int No { get; set; }
    }
}