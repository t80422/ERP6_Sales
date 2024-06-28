using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP6.Models;
using Microsoft.AspNetCore.Http;
using ERP6.ViewModels.Ajax;
using NETCore.Encrypt;
using BarcodeLib;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using ERP6.ViewModels.Sales;

namespace ERP6.Controllers
{
    public class AjaxsController : Controller
    {
        private readonly EEPEF01Context _context;
        public AjaxsController(EEPEF01Context context)
        {
            _context = context;
        }

        public class StockCombobox
        {
            public string id { get; set; }
            public string text { get; set; }
            public double Price { get; set; }
            public double discount { get; set; }
        }

        public JsonResult AjaxAllStockForCombobox(string CoNo)
        {
            
            var stockType = _context.Stock_Type.Where(x => x.TYPE_ISOPEN).OrderBy(x => x.TYPE_ORDER).Select(x => x.TYPE_NAME).ToList();
            var stocks = _context.Stock10.Where(x => stockType.Contains(x.Type) && (bool)x.IsOpen).Select(x => new StockCombobox {
                id = x.PartNo , 
                text = x.PartNo + x.Spec + x.Barcode,
                Price = 0,
                discount = 0,
            }).ToList();
            // 所有產品PartNo
            //var AllPartNo = stocks.Select(x => x.id).Distinct().ToList();
            // 客戶的所有出貨單
            var AllOut = _context.Out10.Where(x => x.CoNo == CoNo && x.OutType == "1").Select(x => x.OutNo).ToList();
            // 所有成功出貨的明細
            var AllOut20 = _context.Out20.Where(x => AllOut.Contains(x.OutNo)).ToList();
            try
            {
                foreach (var stock in stocks)
                {
                    var LastOutPrice = AllOut20.Where(x => AllOut.Contains(x.OutNo) && x.PartNo == stock.id).OrderByDescending(x => x.OutNo).FirstOrDefault();
                    stock.Price = LastOutPrice != null ? (double)(LastOutPrice.Price ?? 0) : 0;
                    stock.discount = LastOutPrice != null ? (double)(LastOutPrice.Discount ?? 0) : 0;
                }
            }catch(Exception e)
            {
                throw;
            }

            return Json(stocks);
        }

        public Dictionary<string,double> GetPartPrice(string CONO, List<string> PARTNO_LIST)
        {
            Stopwatch stopwatch = new Stopwatch();

            Dictionary<string, double> result = new Dictionary<string, double>();

            if (!String.IsNullOrEmpty(CONO) && PARTNO_LIST.Count() > 0)
            {
                //EEPEF01Context _context = new EEPEF01Context();

                // 找到客戶資訊
                var CSTM = _context.Cstm10.Find(CONO);

                // 今日日期
                var TODAY = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));

                //找出在促銷期間單號
                var SalesHistory_OUTNO = _context.Out10.Join(_context.Stock20, x => x.CoNo, y => y.CoNo, (x, y) => new
                {
                    OUTNO = x.OutNo,
                    CONO = x.CoNo,
                    OUTDATE = Convert.ToInt32(x.OutDate),
                    BDATE = Convert.ToInt32(y.Bdate),
                    EDATE = Convert.ToInt32(y.Edate),
                    OUTTYPE = x.OutType,
                }).Where(z => z.CONO == CONO && z.BDATE <= z.OUTDATE && z.EDATE >= z.OUTDATE && z.OUTTYPE == "1").Select(z => z.OUTNO).ToList();

                //找出不在促銷期間的所有單號
                var History_OUT20 = _context.Out20.Join(_context.Out10, x => x.OutNo, y => y.OutNo, (x, y) => new
                {
                    OUTNO = x.OutNo,
                    PARTNO = x.PartNo,
                    CONO = y.CoNo,
                    PRICE = x.Price,
                    OUTTYPE = y.OutType,
                }).Where(z => z.CONO == CONO && !SalesHistory_OUTNO.Contains(z.OUTNO) && z.OUTTYPE == "1").ToList();

