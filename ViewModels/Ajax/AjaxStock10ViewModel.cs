using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ERP6.ViewModels.Ajax
{
    public class AjaxStock10ViewModel
    {
        /// <summary>
        /// 編號
        /// </summary>
        public string PartNo { get; set; }

        /// <summary>
        /// Barcode
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        public string Spec { get; set; }

        /// <summary>
        /// 規格
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public double? initQty2 { get; set; }

        /// <summary>
        /// 廠牌
        /// </summary>
        public string Atti { get; set; }

        /// <summary>
        /// 單價(批發價)
        /// </summary>
        public double? Price1 { get; set; }

        /// <summary>
        /// 牌價(建議售價一)
        /// </summary>
        public double? SPrice1 { get; set; }

        /// <summary>
        /// 促銷價
        /// </summary>
        public double? PPrice1 { get; set; }

        /// <summary>
        /// 入數
        /// </summary>
        public double? PackQty { get; set; }

        /// <summary>
        /// 門市價
        /// </summary>
        public double? Price2 { get; set; }

        /// <summary>
        /// 公教價
        /// </summary>
        public double? Price3 { get; set; }

        /// <summary>
        /// 建議售價二
        /// </summary>
        public double? SPrice2 { get; set; }

        /// <summary>
        /// 建議售價三
        /// </summary>
        public double? SPrice3 { get; set; }

        /// <summary>
        /// 建議售價四
        /// </summary>
        public double? SPrice4 { get; set; }

        /// <summary>
        /// 建議售價五
        /// </summary>
        public double? SPrice5 { get; set; }
    }
}
