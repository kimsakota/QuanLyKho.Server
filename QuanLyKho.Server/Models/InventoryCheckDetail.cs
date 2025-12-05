using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKho.API.Models
{
    public partial class InventoryCheckDetail : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        public int InventoryCheckId { get; set; }

        public int ProductId { get; set; }

        public int SystemQty { get; set; } // Tồn kho trên phần mềm lúc kiểm

        [ObservableProperty]
        private int actualQty;

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(InventoryCheckId))]
        public InventoryCheck? InventoryCheck { get; set; }

        [NotMapped]
        public int Diff => ActualQty - SystemQty;

        partial void OnActualQtyChanged(int value) => OnPropertyChanged(nameof(Diff));
    }
}
