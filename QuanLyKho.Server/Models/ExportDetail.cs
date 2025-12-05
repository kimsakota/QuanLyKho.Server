using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKho.Server.Models
{
    public partial class ExportDetail : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ExportId { get; set; }

        [NotMapped]
        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private decimal unitPrice;

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(ExportId))]
        public Export? Export { get; set; }

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;

        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(TotalPrice));
        partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalPrice));

    }
}
