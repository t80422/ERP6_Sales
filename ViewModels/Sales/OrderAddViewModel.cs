using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP6.ViewModels.Sales
{
    public class OrderAddViewModel
    {
        public string OutNo { get; set; }
        public string Area { get; set; }
        public string CoNo { get; set; }
        public string CoName { get; set; }
        public string Memo { get; set; }
        public List<string> StockTypeList { get; set; }
        public List<StockList> StockList { get; set; }
        public string OutDate { get; set; }        
        public string PayMonth { get; set; }
        public string KeyInDate { get; set; }
        public string Ntus { get; set; }
        public string Userid { get; set; }
        public string PeNo { get; set; }
        public string DriveNo { get; set; }
        public double? Discount { get; set; }
        public string TallyState { get; set; }


    }

    public class StockList
    {
        public string Type { get; set; }
        public bool IsSales { get; set; }
        public bool IsStats { get; set; }
        public string PartNo { get; set; }
        public string Spec { get; set; }
        public double? Price { get; set; }
        public double? Qty { get; set; }
        public double? IQty { get; set; }
        public string Memo { get; set; }
        
    }
}