                // 找到在促銷期間的促銷資料
                var STOCK20_LIST = _context.Stock20.Join(_context.Stock21, x => x.SpNo, y => y.SpNo, (x, y) => new
                {
                    SPNO = x.SpNo,
                    CONO = x.CoNo,
                    BDATE = Convert.ToInt32(x.Bdate),
                    EDATE = Convert.ToInt32(x.Edate),
                    PARTNO = y.PartNo,
                    PRICE = y.Newprice,
                }).Where(z => z.CONO == CONO && TODAY >= z.BDATE && TODAY <= z.EDATE).ToList();

                // 先獲取比歷史的報價單
                var LASTBOU = _context.Bou10.Join(_context.Bou20, x => x.QuNo, y => y.QuNo, (x, y) => new
                {
                    QUNO = x.QuNo,
                    PARTNO = y.PartNo,
                    CONO = x.CoNo,
                    QUDATE = Convert.ToInt32(x.QuDate),
                    SENDDATE = Convert.ToInt32(x.SendDate),
                    PRICE = y.Price
                }).Where(z => z.CONO == CONO  && z.SENDDATE <= TODAY).ToList();


                // 牌價 < 不在促銷期間的歷史價格(約定價格) < 報價單(如果建立時間較歷史新才優先於歷史) < 促銷價

