using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP6.ViewModels.Return
{
    public class ReturnAddViewModel
    {
        // 客戶
        public string CoNo { get; set; }
        // 退回單編號
        public string ReturnNo { get; set; }
        // 退回日期
        public string ReturnTime { get; set; }
        // 負責人
        public string UserId { get; set; }
        // 異動者
        public string EditId { get; set; }
        // 入庫者
        public string PassedId { get; set; }
        // 備註
        public string Memo { get; set; }
        // 清單
        public List<StockList> StockList { get; set; }
        // 是否入庫
        public bool PASSED { get; set; }
    }
    public class StockList
    {
        // 產品編號
        public string PartNo { get; set; }
        // 產品名稱
        public string PartName { get; set; }
        // 價格
        public double? Price { get; set; }
        // 折扣
        public double? Discount { get; set; }
        // 數量
        public int? Amount { get; set; }
        // 破包數量
        public int? BAmount { get; set; }
        // 過期數量
        public int? EAmount { get; set; }
        // 良品數量
        public int? PAmount { get; set; }
        // 其他數量
        public int? OAmount { get; set; }
        // 備註
        public string PMemo { get; set; }
    }
}
