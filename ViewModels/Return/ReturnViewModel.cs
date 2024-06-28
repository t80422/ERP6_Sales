using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using X.PagedList;

namespace ERP6.ViewModels.Return
{
    public class ReturnViewModel
    {
        // 查詢
        public bool IsSearch { get; set; }

        // 入庫狀態
        public string ReturnState { get; set; }

        // 退貨單編號
        public string ReturnNo { get; set; }

        // 退貨單時間
        public string ReturnTime { get; set; }

        // 司機
        public string DriverNo { get; set; }

        // 業務
        public string PerNo { get; set; }

        // 區域
        public string AreaNo { get; set; }

        // 客戶名稱
        public string CoNo { get; set; }

        // 退貨單列表
        public IPagedList<ReturnList> ReturnList {get;set;}

        // 已入庫數量
        public int Passed { get; set; }

        // 未入庫數量
        public int NotPassed { get; set; }

        // 是否全部入庫
        public bool AllPassed { get; set; }

    }

    public class ReturnList
    {
        // 入庫狀態
        public bool ReturnState { get; set; }

        // 退貨單編號
        public string ReturnNo { get; set; }

        // 退貨單時間
        public string ReturnTime { get; set; }

        // 退貨單金額
        public double? Amount { get; set; }

        // 客戶名稱
        public string CoName { get; set; }
    }
}