                foreach (var PARTNO in PARTNO_LIST)
                {

                    // 1. 促銷價
                    #region 促銷價

                    // 找到在促銷期間並且有此產品的促銷資料
                    var STOCK20 = STOCK20_LIST.Where(z => z.PARTNO == PARTNO).OrderByDescending(z => z.SPNO).FirstOrDefault();
                    //var STOCK20 = STOCK20_LIST.ContainsKey(PARTNO)
                    #endregion 促銷價

                    // 加入價格
                    if (STOCK20 != null) { result.Add(PARTNO, Convert.ToDouble(STOCK20.PRICE)); continue; }

                    // 2. 報價單 - 先獲得 不在促銷期間的歷史價格(約定價格)
                    #region 不在促銷期間的歷史價格(約定價格)

                    //stopwatch.Start();


                    //找出不在促銷期間的並且有此產品的最新單號和價格
                    var HISTORY_PART = History_OUT20.Where(x => x.PARTNO == PARTNO).OrderByDescending(x => x.OUTNO).FirstOrDefault();
                    //var HISTORY_PART = History_OUT20.ContainsKey(PARTNO) ? History_OUT20[PARTNO].OrderByDescending(x=>x.)
                    //var History_OUT20 = _context.Out20.Join(_context.Out10, x => x.OutNo, y => y.OutNo, (x, y) => new
                    //{
                    //    OUTNO = x.OutNo,
                    //    PARTNO = x.PartNo,
                    //    CONO = y.CoNo,
                    //    PRICE = x.Price,
                    //    OUTTYPE = y.OutType,
                    //}).Where(z => z.CONO == CONO && z.PARTNO == PARTNO && !SalesHistory_OUTNO.Contains(z.OUTNO) && z.OUTTYPE == "1").OrderByDescending(z => z.OUTNO).FirstOrDefault();

                    //stopwatch.Stop();

                    #endregion 不在促銷期間的歷史價格(約定價格)

                    // 3. 報價單(如果建立時間較歷史新才優先於歷史)
                    #region 報價單(如果建立時間較歷史新才優先於歷史)

                    // 獲取歷史訂單的建立時間
                    var HISTORY_OUTTIME = Convert.ToInt32(_context.Out10.Find(HISTORY_PART?.OUTNO)?.OutDate);
                    // 先獲取比歷史訂單新且有此產品的報價單
                    var BOUDATA = LASTBOU.Where(z => z.PARTNO == PARTNO && z.QUDATE >= HISTORY_OUTTIME).OrderByDescending(x => x.QUNO).FirstOrDefault();

                    #endregion 報價單(如果建立時間較歷史新才優先於歷史)

                    // 加入價格 如果有新的報價單先加入
                    if (BOUDATA != null) { result.Add(PARTNO, Convert.ToDouble(BOUDATA.PRICE)); continue; }
                    if (HISTORY_PART != null) { result.Add(PARTNO, Convert.ToDouble(HISTORY_PART.PRICE)); continue; }

                    // 4. 牌價
                    #region 牌價

                    // 找到產品資訊
                    var STOCK = _context.Stock10.Find(PARTNO);
                    double? RESULT_PRICE = 0;

                    var CSTM_PRICETYPE = CSTM?.PriceType;
                    var STOCKCOL = _context.PRICE_TYPE.Where(x => x.PT_VALUE == CSTM_PRICETYPE).FirstOrDefault()?.PT_STOCKCOL;

                    switch (STOCKCOL)
                    {
                        case "Price1":
                            RESULT_PRICE = STOCK?.Price1;
                            break;
                        case "Price2":
                            RESULT_PRICE = STOCK?.Price2;
                            break;
                        case "Price3":
                            RESULT_PRICE = STOCK?.Price3;
                            break;
                        case "DefaultPrice1":
                            RESULT_PRICE = STOCK?.DefaultPrice1;
                            break;
                        case "DefaultPrice2":
                            RESULT_PRICE = STOCK?.DefaultPrice2;
                            break;
                        case "DefaultPrice3":
                            RESULT_PRICE = STOCK?.DefaultPrice3;
                            break;
                        case "DefaultPrice4":
                            RESULT_PRICE = STOCK?.DefaultPrice4;
                            break;
                        case "DefaultPrice5":
                            RESULT_PRICE = STOCK?.DefaultPrice5;
                            break;
                        default:
                            RESULT_PRICE = 0;
                            break;
                    }

                    #endregion 牌價
                    result.Add(PARTNO, Convert.ToDouble(RESULT_PRICE));

                }
            }
            return result;

        }

        // GET: Ajaxs
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult AjaxGetLast(string CustomerId)
        {
            try
            {
                CustomerId = CustomerId ?? "";

                // 建立模型
                var vm = new OrderEditViewModel();

                // 找出客戶
                var CSTM = _context.Cstm10.Find(CustomerId);

                // 上筆歷史資料
                var LAST_OUT = _context.Out10.Where(x => x.OutType == "1" && x.CoNo == CustomerId).OrderByDescending(x => x.OutNo).FirstOrDefault();

                // 上筆訂單的產品
                //var LAST_STOCK = LAST_OUT != null ? _context.Out20.Where(x => x.OutNo == LAST_OUT.OutNo).Select(x => x.PartNo).Distinct().ToList() : new List<string>();
                var LAST_STOCK = _context.Out20.Join(_context.Out10.Where(x => x.CoNo == CustomerId && x.OutType == "1")
                    , x => x.OutNo, y => y.OutNo, (x, y) => new { PartNo = x.PartNo, Qty = x.Qty }).Where(x => x.Qty > 0).Select(x => x.PartNo).Distinct().ToList();

                // 帶入資料
                vm.OutNo = "";
                vm.CoName = CSTM?.CoNo;
                vm.OrderState = "1"; // 預設為1:出貨
                vm.OrderTime = "";
                vm.Area = CSTM?.AreaNo;
                vm.CustomerId = CustomerId ?? "";
                vm.Memo = LAST_OUT != null ? LAST_OUT.Memo : "";
                vm.Business = "";
                vm.Driver = "";
                vm.IsCopy = "";

                // 所有的產品分類
                var Types = _context.Stock_Type.Where(x => x.TYPE_ISOPEN).OrderBy(x => x.TYPE_ORDER).Select(x => x.TYPE_NAME).ToList();

                // 所有分類裡的產品 ( 顯示的主體 ) 
                var ALLSTOCKLIST = _context.Stock10.Where(x => Types.Contains(x.Type) && (bool)x.IsOpen).ToList();

                // 所有產品的價格
                var StockList_PRICE = GetPartPrice(CustomerId, ALLSTOCKLIST.Select(x => x.PartNo).ToList());

                // 顯示 - 產品資訊
                vm.StockList = ALLSTOCKLIST.Select(x => new StockInfo
                {
                    //IsPromise
                    PartNo = x.PartNo,
                    Spec = x.Spec,
                    //PackQty
                    IsSalesStatus = LAST_STOCK.Contains(x.PartNo),
                    StPs = "",
                    StockType = x.Type,
                    Price = !String.IsNullOrEmpty(CustomerId) ? StockList_PRICE[x.PartNo] : 0,
                    OriPrice = !String.IsNullOrEmpty(CustomerId) ? StockList_PRICE[x.PartNo] : 0,
                    IsPromotion = false,
                }).ToList();

                var LASTOUT = _context.Out10.Where(x => x.CoNo == CustomerId && x.OutType == "1").Select(x => x.OutNo).ToList();
                var OUTSTOCK = _context.Out20.Where(x => LASTOUT.Contains(x.OutNo)).ToList();

                foreach (var stock in vm.StockList)
                {
                    var stockdetail = OUTSTOCK.Where(x => x.PartNo == stock.PartNo).OrderByDescending(x => x.OutNo).FirstOrDefault();
                    stock.PackQty = stockdetail != null ? stockdetail?.Qty : stock.PackQty;
                    stock.IsPromise = stockdetail != null ? !String.IsNullOrEmpty(stockdetail?.IsPromise) : false;
                    stock.Price = stockdetail != null ? Convert.ToDouble(stockdetail?.Price) : stock.Price;
                    stock.StPs = stockdetail != null ? stockdetail?.Memo : stock.StPs;
                }

                // 顯示 - 產品分類
                vm.StockTitleList = Types.Select(x => new StockTitle { Name = x }).ToList();



                return Json(vm);
            }
            catch (Exception)
            {
                throw;
            }


        }

        public JsonResult AjaxClientName(string id)
        {
            var data = _context.Cstm10.Where(x => x.AreaNo == id).ToList();
            return Json(data);
        }

        public ActionResult AjaxClientAddr(string id)
        {
            var data = _context.Cstm10.Where(x => x.CoNo == id).ToList();
            return Json(data);
        }

        public ActionResult AjaxOrderDet(string id, string outNo, string coNo)
        {
            //客戶資料
            var coData = _context.Cstm10.Where(x => x.CoNo == coNo).FirstOrDefault();

            //產品資料
            var stock10Data = _context.Stock10.Where(x => x.PartNo == id).FirstOrDefault();

            //單價
            double? price = 0;

            //售價
            double? sprice = 0;

            // 修正報錯bug 20230424 by WeiRen

            if(coData != null)
            {
                switch (coData.PriceType)
                {
                    case "批發價":
                        price = stock10Data.Price1;
                        sprice = stock10Data.SPrice1;

                        break;
                    case "門市價":
                        price = stock10Data.Price2;
                        sprice = stock10Data.SPrice2;

                        break;
                    case "公教價":
                        price = stock10Data.Price3;
                        sprice = stock10Data.SPrice3;

                        break;
                    default:
                        break;
                }
            }


            AjaxStock10ViewModel data = new AjaxStock10ViewModel
            {
                PartNo = stock10Data.PartNo,
                Barcode = stock10Data.Barcode,
                Spec = stock10Data.Spec,
                Unit = stock10Data.Unit,
                initQty2 = stock10Data.InitQty2,
                Atti = stock10Data.Atti,
                Price1 = price ?? 0,
                SPrice1 = sprice ?? 0,
                PackQty = stock10Data.PackQty,
                Price2 = stock10Data.Price2,
                Price3 = stock10Data.Price3,
                SPrice2 = stock10Data.SPrice2,
                SPrice3 = stock10Data.SPrice3,
                SPrice4 = 0,
                SPrice5 = 0,
            };

            //取出出貨單確定是否有促銷資料
            var out10Data = _context.Out10.Where(x => x.OutNo == outNo).FirstOrDefault();

            if (out10Data != null)
            {
                if (!string.IsNullOrEmpty(out10Data.Promotion_No))
                {
                    var stock20Data = _context.Stock21
                        .Where(x => x.SpNo == out10Data.Promotion_No && x.PartNo == id).FirstOrDefault();

                    data.PPrice1 = stock20Data.Newprice ?? 0;
                }
                else
                {
                    data.PPrice1 = 0;
                }
            }
            else
            {
                data.PPrice1 = 0;
            }

            return Json(data);
        }

        //撈取Barcode並轉成圖片
        public ActionResult AjaxGetBarcode(string id)
        {
            var byteData = GetBarCode(id);
            var data = Convert.ToBase64String(byteData);

            return Json(data);
        }

        public static void GetBarCode(string Code, string path, TYPE type = TYPE.EAN13, int Length = 1000, int Height = 200, int FontSize = 40)
        {
            try
            {
                using (Barcode barcode = new Barcode())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        barcode.IncludeLabel = true;
                        barcode.Alignment = AlignmentPositions.CENTER;
                        barcode.LabelFont = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, FontSize, System.Drawing.FontStyle.Regular);
                        var barcodeImage = barcode.Encode(type, Code, System.Drawing.Color.Black, System.Drawing.Color.White, Length, Height);
                        barcodeImage.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static byte[] GetBarCode(string Code, TYPE type = TYPE.EAN13, int Length = 150, int Height = 40, int FontSize = 4)
        {
            try
            {
                using (Barcode barcode = new Barcode())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        barcode.IncludeLabel = true;
                        barcode.Alignment = AlignmentPositions.CENTER;
                        barcode.LabelFont = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, FontSize, System.Drawing.FontStyle.Regular);
                        var barcodeImage = barcode.Encode(type, Code, System.Drawing.Color.Black, System.Drawing.Color.White, Length, Height);
                        barcodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        ms.Position = 0;
                        using (BinaryReader reader = new BinaryReader(ms))
                        {
                            byte[] bytes = (byte[])reader.ReadBytes((int)ms.Length).Clone();
                            reader.Dispose();
                            ms.Dispose();
                            return bytes;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        //撈取業務人員資料
        public ActionResult AjaxBusiness(string id)
        {
            var data = _context.Pepo10.Where(x => x.PeNo == id).ToList();
            return Json(data);
        }

        //撈取司機人員資料
        public ActionResult AjaxDriver(string id)
        {
            var data = _context.Pepo10.Where(x => x.PeNo == id).FirstOrDefault();
            return Json(data);
        }

        public ActionResult AjaxVender(string vendorNo)
        {
            if (string.IsNullOrEmpty(vendorNo))
            {
                return Json(null);
            }

            var data = _context.Vendor10.Where(x => x.VendorNo == vendorNo).FirstOrDefault();

            if (data == null)
            {
                return Json(null);
            }

            return Json(data);
        }

        public ActionResult AjaxCustomer(string coNo)
        {
            if (string.IsNullOrEmpty(coNo))
            {
                return Json(null);
            }

            var data = _context.Cstm10.Where(x => x.CoNo == coNo).FirstOrDefault();

            if (data == null)
            {
                return Json(null);
            }

            return Json(data);
        }

        public ActionResult AjaxChangePassword(string userId, string pwd)
        {
            var checkData = false;

            if (string.IsNullOrEmpty(pwd))
            {
                return Json(checkData);
            }

            var userData = _context.Users.Where(x => x.Userid == userId).FirstOrDefault();

            if (userData == null)
            {
                return Json(checkData);
            }

            var encryptPwd = EncryptPwd(pwd);

            userData.NewPwd = encryptPwd;
            checkData = true;

            _context.SaveChanges();

            return Json(checkData);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public string EncryptPwd(string pwd)
        {
            //SHA256
            var hashed = EncryptProvider.Sha256(pwd);

            return hashed;
        }
    }
}